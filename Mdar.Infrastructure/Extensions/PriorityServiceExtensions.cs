using Mdar.Core.Interfaces;
using Mdar.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mdar.Infrastructure.Extensions;

/// <summary>
/// امتداد تسجيل خدمات محرك الأولويات في حاوية الـ DI.
///
/// الاستخدام في Program.cs:
/// <code>
/// builder.Services.AddPriorityEngine();
/// </code>
///
/// نمط Scoped مناسب لأن:
///   - AppDbContext مسجَّل Scoped (طلب HTTP واحد = DbContext واحد)
///   - الخدمات تعتمد على DbContext → يجب أن تكون Scoped أو Transient
///   - Scoped أفضل من Transient لتجنب إنشاء كائنات متعددة في نفس الطلب
/// </summary>
public static class PriorityServiceExtensions
{
    public static IServiceCollection AddPriorityEngine(this IServiceCollection services)
    {
        services.AddScoped<IPrayerTimeService, PrayerTimeService>();
        services.AddScoped<IPriorityWeightCalculator, PriorityWeightCalculator>();
        services.AddScoped<ITaskPriorityEngine, TaskPriorityEngine>();

        // Application Service Facade — الواجهة التي تُحقَن في Controllers
        services.AddScoped<IPriorityEngineService, PriorityEngineService>();

        return services;
    }
}
