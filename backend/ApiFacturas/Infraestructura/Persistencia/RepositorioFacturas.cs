using System.Data;
using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Importacion;
using ApiFacturas.Dominio;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApiFacturas.Infraestructura.Persistencia;
public sealed class RepositorioFacturas(ContextoFacturas baseDatos) : IRepositorioFacturas
{
    public async Task<ResultadoImportacion> ImportarLoteAsync(LoteValidado lote, CancellationToken cancelacion)
    {
        await baseDatos.Database.OpenConnectionAsync(cancelacion);
        var conexion = (SqliteConnection)baseDatos.Database.GetDbConnection();
        await using var transaccion = conexion.BeginTransaction(IsolationLevel.Serializable, deferred: false);
        await baseDatos.Database.UseTransactionAsync(transaccion, cancelacion);
        var existentes = await baseDatos.Facturas.AsNoTracking().ToDictionaryAsync(factura => factura.NumeroFactura, factura => factura.HuellaOrigen, cancelacion);
        var duplicados = lote.Duplicados.ToList();
        int importadas = 0, inconsistentes = 0;
        foreach (var factura in lote.Facturas)
        {
            if (existentes.TryGetValue(factura.NumeroFactura, out var huella))
            {
                if (huella != factura.HuellaOrigen)
                    throw new ErrorNegocio(TipoError.Conflicto, $"Numero {factura.NumeroFactura} existente con contenido diferente; no se guardo el lote.");
                duplicados.Add(factura.NumeroFactura);
                continue;
            }

            baseDatos.Facturas.Add(factura);
            importadas++;
            if (!factura.EsConsistente)
                inconsistentes++;
        }

        await baseDatos.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return new(importadas, inconsistentes, duplicados.Count, duplicados, lote.Advertencias);
    }

    public async Task<IReadOnlyList<FilaFactura>> LeerActivasAsync(CancellationToken cancelacion) => await baseDatos.Proyecciones.FromSqlRaw("SELECT * FROM v_facturas_activas").AsNoTracking().ToListAsync(cancelacion);
    public async Task<IReadOnlyList<FilaFactura>> LeerInconsistentesAsync(CancellationToken cancelacion) => await baseDatos.Proyecciones.AsNoTracking().Where(fila => !fila.EsConsistente).ToListAsync(cancelacion);
    public async Task<DetallePersistido?> LeerDetalleAsync(int numero, CancellationToken cancelacion)
    {
        await using var transaccion = await baseDatos.Database.BeginTransactionAsync(cancelacion);
        var fila = await baseDatos.Proyecciones.AsNoTracking().SingleOrDefaultAsync(fila => fila.NumeroFactura == numero && fila.EsConsistente, cancelacion);
        if (fila is null)
            return null;
        var lineas = await baseDatos.Lineas.AsNoTracking().Where(linea => linea.FacturaId == fila.Id).OrderBy(linea => linea.Id).ToListAsync(cancelacion);
        var notas = await baseDatos.NotasCredito.AsNoTracking().Where(nota => nota.FacturaId == fila.Id).OrderBy(nota => nota.Id).ToListAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return new(fila, lineas, notas);
    }

    public async Task<NotaCredito> AgregarNotaAsync(int numero, decimal monto, DateOnly fecha, CancellationToken cancelacion)
    {
        await baseDatos.Database.OpenConnectionAsync(cancelacion);
        var conexion = (SqliteConnection)baseDatos.Database.GetDbConnection();
        await using var transaccion = conexion.BeginTransaction(IsolationLevel.Serializable, deferred: false);
        await baseDatos.Database.UseTransactionAsync(transaccion, cancelacion);
        var factura = await baseDatos.Facturas.Include(factura => factura.NotasCredito).SingleOrDefaultAsync(factura => factura.NumeroFactura == numero && factura.EsConsistente, cancelacion) ?? throw new ErrorNegocio(TipoError.NoEncontrado, "Factura activa no encontrada.");
        ReglasFactura.ExigirSaldo(monto, factura.MontoTotal, factura.NotasCredito.Sum(nota => nota.Monto));
        long siguienteNumero = checked((await baseDatos.NotasCredito.MaxAsync(nota => (long? )nota.NumeroNotaCredito, cancelacion) ?? 0) + 1);
        var nuevaNota = new NotaCredito
        {
            FacturaId = factura.Id,
            NumeroNotaCredito = siguienteNumero,
            FechaCreacion = fecha,
            Monto = monto
        };
        baseDatos.NotasCredito.Add(nuevaNota);
        await baseDatos.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return nuevaNota;
    }
}
