namespace Comeback.BuildingBlocks.Infrastructure.Middleware;

using Comeback.BuildingBlocks.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var (statusCode, title) = exception switch
        {
            NotFoundException     => (StatusCodes.Status404NotFound,            "Not Found"),
            ConflictException     => (StatusCodes.Status409Conflict,            "Conflict"),
            ForbiddenException    => (StatusCodes.Status403Forbidden,           "Forbidden"),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation"),
            ValidationException   => (StatusCodes.Status400BadRequest,          "Validation Error"),
            _                     => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception is ValidationException ? null : exception.Message,
        };

        // Machine-readable code lets the client localize the message (no user-facing text in backend).
        if (exception is DomainException { Code: not null } domainException)
            problemDetails.Extensions["code"] = domainException.Code;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
