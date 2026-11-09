using ApiFacturas.Api.Contratos;
using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Consultas;
using ApiFacturas.Aplicacion.Creditos;
using ApiFacturas.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace ApiFacturas.Api.Controladores;
[ApiController]
[Route("api/invoices")]
public sealed class FacturasController(ServicioConsultas consultas, RegistrarNotaCredito registrarNota) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<FacturaConsultada>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    public async Task<ActionResult<ResultadoPaginado<FacturaConsultada>>> Buscar([FromQuery] int? numero, [FromQuery] string? estadoFactura, [FromQuery] string? estadoPago, CancellationToken cancelacion, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20) => Ok(await consultas.BuscarAsync(new(numero, estadoFactura, estadoPago, pagina, tamanoPagina), cancelacion));
    [HttpGet("{numero:int}")]
    [ProducesResponseType<DetalleFactura>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<DetalleFactura>> Detalle(int numero, CancellationToken cancelacion) => Ok(await consultas.ObtenerDetalleAsync(numero, cancelacion));
    [HttpPost("{numero:int}/credit-notes")]
    [ProducesResponseType<NotaCredito>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<NotaCredito>> CrearNota(int numero, SolicitudCredito solicitud, CancellationToken cancelacion)
    {
        var nota = await registrarNota.EjecutarAsync(numero, solicitud.Monto!.Value, cancelacion);
        return CreatedAtAction(nameof(Detalle), new { numero }, nota);
    }
}
