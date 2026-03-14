using Mdar.Core.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace Mdar.API.Extensions;

/// <summary>
/// امتدادات تسجيل خدمات طبقة API في حاوية الـ DI.
/// تُستدعى من Program.cs لتجميع كل إعدادات الطبقة في مكان واحد.
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    /// يُسجِّل إعدادات الـ Controllers مع JSON serialization المُهيَّأة.
    ///
    /// الإعدادات المُطبَّقة:
    ///   - تسلسل Enums كنصوص (لا أرقام) للوضوح في الـ API
    ///   - تجاهل الحقول null في الاستجابة لتخفيف حجمها
    ///   - camelCase لأسماء الحقول (معيار JSON APIs)
    /// </summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums كنصوص: "Pending" بدل 0
                options.JsonSerializerOptions.Converters
                    .Add(new JsonStringEnumConverter());

                // حذف الحقول null من الاستجابة
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;

                // camelCase: "dueDate" لا "DueDate"
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        return services;
    }

    /// <summary>
    /// يُسجِّل Swagger مع وثائق API ودعم JWT Authentication.
    /// يُضيف زر "Authorize" في واجهة Swagger لاختبار النقاط المحمية.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Mdar Personal ERP API",
                Version     = "v1",
                Description = "نظام ERP شخصي مدمج مع محرك أولويات مبني على أوقات الصلاة."
            });

            // تمكين JWT Authentication في Swagger UI
            var jwtScheme = new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "أدخل JWT Token: Bearer {token}"
            };

            options.AddSecurityDefinition("Bearer", jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id   = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // تضمين XML Comments في Swagger من ملف التوثيق
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    /// <summary>
    /// يُسجِّل JWT Bearer Authentication.
    ///
    /// الإعدادات تُقرأ من appsettings.json تحت مفتاح "Jwt":
    /// <code>
    /// "Jwt": {
    ///   "Key":      "your-super-secret-key-min-32-chars",
    ///   "Issuer":   "MdarAPI",
    ///   "Audience": "MdarClient"
    /// }
    /// </code>
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(
            jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key مطلوب في appsettings.json"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ValidateIssuer           = true,
                    ValidIssuer              = jwtSection["Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = jwtSection["Audience"],
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero // لا هامش زمني
                };

                // إرجاع ProblemDetails عند فشل الـ Authentication
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode  = 401;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            status = 401,
                            title  = "غير مصرح بالوصول",
                            detail = "رمز التحقق مفقود أو منتهي الصلاحية."
                        });
                    }
                };
            });

        return services;
    }
}
