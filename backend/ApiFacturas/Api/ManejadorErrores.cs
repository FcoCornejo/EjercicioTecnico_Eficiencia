using ApiFacturas.Dominio;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApiFacturas.Api;
public sealed class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> registro) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        int codigo = 500;
        string titulo = "Error interno del servidor.";
        if (excepcion is ErrorNegocio error)
        {
            codigo = error.Tipo switch
            {
                TipoError.Validacion => 400,
                TipoError.NoEncontrado => 404,
                _ => 409
            };
            titulo = error.Message;
        }
        else if ((excepcion is SqliteException || excepcion is DbUpdateException) && (excepcion as SqliteException ?? excepcion.InnerException as SqliteException)?.SqliteErrorCode is 5 or 6 or 19)
        {
            codigo = 409;
            titulo = "Conflicto de escritura. Actualiza e intenta nuevamente.";
        }
        else if (excepcion is BadHttpRequestException peticion)
        {
            codigo = peticion.StatusCode;
            titulo = codigo == 413 ? "El archivo supera el limite de 10 MB." : "Solicitud HTTP invalida.";
        }
        else if (excepcion is OperationCanceledException && contexto.RequestAborted.IsCancellationRequested)
            return false;
        if (codigo == 500)
            registro.LogError("Fallo interno de tipo {Tipo}; traceId {Traza}", excepcion.GetType().Name, contexto.TraceIdentifier);
        contexto.Response.StatusCode = codigo;
        return await problemas.TryWriteAsync(new ProblemDetailsContext { HttpContext = contexto, ProblemDetails = new ProblemDetails { Status = codigo, Title = titulo, Instance = contexto.Request.Path } });
    }
}
