using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Consultas;
using Microsoft.AspNetCore.Mvc;

namespace ApiFacturas.Api.Controladores;
[ApiController]
[Route("api/reports")]
public sealed class ReportesController(ServicioConsultas consultas) : ControllerBase
{
    [HttpGet("overdue-without-payment-or-credit")]
    [ProducesResponseType<IReadOnlyList<FacturaConsultada>>(200)]
    public async Task<ActionResult<IReadOnlyList<FacturaConsultada>>> Vencidas(CancellationToken cancelacion) => Ok(await consultas.ObtenerVencidasAsync(cancelacion));
    [HttpGet("payment-status")]
    [ProducesResponseType<ResumenPagos>(200)]
    public async Task<ActionResult<ResumenPagos>> Resumen(CancellationToken cancelacion) => Ok(await consultas.ObtenerResumenAsync(cancelacion));
    [HttpGet("inconsistent-invoices")]
    [ProducesResponseType<IReadOnlyList<FacturaInconsistente>>(200)]
    public async Task<ActionResult<IReadOnlyList<FacturaInconsistente>>> Inconsistentes(CancellationToken cancelacion) => Ok(await consultas.ObtenerInconsistentesAsync(cancelacion));
}
