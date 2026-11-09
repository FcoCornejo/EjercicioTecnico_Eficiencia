using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Importacion;
using ApiFacturas.Dominio;

namespace ApiFacturas.Infraestructura.Lectura;
public sealed class LectorFacturasJson : ILectorFacturasJson
{
    public async Task<IReadOnlyList<OrigenFactura>> LeerAsync(Stream archivo, CancellationToken cancelacion)
    {
        try
        {
            using var documento = await JsonDocument.ParseAsync(archivo, cancellationToken: cancelacion);
            ValidarClaves(documento.RootElement);
            if (documento.RootElement.ValueKind != JsonValueKind.Object || !documento.RootElement.TryGetProperty("invoices", out var facturas) || facturas.ValueKind != JsonValueKind.Array)
                throw new ErrorNegocio(TipoError.Validacion, "El JSON debe ser un objeto con arreglo invoices.");
            if (facturas.GetArrayLength()is 0 or > 10_000)
                throw new ErrorNegocio(TipoError.Validacion, "El lote debe tener entre 1 y 10.000 facturas.");
            var resultado = new List<OrigenFactura>();
            foreach (var elemento in facturas.EnumerateArray())
            {
                if (elemento.ValueKind != JsonValueKind.Object)
                    throw new ErrorNegocio(TipoError.Validacion, "Cada factura debe ser un objeto.");
                var datos = elemento.Deserialize<FacturaEntrada>() ?? throw new ErrorNegocio(TipoError.Validacion, "Factura vacia.");
                string canonico = Canonizar(elemento);
                string huella = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonico)));
                resultado.Add(new(datos, elemento.GetRawText(), huella));
            }

            return resultado;
        }
        catch (Exception excepcion)when (excepcion is JsonException or FormatException or OverflowException)
        {
            throw new ErrorNegocio(TipoError.Validacion, "JSON invalido: revisa estructura, tipos y fechas.");
        }
    }

    private static void ValidarClaves(JsonElement elemento)
    {
        if (elemento.ValueKind == JsonValueKind.Object)
        {
            var claves = new HashSet<string>(StringComparer.Ordinal);
            foreach (var propiedad in elemento.EnumerateObject())
            {
                if (!claves.Add(propiedad.Name))
                    throw new ErrorNegocio(TipoError.Validacion, "El JSON contiene claves repetidas.");
                ValidarClaves(propiedad.Value);
            }
        }
        else if (elemento.ValueKind == JsonValueKind.Array)
            foreach (var hijo in elemento.EnumerateArray())
                ValidarClaves(hijo);
    }

    private static string Canonizar(JsonElement elemento) => elemento.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", elemento.EnumerateObject().OrderBy(propiedad => propiedad.Name, StringComparer.Ordinal).Select(propiedad => JsonSerializer.Serialize(propiedad.Name) + ":" + Canonizar(propiedad.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", elemento.EnumerateArray().Select(Canonizar)) + "]",
        JsonValueKind.Number => elemento.GetDecimal().ToString("G29", CultureInfo.InvariantCulture),
        JsonValueKind.String => JsonSerializer.Serialize(elemento.GetString()),
        _ => elemento.GetRawText()};
}
