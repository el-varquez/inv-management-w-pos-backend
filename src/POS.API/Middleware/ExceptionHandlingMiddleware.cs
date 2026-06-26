using System.Net;
using System.Text.Json;
using FluentValidation;
using POS.Domain.Exceptions;

namespace POS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger
    )
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

    public async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message),
            DomainException e => (HttpStatusCode.BadRequest, e.Message),
            ValidationException e => (HttpStatusCode.BadRequest,
                string.Join(", ", e.Errors.Select(x => x.ErrorMessage))),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occured.")
        };

        if (statusCode == HttpStatusCode.InternalServerError) _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(response);
    }
}