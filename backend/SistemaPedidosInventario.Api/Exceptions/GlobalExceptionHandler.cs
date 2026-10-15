using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SistemaPedidosInventario.Api.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problema = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case BusinessRuleException reglaEx:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problema.Status = StatusCodes.Status400BadRequest;
                problema.Title = "Regla de Negocio Incumplida";
                problema.Detail = reglaEx.Message;
                break;

            case NotFoundException noEncontradoEx:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                problema.Status = StatusCodes.Status404NotFound;
                problema.Title = "Recurso No Encontrado";
                problema.Detail = noEncontradoEx.Message;
                break;

            case DbUpdateConcurrencyException:
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                problema.Status = StatusCodes.Status409Conflict;
                problema.Title = "Conflicto de Concurrencia";
                problema.Detail = "El recurso fue modificado por otra operación simultánea. Por favor, intente de nuevo.";
                break;

            default:
                _logger.LogError(exception, "Error no controlado durante la solicitud HTTP");
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problema.Status = StatusCodes.Status500InternalServerError;
                problema.Title = "Error Interno del Servidor";
                problema.Detail = "Ocurrió un error inesperado al procesar la solicitud.";
                break;
        }

        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);
        return true;
    }
}
