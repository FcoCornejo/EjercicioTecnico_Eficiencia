using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json.Nodes;
using System.Text;

namespace ApiFacturas.Pruebas;
public sealed class RelojFijo : IRelojNegocio
{
    public DateOnly Hoy => new(2026, 11, 8);
}

public sealed class ServidorPrueba : WebApplicationFactory<Program>
{
    public string RutaBase { get; } = Path.Combine(Path.GetTempPath(), $"eficiencia-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Testing").UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ApiFacturas"))).ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Facturas"] = $"Data Source={RutaBase};Default Timeout=5", ["Logging:LogLevel:Default"] = "None" })).ConfigureServices(servicios =>
        {
            servicios.RemoveAll<IRelojNegocio>();
            servicios.AddSingleton<IRelojNegocio, RelojFijo>();
        });
    }

    protected override void Dispose(bool liberando)
    {
        base.Dispose(liberando);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var sufijo in new[]
        {
            "",
            "-wal",
            "-shm"
        }

        )
            if (File.Exists(RutaBase + sufijo))
                File.Delete(RutaBase + sufijo);
    }
}

public static class DatosPrueba
{
    public static JsonObject Factura(int numero = 1, int dias = 31, bool pagada = false) => new()
    {
        ["invoice_number"] = numero,
        ["invoice_date"] = "2026-10-01",
        ["payment_due_date"] = new DateOnly(2026, 11, 8).AddDays(-dias).ToString("yyyy-MM-dd"),
        ["days_to_due"] = 30,
        ["total_amount"] = 100,
        ["invoice_status"] = "canceled",
        ["payment_status"] = "Paid",
        ["customer"] = new JsonObject
        {
            ["customer_run"] = "SINTETICO",
            ["customer_name"] = "Cliente sintetico",
            ["customer_email"] = "demo@example.test"
        },
        ["invoice_detail"] = new JsonArray(new JsonObject { ["product_name"] = "Producto de prueba", ["unit_price"] = 100, ["quantity"] = 1, ["subtotal"] = 100 }),
        ["invoice_payment"] = new JsonObject
        {
            ["payment_date"] = pagada ? "2026-11-01" : null,
            ["payment_method"] = pagada ? "Prueba" : null
        },
        ["invoice_credit_note"] = new JsonArray()
    };
    public static JsonObject Nota(int numero, int monto) => new()
    {
        ["credit_note_number"] = numero,
        ["credit_note_date"] = "2026-10-10",
        ["credit_note_amount"] = monto
    };
    public static string Documento(params JsonObject[] facturas) => new JsonObject
    {
        ["invoices"] = new JsonArray(facturas.Select(factura => (JsonNode)factura).ToArray())
    }.ToJsonString();
    public static async Task<HttpResponseMessage> Importar(HttpClient cliente, string json)
    {
        using var formulario = new MultipartFormDataContent();
        formulario.Add(new StringContent(json, Encoding.UTF8, "application/json"), "archivo", "datos.json");
        return await cliente.PostAsync("/api/imports", formulario);
    }
}
