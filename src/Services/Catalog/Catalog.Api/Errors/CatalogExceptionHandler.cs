using System.Diagnostics;
using Catalog.Application.Products;
using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Errors;

public sealed class CatalogExceptionHandler(
    ILogger<CatalogExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ProductNotFoundException => (StatusCodes.Status404NotFound, "Product not found"),
            DuplicateProductSkuException => (StatusCodes.Status409Conflict, "SKU conflict"),
            InsufficientInventoryException => (StatusCodes.Status409Conflict, "Insufficient inventory"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for request {RequestPath}",
                httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "The request could not be completed."
                : exception.Message,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });

        return true;
    }
}
