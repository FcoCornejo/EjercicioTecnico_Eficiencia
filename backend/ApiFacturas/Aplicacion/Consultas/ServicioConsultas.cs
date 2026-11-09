using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Dominio;

namespace ApiFacturas.Aplicacion.Consultas;
public sealed class ServicioConsultas(IRepositorioFacturas repositorio, IRelojNegocio reloj)
{
    private FacturaConsultada Proyectar(FilaFactura fila, DateOnly hoy)
    {
        var estado = ReglasFactura.Calcular(fila.MontoTotal, fila.MontoAcreditado, fila.CantidadNotasCredito, fila.FechaPago, fila.FechaVencimiento, hoy);
        return new(fila.Id, fila.NumeroFactura, fila.FechaFactura, fila.FechaVencimiento, fila.MontoTotal, fila.TotalDetalle, fila.MontoAcreditado, estado.SaldoPendiente, fila.RunCliente, fila.NombreCliente, fila.CorreoCliente, fila.FechaPago, fila.MetodoPago, fila.EsConsistente, estado.EstadoFactura, estado.EstadoPago, fila.CantidadNotasCredito, estado.DiasVencidos);
    }

    public async Task<ResultadoPaginado<FacturaConsultada>> BuscarAsync(FiltrosFactura filtros, CancellationToken cancelacion)
    {
        if (filtros.Pagina is < 1 or > 1_000_000 || filtros.TamanoPagina is < 1 or > 100 || filtros.Numero is <= 0)
            throw new ErrorNegocio(TipoError.Validacion, "Numero positivo, pagina >= 1 y tamano entre 1 y 100.");
        if (filtros.EstadoFactura is not null && !new[]
        {
            "Issued",
            "Partial",
            "Cancelled"
        }.Contains(filtros.EstadoFactura))
            throw new ErrorNegocio(TipoError.Validacion, "Estado de factura invalido.");
        if (filtros.EstadoPago is not null && !new[]
        {
            "Paid",
            "Pending",
            "Overdue"
        }.Contains(filtros.EstadoPago))
            throw new ErrorNegocio(TipoError.Validacion, "Estado de pago invalido.");
        var hoy = reloj.Hoy;
        var filas = await repositorio.LeerActivasAsync(cancelacion);
        var consulta = filas.Select(fila => Proyectar(fila, hoy)).Where(fila => (!filtros.Numero.HasValue || fila.NumeroFactura == filtros.Numero) && (filtros.EstadoFactura is null || fila.EstadoFactura == filtros.EstadoFactura) && (filtros.EstadoPago is null || fila.EstadoPago == filtros.EstadoPago)).OrderBy(fila => fila.NumeroFactura).ToList();
        return new(consulta.Skip((filtros.Pagina - 1) * filtros.TamanoPagina).Take(filtros.TamanoPagina).ToList(), consulta.Count, filtros.Pagina, filtros.TamanoPagina);
    }

    public async Task<DetalleFactura> ObtenerDetalleAsync(int numero, CancellationToken cancelacion)
    {
        var detalle = await repositorio.LeerDetalleAsync(numero, cancelacion) ?? throw new ErrorNegocio(TipoError.NoEncontrado, "Factura activa no encontrada.");
        return new(Proyectar(detalle.Factura, reloj.Hoy), detalle.Lineas, detalle.NotasCredito);
    }

    public async Task<IReadOnlyList<FacturaConsultada>> ObtenerVencidasAsync(CancellationToken cancelacion)
    {
        var hoy = reloj.Hoy;
        var filas = await repositorio.LeerActivasAsync(cancelacion);
        return filas.Select(fila => Proyectar(fila, hoy)).Where(fila => ReglasFactura.IncluirEnVencidas(new(fila.EstadoFactura, fila.EstadoPago, fila.SaldoPendiente, fila.DiasVencidos), fila.FechaPago.HasValue, fila.CantidadNotasCredito)).OrderBy(fila => fila.NumeroFactura).ToList();
    }

    public async Task<ResumenPagos> ObtenerResumenAsync(CancellationToken cancelacion)
    {
        var hoy = reloj.Hoy;
        var filas = await repositorio.LeerActivasAsync(cancelacion);
        var estados = filas.Select(fila => Proyectar(fila, hoy).EstadoPago).ToList();
        return new("Facturas consistentes, incluidas las anuladas", hoy, estados.Count, new[] { "Paid", "Pending", "Overdue" }.Select(estado =>
        {
            int cantidad = estados.Count(valor => valor == estado);
            return new ConteoEstado(estado, cantidad, estados.Count == 0 ? 0 : decimal.Round(cantidad * 100m / estados.Count, 2));
        }).ToList());
    }

    public async Task<IReadOnlyList<FacturaInconsistente>> ObtenerInconsistentesAsync(CancellationToken cancelacion) => (await repositorio.LeerInconsistentesAsync(cancelacion)).OrderBy(fila => fila.NumeroFactura).Select(fila => new FacturaInconsistente(fila.NumeroFactura, fila.MontoTotal, fila.TotalDetalle, fila.TotalDetalle - fila.MontoTotal, fila.NombreCliente, fila.FechaVencimiento)).ToList();
}
