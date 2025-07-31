using System.Net;
using System.Text.Json;
using Stockly.Dtos.Errors;
using Stockly.Exceptions;

namespace Stockly.Configuration;

public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    private readonly ILogger<GlobalExceptionHandler>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

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

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError; // 500 if unexpected
        var response = new ErrorResponseDto
        {
            StatusCode = (int)code,
            Message = "An unexpected error occurred.",
            Values = null
        };

        switch (exception)
        {
            case ApiApplicationException apiApplicationException:
                code = HttpStatusCode.NotFound;
                response.Message = exception.Message;
                response.Values = apiApplicationException.ExpectedValues;
                break;
            default:
                _logger.LogError("Unhandled exception occurred: {ExceptionMessage}", exception.Message);
                break;
        }

        response.StatusCode = (int)code;
        var result = JsonSerializer.Serialize(response);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}