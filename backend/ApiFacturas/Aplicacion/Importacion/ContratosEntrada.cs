using System.Text.Json.Serialization;
using ApiFacturas.Dominio;

namespace ApiFacturas.Aplicacion.Importacion;
public sealed class FacturaEntrada
{
    [JsonPropertyName("invoice_number")]
    public int? NumeroFactura { get; set; }

    [JsonPropertyName("invoice_date")]
    public DateOnly? FechaFactura { get; set; }

    [JsonPropertyName("payment_due_date")]
    public DateOnly? FechaVencimiento { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal? MontoTotal { get; set; }

    [JsonPropertyName("days_to_due")]
    public int? DiasAlVencimiento { get; set; }

    [JsonPropertyName("invoice_detail")]
    public List<LineaEntrada?>? Lineas { get; set; }

    [JsonPropertyName("invoice_credit_note")]
    public List<NotaEntrada?>? NotasCredito { get; set; }

    [JsonPropertyName("invoice_payment")]
    public PagoEntrada? Pago { get; set; }

    [JsonPropertyName("customer")]
    public ClienteEntrada? Cliente { get; set; }
}

public sealed record LineaEntrada([property: JsonPropertyName("product_name")] string? NombreProducto, [property: JsonPropertyName("unit_price")] decimal? PrecioUnitario, [property: JsonPropertyName("quantity")] int? Cantidad, [property: JsonPropertyName("subtotal")] decimal? Subtotal);
public sealed record NotaEntrada([property: JsonPropertyName("credit_note_number")] long? NumeroNotaCredito, [property: JsonPropertyName("credit_note_date")] DateOnly? Fecha, [property: JsonPropertyName("credit_note_amount")] decimal? Monto);
public sealed record PagoEntrada([property: JsonPropertyName("payment_date")] DateOnly? Fecha, [property: JsonPropertyName("payment_method")] string? Metodo);
public sealed record ClienteEntrada([property: JsonPropertyName("customer_run")] string? Run, [property: JsonPropertyName("customer_name")] string? Nombre, [property: JsonPropertyName("customer_email")] string? Correo);
public sealed record OrigenFactura(FacturaEntrada Datos, string JsonOriginal, string Huella);
public sealed record LoteValidado(IReadOnlyList<Factura> Facturas, IReadOnlyList<int> Duplicados, IReadOnlyList<string> Advertencias);
