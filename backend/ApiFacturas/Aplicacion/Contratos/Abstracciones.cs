using ApiFacturas.Dominio;
using ApiFacturas.Aplicacion.Importacion;

namespace ApiFacturas.Aplicacion.Contratos;
public interface IRelojNegocio
{
    DateOnly Hoy { get; }
}

public interface ILectorFacturasJson
{
    Task<IReadOnlyList<OrigenFactura>> LeerAsync(Stream archivo, CancellationToken cancelacion);
}

public interface IRepositorioFacturas
{
    Task<ResultadoImportacion> ImportarLoteAsync(LoteValidado lote, CancellationToken cancelacion);
    Task<IReadOnlyList<FilaFactura>> LeerActivasAsync(CancellationToken cancelacion);
    Task<IReadOnlyList<FilaFactura>> LeerInconsistentesAsync(CancellationToken cancelacion);
    Task<DetallePersistido?> LeerDetalleAsync(int numero, CancellationToken cancelacion);
    Task<NotaCredito> AgregarNotaAsync(int numero, decimal monto, DateOnly fecha, CancellationToken cancelacion);
}

public sealed class FilaFactura
{
    public int Id { get; set; }
    public int NumeroFactura { get; set; }
    public DateOnly FechaFactura { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal TotalDetalle { get; set; }
    public decimal MontoAcreditado { get; set; }
    public int CantidadNotasCredito { get; set; }
    public bool EsConsistente { get; set; }
    public string RunCliente { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string CorreoCliente { get; set; } = "";
    public DateOnly? FechaPago { get; set; }
    public string? MetodoPago { get; set; }
}

public sealed record DetallePersistido(FilaFactura Factura, IReadOnlyList<LineaFactura> Lineas, IReadOnlyList<NotaCredito> NotasCredito);
public sealed record ResultadoImportacion(int Importadas, int Inconsistentes, int DuplicadasOmitidas, IReadOnlyList<int> NumerosDuplicados, IReadOnlyList<string> Advertencias);
public sealed record FiltrosFactura(int? Numero = null, string? EstadoFactura = null, string? EstadoPago = null, int Pagina = 1, int TamanoPagina = 20);
public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Elementos, int Total, int Pagina, int TamanoPagina);
public sealed record FacturaConsultada(int Id, int NumeroFactura, DateOnly FechaFactura, DateOnly FechaVencimiento, decimal MontoTotal, decimal TotalDetalle, decimal MontoAcreditado, decimal SaldoPendiente, string RunCliente, string NombreCliente, string CorreoCliente, DateOnly? FechaPago, string? MetodoPago, bool EsConsistente, string EstadoFactura, string EstadoPago, int CantidadNotasCredito, int DiasVencidos);
public sealed record DetalleFactura(FacturaConsultada Factura, IReadOnlyList<LineaFactura> Lineas, IReadOnlyList<NotaCredito> NotasCredito);
public sealed record ConteoEstado(string Estado, int Cantidad, decimal Porcentaje);
public sealed record ResumenPagos(string Universo, DateOnly FechaCalculo, int Total, IReadOnlyList<ConteoEstado> Elementos);
public sealed record FacturaInconsistente(int NumeroFactura, decimal MontoTotal, decimal TotalDetalle, decimal Diferencia, string NombreCliente, DateOnly FechaVencimiento);
