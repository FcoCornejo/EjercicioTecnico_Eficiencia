using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiFacturas.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiFacturas.Pruebas;
public sealed class PruebasIntegracion
{
    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) => await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    [Fact]
    public async Task ImportaArchivoOriginalYCalculaReportes()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        string json = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "bd_exam_invoices.json"));
        var importacion = await DatosPrueba.Importar(cliente, json);
        importacion.EnsureSuccessStatusCode();
        var resultado = await Leer(importacion);
        Assert.Equal(50, resultado.GetProperty("importadas").GetInt32());
        Assert.Equal(2, resultado.GetProperty("inconsistentes").GetInt32());
        var segunda = await Leer(await DatosPrueba.Importar(cliente, json));
        Assert.Equal(50, segunda.GetProperty("duplicadasOmitidas").GetInt32());
        var activas = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices?tamanoPagina=100");
        Assert.Equal(48, activas.GetProperty("total").GetInt32());
        var inconsistentes = await cliente.GetFromJsonAsync<JsonElement>("/api/reports/inconsistent-invoices");
        Assert.Equal(new[] { 41, 48 }, inconsistentes.EnumerateArray().Select(fila => fila.GetProperty("numeroFactura").GetInt32()).ToArray());
        Assert.Equal(137165m, inconsistentes[0].GetProperty("diferencia").GetDecimal());
        Assert.Equal(6m, inconsistentes[1].GetProperty("diferencia").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/invoices/41")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsJsonAsync("/api/invoices/41/credit-notes", new { monto = 1 })).StatusCode);
        var resumen = await cliente.GetFromJsonAsync<JsonElement>("/api/reports/payment-status");
        Assert.Equal(48, resumen.GetProperty("total").GetInt32());
        Assert.Equal("2026-11-08", resumen.GetProperty("fechaCalculo").GetString());
        Assert.Contains("consistentes", resumen.GetProperty("universo").GetString());
        var estados = resumen.GetProperty("elementos").EnumerateArray().ToArray();
        Assert.Equal(new[] { 13, 0, 35 }, estados.Select(fila => fila.GetProperty("cantidad").GetInt32()).ToArray());
        Assert.Equal(27.08m, estados[0].GetProperty("porcentaje").GetDecimal());
        var vencidas = await cliente.GetFromJsonAsync<JsonElement>("/api/reports/overdue-without-payment-or-credit");
        Assert.Equal(30, vencidas.GetArrayLength());
        var detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices/30");
        Assert.Equal("Issued", detalle.GetProperty("factura").GetProperty("estadoFactura").GetString());
    }

    [Fact]
    public async Task ConflictoExistenteRevierteTodoElLote()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura()))).EnsureSuccessStatusCode();
        var distinta = DatosPrueba.Factura();
        distinta["customer"]!["customer_name"] = "Otro cliente sintetico";
        var respuesta = await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(2), distinta));
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var activas = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices");
        Assert.Equal(1, activas.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/invoices/2")).StatusCode);
    }

    [Fact]
    public async Task DuplicadosDentroDelLote()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var iguales = await Leer(await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(), DatosPrueba.Factura())));
        Assert.Equal(1, iguales.GetProperty("importadas").GetInt32());
        Assert.Equal(1, iguales.GetProperty("duplicadasOmitidas").GetInt32());
        var distinta = DatosPrueba.Factura(3);
        distinta["total_amount"] = 101;
        var conflicto = await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(3), distinta));
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/invoices/3")).StatusCode);
    }

    [Fact]
    public async Task HuellaIndependienteDeFormatoYDeNcNuevas()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var fuente = DatosPrueba.Factura(pagada: true);
        string original = DatosPrueba.Documento(fuente);
        (await DatosPrueba.Importar(cliente, original)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto = 20 })).StatusCode);
        var reordenada = new JsonObject(fuente.Reverse().Select(par => KeyValuePair.Create(par.Key, par.Value?.DeepClone())));
        var segunda = await Leer(await DatosPrueba.Importar(cliente, DatosPrueba.Documento(reordenada)));
        Assert.Equal(1, segunda.GetProperty("duplicadasOmitidas").GetInt32());
    }

    [Fact]
    public async Task FiltraAntesDePaginar()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var parcial = DatosPrueba.Factura(13);
        parcial["invoice_credit_note"]!.AsArray().Add(DatosPrueba.Nota(1, 20));
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(1), DatosPrueba.Factura(2), parcial))).EnsureSuccessStatusCode();
        var filtradas = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices?numero=13&estadoFactura=Partial&estadoPago=Overdue&tamanoPagina=1");
        Assert.Equal(1, filtradas.GetProperty("total").GetInt32());
        Assert.Equal(13, filtradas.GetProperty("elementos")[0].GetProperty("numeroFactura").GetInt32());
        Assert.Equal(1, (await cliente.GetFromJsonAsync<JsonElement>("/api/invoices?estadoFactura=Partial&tamanoPagina=1")).GetProperty("total").GetInt32());
        Assert.Equal(3, (await cliente.GetFromJsonAsync<JsonElement>("/api/invoices?estadoPago=Overdue")).GetProperty("total").GetInt32());
        Assert.Equal(1, (await cliente.GetFromJsonAsync<JsonElement>("/api/invoices?numero=13")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task PagadaAdmiteNcYSaldoExacto()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(pagada: true)))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto = 40 })).StatusCode);
        var detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices/1");
        Assert.Equal("Paid", detalle.GetProperty("factura").GetProperty("estadoPago").GetString());
        Assert.Equal(60, detalle.GetProperty("factura").GetProperty("saldoPendiente").GetDecimal());
        Assert.Equal("2026-11-08", detalle.GetProperty("notasCredito")[0].GetProperty("fechaCreacion").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto = 61 })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto = 60 })).StatusCode);
        detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices/1");
        Assert.Equal("Cancelled", detalle.GetProperty("factura").GetProperty("estadoFactura").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto = 1 })).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.1)]
    public async Task MontosInvalidosDevuelven400(decimal monto)
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura()))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto })).StatusCode);
        Assert.Empty((await cliente.GetFromJsonAsync<JsonElement>("/api/invoices/1")).GetProperty("notasCredito").EnumerateArray());
    }

    [Fact]
    public async Task DosNcConcurrentesNoSobreacreditan()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura()))).EnsureSuccessStatusCode();
        using var salida = new ManualResetEventSlim(false);
        var tareas = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            using var conexion = servidor.CreateClient();
            salida.Wait();
            return await conexion.PostAsJsonAsync("/api/invoices/1/credit-notes", new { monto = 70 });
        })).ToArray();
        salida.Set();
        var respuestas = await Task.WhenAll(tareas);
        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Created);
        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Conflict);
        var detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices/1");
        Assert.Equal(30, detalle.GetProperty("factura").GetProperty("saldoPendiente").GetDecimal());
        Assert.Single(detalle.GetProperty("notasCredito").EnumerateArray());
    }

    [Fact]
    public async Task DosImportacionesConcurrentesNoDuplican()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        string documento = DatosPrueba.Documento(DatosPrueba.Factura());
        using var salida = new ManualResetEventSlim(false);
        var tareas = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            using var conexion = servidor.CreateClient();
            salida.Wait();
            return await DatosPrueba.Importar(conexion, documento);
        })).ToArray();
        salida.Set();
        var respuestas = await Task.WhenAll(tareas);
        Assert.All(respuestas, respuesta => respuesta.EnsureSuccessStatusCode());
        var resultados = await Task.WhenAll(respuestas.Select(Leer));
        Assert.Equal(1, resultados.Sum(resultado => resultado.GetProperty("importadas").GetInt32()));
        Assert.Equal(1, (await cliente.GetFromJsonAsync<JsonElement>("/api/invoices")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ReporteYFechasLimite()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var conNota = DatosPrueba.Factura(6, 31);
        conNota["invoice_credit_note"]!.AsArray().Add(DatosPrueba.Nota(1, 1));
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(1, 29), DatosPrueba.Factura(2, 30), DatosPrueba.Factura(3, 31), DatosPrueba.Factura(4, 0), DatosPrueba.Factura(5, 31, true), conNota, DatosPrueba.Factura(7, -1)))).EnsureSuccessStatusCode();
        var reporte = await cliente.GetFromJsonAsync<JsonElement>("/api/reports/overdue-without-payment-or-credit");
        Assert.Single(reporte.EnumerateArray());
        Assert.Equal(3, reporte[0].GetProperty("numeroFactura").GetInt32());
        var pendientes = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices?estadoPago=Pending");
        Assert.Equal(2, pendientes.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task UniversoVacioNoDividePorCero()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var resumen = await cliente.GetFromJsonAsync<JsonElement>("/api/reports/payment-status");
        Assert.Equal(0, resumen.GetProperty("total").GetInt32());
        Assert.Equal(3, resumen.GetProperty("elementos").GetArrayLength());
        Assert.All(resumen.GetProperty("elementos").EnumerateArray(), fila => Assert.Equal(0, fila.GetProperty("porcentaje").GetDecimal()));
    }

    [Fact]
    public async Task VistaNoMultiplicaAgregadosYPreservaFechas()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var factura = DatosPrueba.Factura();
        var lineas = factura["invoice_detail"]!.AsArray();
        lineas[0]!["subtotal"] = 40;
        lineas[0]!["unit_price"] = 40;
        lineas.Add(new JsonObject { ["product_name"] = "Segundo sintetico", ["unit_price"] = 60, ["quantity"] = 1, ["subtotal"] = 60 });
        factura["invoice_credit_note"]!.AsArray().Add(DatosPrueba.Nota(1, 10));
        factura["invoice_credit_note"]!.AsArray().Add(DatosPrueba.Nota(2, 20));
        (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(factura))).EnsureSuccessStatusCode();
        var detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/invoices/1");
        var datos = detalle.GetProperty("factura");
        Assert.Equal(100, datos.GetProperty("totalDetalle").GetDecimal());
        Assert.Equal(30, datos.GetProperty("montoAcreditado").GetDecimal());
        Assert.Equal(70, datos.GetProperty("saldoPendiente").GetDecimal());
        Assert.All(detalle.GetProperty("notasCredito").EnumerateArray(), nota => Assert.Equal("2026-10-10", nota.GetProperty("fechaCreacion").GetString()));
    }

    [Theory]
    [InlineData("no-json")]
    [InlineData("{}")]
    [InlineData("{\"invoices\":[]}")]
    [InlineData("{\"invoices\":[null]}")]
    [InlineData("{\"invoices\":[],\"invoices\":[]}")]
    public async Task EstructurasInvalidas(string json)
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await DatosPrueba.Importar(cliente, json)).StatusCode);
        Assert.Equal(0, (await cliente.GetFromJsonAsync<JsonElement>("/api/invoices")).GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("total")]
    [InlineData("cliente")]
    [InlineData("nc")]
    [InlineData("sin-total")]
    public async Task LoteEstructuralmenteInvalidoEsAtomico(string campo)
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        var invalida = DatosPrueba.Factura(2);
        switch (campo)
        {
            case "total":
                invalida["total_amount"] = 1.5;
                break;
            case "cliente":
                invalida.Remove("customer");
                break;
            case "nc":
                invalida["invoice_credit_note"]!.AsArray().Add(DatosPrueba.Nota(1, 101));
                break;
            default:
                invalida.Remove("total_amount");
                break;
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura(), invalida))).StatusCode);
        Assert.Equal(0, (await cliente.GetFromJsonAsync<JsonElement>("/api/invoices")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task CodigosContratoYSwagger()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        foreach (var ruta in new[]
        {
            "/api/invoices?estadoPago=unknown",
            "/api/invoices?pagina=0",
            "/api/invoices?numero=-1",
            "/api/invoices?tamanoPagina=101"
        }

        )
        {
            var respuesta = await cliente.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            var problema = await Leer(respuesta);
            Assert.Equal(400, problema.GetProperty("status").GetInt32());
            Assert.True(problema.TryGetProperty("title", out _));
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/invoices/1/credit-notes", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/invoices/99999")).StatusCode);
        var documento = await cliente.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(documento.GetProperty("paths").TryGetProperty("/api/imports", out _));
        Assert.True(documento.GetProperty("paths").GetProperty("/api/invoices/{numero}/credit-notes").GetProperty("post").GetProperty("responses").TryGetProperty("409", out _));
    }

    [Fact]
    public async Task MigracionesReinicioEIntegridad()
    {
        using var servidor = new ServidorPrueba();
        using (var cliente = servidor.CreateClient())
            (await DatosPrueba.Importar(cliente, DatosPrueba.Documento(DatosPrueba.Factura()))).EnsureSuccessStatusCode();
        using (var alcance = servidor.Services.CreateScope())
        {
            var bd = alcance.ServiceProvider.GetRequiredService<ContextoFacturas>();
            Assert.Single(await bd.Database.GetAppliedMigrationsAsync());
            Assert.False(bd.Database.HasPendingModelChanges());
            bd.Facturas.Add(new() { NumeroFactura = 1, FechaFactura = new(2026, 11, 8), FechaVencimiento = new(2026, 11, 8) });
            await Assert.ThrowsAsync<DbUpdateException>(() => bd.SaveChangesAsync());
            bd.ChangeTracker.Clear();
            bd.Lineas.Add(new() { FacturaId = 99999, NombreProducto = "Huerfano", Cantidad = 1 });
            await Assert.ThrowsAsync<DbUpdateException>(() => bd.SaveChangesAsync());
            bd.ChangeTracker.Clear();
            await bd.Database.MigrateAsync();
            Assert.Single(await bd.Facturas.ToListAsync());
        }

        using var nuevoCliente = servidor.CreateClient();
        Assert.Equal(1, (await nuevoCliente.GetFromJsonAsync<JsonElement>("/api/invoices")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task MigracionReversibleConVistas()
    {
        using var servidor = new ServidorPrueba();
        using var cliente = servidor.CreateClient();
        using var alcance = servidor.Services.CreateScope();
        var bd = alcance.ServiceProvider.GetRequiredService<ContextoFacturas>();
        await bd.GetService<IMigrator>().MigrateAsync("0");
        Assert.Empty(await bd.Database.GetAppliedMigrationsAsync());
        await bd.Database.MigrateAsync();
        Assert.Empty(await bd.Proyecciones.ToListAsync());
    }
}
