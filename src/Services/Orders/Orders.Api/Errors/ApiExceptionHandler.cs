using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Orders;
using Orders.Domain.Exceptions;

namespace Orders.Api.Errors;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            OrderNotFoundException => (StatusCodes.Status404NotFound, "Order not found"),
            CatalogProductNotFoundException => (StatusCodes.Status404NotFound, "Product not found"),
            OrderStateException => (StatusCodes.Status409Conflict, "Order state conflict"),
            InsufficientProductStockException => (StatusCodes.Status409Conflict, "Insufficient product stock"),
            CatalogUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Catalog unavailable"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
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
            Detail = status switch
            {
                StatusCodes.Status500InternalServerError => "The request could not be completed.",
                StatusCodes.Status503ServiceUnavailable => "The request could not be completed because Catalog is unavailable.",
                _ => exception.Message
            },
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        var problemDetailsService = httpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        return true;
    }
}
