using ApiFacturas.Dominio;

namespace ApiFacturas.Aplicacion.Importacion;
public sealed class ValidadorImportacion
{
    public LoteValidado Validar(IReadOnlyList<OrigenFactura> origenes)
    {
        if (origenes.Count is 0 or > 10_000)
            throw new ErrorNegocio(TipoError.Validacion, "El archivo debe contener entre 1 y 10.000 facturas.");
        var facturas = new Dictionary<int, Factura>();
        var duplicados = new List<int>();
        var advertencias = new List<string>();
        foreach (var origen in origenes)
        {
            var factura = ConvertirYValidar(origen, advertencias);
            if (facturas.TryGetValue(factura.NumeroFactura, out var anterior))
            {
                if (anterior.HuellaOrigen != factura.HuellaOrigen)
                    throw new ErrorNegocio(TipoError.Conflicto, $"Numero {factura.NumeroFactura} repetido con contenido diferente dentro del lote.");
                duplicados.Add(factura.NumeroFactura);
            }
            else
                facturas.Add(factura.NumeroFactura, factura);
        }

        return new(facturas.Values.ToList(), duplicados, advertencias);
    }

    private static Factura ConvertirYValidar(OrigenFactura origen, List<string> advertencias)
    {
        var entrada = origen.Datos;
        void Exigir(bool condicion, string mensaje)
        {
            if (!condicion)
                throw new ErrorNegocio(TipoError.Validacion, $"Factura {entrada.NumeroFactura}: {mensaje}");
        }

        Exigir(entrada.NumeroFactura is> 0, "numero positivo obligatorio.");
        Exigir(entrada.FechaFactura is not null && entrada.FechaFactura != default(DateOnly) && entrada.FechaVencimiento is not null && entrada.FechaVencimiento != default(DateOnly), "fechas validas obligatorias.");
        Exigir(entrada.MontoTotal.HasValue && Dinero.EsValido(entrada.MontoTotal.Value), "total obligatorio no negativo en pesos enteros.");
        Exigir(entrada.Cliente is not null && !string.IsNullOrWhiteSpace(entrada.Cliente.Run) && !string.IsNullOrWhiteSpace(entrada.Cliente.Nombre) && !string.IsNullOrWhiteSpace(entrada.Cliente.Correo), "cliente incompleto.");
        Exigir(entrada.Lineas is { Count: > 0 and <= 10_000 }, "se requieren de 1 a 10.000 productos.");
        var lineas = new List<LineaFactura>();
        foreach (var linea in entrada.Lineas!)
        {
            Exigir(linea is not null && !string.IsNullOrWhiteSpace(linea.NombreProducto) && linea.Cantidad is> 0 && linea.PrecioUnitario.HasValue && Dinero.EsValido(linea.PrecioUnitario.Value) && linea.Subtotal.HasValue && Dinero.EsValido(linea.Subtotal.Value), "producto invalido o con decimales.");
            lineas.Add(new() { NombreProducto = linea!.NombreProducto!, Cantidad = linea.Cantidad!.Value, PrecioUnitario = linea.PrecioUnitario!.Value, Subtotal = linea.Subtotal!.Value });
            if (linea.PrecioUnitario * linea.Cantidad != linea.Subtotal)
                advertencias.Add($"Factura {entrada.NumeroFactura}: precio por cantidad difiere del subtotal de una linea; se conserva el subtotal declarado.");
        }

        Exigir(entrada.NotasCredito is null || entrada.NotasCredito.Count <= 10_000, "demasiadas notas de credito.");
        var notas = new List<NotaCredito>();
        foreach (var nota in entrada.NotasCredito ?? [])
        {
            Exigir(nota is not null && nota.NumeroNotaCredito is> 0 && nota.Fecha.HasValue && nota.Fecha != default(DateOnly) && nota.Monto.HasValue && Dinero.EsValido(nota.Monto.Value) && nota.Monto > 0, "nota historica invalida.");
            notas.Add(new() { NumeroNotaCredito = nota!.NumeroNotaCredito!.Value, FechaCreacion = nota.Fecha!.Value, Monto = nota.Monto!.Value });
        }

        Exigir(notas.Select(nota => nota.NumeroNotaCredito).Distinct().Count() == notas.Count, "numero de NC historica duplicado.");
        Exigir(notas.Sum(nota => nota.Monto) <= entrada.MontoTotal, "NC historicas superiores al total.");
        Exigir(entrada.Pago?.Fecha != default(DateOnly), "fecha de pago invalida.");
        if (entrada.DiasAlVencimiento.HasValue && entrada.FechaVencimiento!.Value.DayNumber - entrada.FechaFactura!.Value.DayNumber != entrada.DiasAlVencimiento)
            advertencias.Add($"Factura {entrada.NumeroFactura}: days_to_due no coincide; se usa payment_due_date.");
        decimal suma = lineas.Sum(linea => linea.Subtotal);
        return new Factura
        {
            NumeroFactura = entrada.NumeroFactura!.Value,
            FechaFactura = entrada.FechaFactura!.Value,
            FechaVencimiento = entrada.FechaVencimiento!.Value,
            MontoTotal = entrada.MontoTotal!.Value,
            EsConsistente = ReglasFactura.EsConsistente(entrada.MontoTotal.Value, suma),
            RunCliente = entrada.Cliente!.Run!,
            NombreCliente = entrada.Cliente.Nombre!,
            CorreoCliente = entrada.Cliente.Correo!,
            FechaPago = entrada.Pago?.Fecha,
            MetodoPago = entrada.Pago?.Metodo,
            HuellaOrigen = origen.Huella,
            JsonOrigen = origen.JsonOriginal,
            Lineas = lineas,
            NotasCredito = notas
        };
    }
}
