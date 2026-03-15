using Mdar.API.Extensions;
using Mdar.API.Hubs;
using Mdar.API.Middleware;
using Mdar.Infrastructure.Data;
using Mdar.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════════════════
//  Mdar Personal ERP — ASP.NET Core Web API Entry Point
// ═══════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// تحميل الإعدادات المحلية السرية (مستبعدة من git)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// ─── Database ──────────────────────────────────────────────────────────────

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            // إعادة المحاولة تلقائياً عند فشل الاتصال العابر بـ TiDB
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);

            mySqlOptions.MigrationsAssembly("Mdar.Infrastructure");
        }));

// ─── Priority Engine Services (BLL) ───────────────────────────────────────

builder.Services.AddPriorityEngine();  // ← يُسجِّل: IPrayerTimeService
                                        //              IPriorityWeightCalculator
                                        //              ITaskPriorityEngine
                                        //              IPriorityEngineService

// ─── API Layer ─────────────────────────────────────────────────────────────

builder.Services.AddApiControllers();        // Controllers + JSON settings
builder.Services.AddSwaggerWithJwt();        // Swagger + JWT button
builder.Services.AddJwtAuthentication(builder.Configuration); // JWT Bearer

// ─── CORS (اضبطه حسب عنوان تطبيق الـ Frontend) ───────────────────────────

builder.Services.AddCors(options =>
    options.AddPolicy("MdarCorsPolicy", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials())); // ← مطلوب لـ SignalR WebSocket

// ─── HTTP Client (لجلب أوقات الصلاة من API خارجي مستقبلاً) ───────────────
builder.Services.AddHttpClient();

// ─── SignalR (التزامن اللحظي بين الجلسات) ──────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors =
        builder.Environment.IsDevelopment();
});

// ══════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ══════════════════════════════════════════════════════════════════════════

// ─── Middleware Pipeline (الترتيب مهم جداً) ───────────────────────────────

// [1] معالج الأخطاء العالمي — يجب أن يكون الأول دائماً
app.UseGlobalExceptionHandler();

// [2] Swagger — في جميع البيئات
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mdar API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

// [3] HTTPS Redirect
app.UseHttpsRedirection();

// [4] CORS — قبل Authentication
app.UseCors("MdarCorsPolicy");

// [5] Authentication → ثم Authorization (الترتيب ضروري)
app.UseAuthentication();
app.UseAuthorization();

// [6] Controllers + SignalR Hub
app.MapControllers();
app.MapHub<CanvasHub>("/hubs/canvas");

// ─── Database Migration التلقائي (بيئة التطوير فقط) ──────────────────────

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();
