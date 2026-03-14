using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Mdar.API.Middleware;

/// <summary>
/// Middleware للمعالجة المركزية للاستثناءات غير المتوقعة.
///
/// يُحوِّل كل Exception إلى استجابة RFC 7807 (ProblemDetails) موحَّدة،
/// مما يضمن عدم تسريب تفاصيل التنفيذ الداخلية للعميل في بيئة الإنتاج.
///
/// خريطة التحويل:
///   UnauthorizedAccessException → 401 Unauthorized
///   KeyNotFoundException         → 404 Not Found
///   ArgumentException            → 400 Bad Request
///   InvalidOperationException    → 422 Unprocessable Entity
///   Exception (غير محدد)         → 500 Internal Server Error
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "استثناء غير متوقع في {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "غير مصرح بالوصول"),
            KeyNotFoundException        => (StatusCodes.Status404NotFound,     "العنصر غير موجود"),
            ArgumentException           => (StatusCodes.Status400BadRequest,   "بيانات الطلب غير صالحة"),
            InvalidOperationException   => (StatusCodes.Status422UnprocessableEntity, "العملية غير صالحة"),
            OperationCanceledException  => (StatusCodes.Status499ClientClosedRequest, "تم إلغاء الطلب"),
            _                           => (StatusCodes.Status500InternalServerError, "خطأ داخلي في الخادم")
        };

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Instance = context.Request.Path,
            // في بيئة التطوير نُظهر التفاصيل — في الإنتاج نُخفيها
            Detail = _env.IsDevelopment()
                ? exception.ToString()
                : exception.Message
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(problem, jsonOptions);
    }
}

/// <summary>امتداد لتسجيل الـ Middleware في Pipeline بشكل أنيق</summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
