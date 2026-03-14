using System.Security.Claims;

namespace Mdar.API.Extensions;

/// <summary>
/// امتدادات ClaimsPrincipal لاستخراج بيانات المستخدم من JWT Claims.
/// تُستخدم في جميع الـ Controllers للحصول على هوية المستخدم الحالي.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// يستخرج معرّف المستخدم (UserId) من JWT Token.
    ///
    /// يبحث عن Claim في هذا الترتيب:
    ///   1. ClaimTypes.NameIdentifier (النمط الافتراضي لـ ASP.NET Identity)
    ///   2. "sub" (معيار JWT RFC 7519)
    ///
    /// يرمي UnauthorizedAccessException إذا:
    ///   - لم يُعثر على الـ Claim
    ///   - القيمة ليست Guid صالحاً
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// عند غياب الـ Claim أو كون قيمته غير صالحة.
    /// </exception>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var claimValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(claimValue))
            throw new UnauthorizedAccessException(
                "لم يُعثر على معرّف المستخدم في رمز التحقق (JWT). " +
                "تأكد من تضمين Claim 'sub' أو 'NameIdentifier' في التوكن.");

        if (!Guid.TryParse(claimValue, out var userId))
            throw new UnauthorizedAccessException(
                $"قيمة Claim المستخدم '{claimValue}' ليست معرّفاً Guid صالحاً.");

        return userId;
    }

    /// <summary>
    /// يحاول استخراج UserId دون رمي استثناء.
    /// يُعيد null إذا لم يُعثر على الـ Claim أو كانت القيمة غير صالحة.
    /// </summary>
    public static Guid? TryGetUserId(this ClaimsPrincipal principal)
    {
        try { return principal.GetUserId(); }
        catch { return null; }
    }
}
