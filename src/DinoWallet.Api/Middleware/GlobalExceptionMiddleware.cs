using System.Net;
using System.Text.Json;
using DinoWallet.Api.DTOs.Responses;
using DinoWallet.Api.Exceptions;

namespace DinoWallet.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, error, detail) = ex switch
        {
            InsufficientFundsException ife =>
                (HttpStatusCode.UnprocessableEntity,
                 "Insufficient funds",
                 $"Current balance: {ife.CurrentBalance:F4}, Requested: {ife.RequestedAmount:F4}"),

            AccountNotFoundException anf =>
                (HttpStatusCode.NotFound,
                 "Account not found",
                 anf.Message),

            InvalidAmountException iam =>
                (HttpStatusCode.BadRequest,
                 "Invalid amount",
                 iam.Message),

            _ =>
                (HttpStatusCode.InternalServerError,
                 "An unexpected error occurred",
                 null as string)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse { Error = error, Detail = detail };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
