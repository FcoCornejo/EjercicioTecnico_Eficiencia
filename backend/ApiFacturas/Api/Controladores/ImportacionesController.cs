using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Importacion;
using Microsoft.AspNetCore.Mvc;

namespace ApiFacturas.Api.Controladores;
[ApiController]
[Route("api/imports")]
public sealed class ImportacionesController(FachadaImportacion fachada) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ResultadoImportacion>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(413)]
    public async Task<ActionResult<ResultadoImportacion>> Importar(IFormFile archivo, CancellationToken cancelacion)
    {
        if (archivo.Length == 0)
            return BadRequest(new ProblemDetails { Status = 400, Title = "El archivo esta vacio." });
        await using var flujo = archivo.OpenReadStream();
        return Ok(await fachada.ImportarAsync(flujo, cancelacion));
    }
}
