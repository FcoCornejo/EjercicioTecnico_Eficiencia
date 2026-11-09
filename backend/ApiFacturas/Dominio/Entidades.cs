namespace ApiFacturas.Dominio;
public sealed class Factura
{
    public int Id { get; set; }
    public int NumeroFactura { get; set; }
    public DateOnly FechaFactura { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal MontoTotal { get; set; }
    public bool EsConsistente { get; set; }
    public string RunCliente { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string CorreoCliente { get; set; } = "";
    public DateOnly? FechaPago { get; set; }
    public string? MetodoPago { get; set; }
    public string HuellaOrigen { get; set; } = "";
    public string JsonOrigen { get; set; } = "";
    public List<LineaFactura> Lineas { get; set; } = [];
    public List<NotaCredito> NotasCredito { get; set; } = [];
}

public sealed class LineaFactura
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public string NombreProducto { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}

public sealed class NotaCredito
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public long NumeroNotaCredito { get; set; }
    public DateOnly FechaCreacion { get; set; }
    public decimal Monto { get; set; }
}
