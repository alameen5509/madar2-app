using Mdar.API.Extensions;
using Mdar.API.Hubs;
using Mdar.API.Middleware;
using Mdar.Core.Entities.Identity;
using Mdar.Infrastructure.Data;
using Mdar.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════════════════
//  Mdar Personal ERP — ASP.NET Core Web API Entry Point
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine("MDAR2 v1.1.0 STARTING");

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
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://madar22-app.vercel.app",
            "https://madar2-app.vercel.app"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

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
app.MapGet("/api/health/status", () => Results.Ok(new { status = "ok" }));

// [3] HTTPS Redirect
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// [4] CORS — قبل Authentication
app.UseCors();

// [5] Authentication → ثم Authorization (الترتيب ضروري)
app.UseAuthentication();
app.UseAuthorization();

// [6] Controllers + SignalR Hub
app.MapControllers();
app.MapHub<CanvasHub>("/hubs/canvas");

// ─── Seed: إنشاء Admin افتراضي إذا لم يوجد أي مستخدم ─────────────────────

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Users.Any())
    {
        db.Users.Add(new User
        {
            FullName     = "مدير النظام",
            Email        = "admin@mdar2.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Mdar2@2026")
        });
        db.SaveChanges();
    }
}

await app.RunAsync();
