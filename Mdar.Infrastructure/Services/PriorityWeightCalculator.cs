using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;
using Mdar.Core.Interfaces;
using Mdar.Core.Models.Priority;
using System.Text;

namespace Mdar.Infrastructure.Services;

/// <summary>
/// تنفيذ حاسبة الوزن الأولوي.
///
/// ═══════════════════════════════════════════════════════
///  صيغة الحساب الكاملة:
///
///  PriorityWeight = (BaseScore × PrayerMultiplier)
///                + UrgencyBoost
///                + AgeBoost
///                + PomodoroBonus
///
/// ─── BaseScore (بحسب الأولوية) ───────────────────────
///   Critical = 1000 | High = 750 | Medium = 500 | Low = 250
///
/// ─── PrayerMultiplier (بحسب توافق الفترة) ────────────
///   تطابق تام  = 1.50  (الأنسب لهذه الفترة)
///   فترة مجاورة = 1.20  (قريبة جداً)
///   لا تفضيل   = 1.00  (مهمة Anywhere-Time)
///   غير متوافق = 0.85  (أجّلها لفترتها الأنسب)
///
/// ─── UrgencyBoost (بحسب الموعد النهائي) ──────────────
///   متأخرة     = 500 + (أيام_التأخر × 30) بحد أقصى 800
///   اليوم      = 400
///   غداً       = 250
///   2-3 أيام   = 150
///   4-7 أيام   = 75
///   8-30 يوماً = 25
///   أكثر/بلا   = 0
///
/// ─── AgeBoost (منع تجويع المهام القديمة) ─────────────
///   = أيام_الانتظار × 1.5 بحد أقصى 75
///
/// ─── PomodoroBonus ────────────────────────────────────
///   متوافق + فترة تركيز عالية  = 60  (AfterFajr / Duha)
///   متوافق + فترة تركيز متوسطة = 30  (AfterDhuhr / AfterAsr)
///   متوافق + فترة تركيز منخفضة = 10  (AfterMaghrib / AfterIsha)
///   غير متوافق مع الطماطم      = 0
/// ═══════════════════════════════════════════════════════
/// </summary>
internal sealed class PriorityWeightCalculator : IPriorityWeightCalculator
{
    // ─── ثوابت BaseScore ──────────────────────────────────────────────────────
    private const double BaseScoreCritical = 1000;
    private const double BaseScoreHigh     = 750;
    private const double BaseScoreMedium   = 500;
    private const double BaseScoreLow      = 250;

    // ─── ثوابت PrayerMultiplier ───────────────────────────────────────────────
    private const double MultiplierExactMatch = 1.50;
    private const double MultiplierAdjacent   = 1.20;
    private const double MultiplierNoPref     = 1.00;
    private const double MultiplierMismatch   = 0.85;

    // ─── ثوابت UrgencyBoost ───────────────────────────────────────────────────
    private const double UrgencyOverdueBase   = 500;
    private const double UrgencyOverduePerDay = 30;
    private const double UrgencyOverdueMax    = 800;
    private const double UrgencyDueToday      = 400;
    private const double UrgencyDueTomorrow   = 250;
    private const double UrgencyDue2to3Days   = 150;
    private const double UrgencyDue4to7Days   = 75;
    private const double UrgencyDue8to30Days  = 25;

    // ─── ثوابت AgeBoost ───────────────────────────────────────────────────────
    private const double AgeBoostPerDay = 1.5;
    private const double AgeBoostMax    = 75;

    // ─── ثوابت PomodoroBonus ──────────────────────────────────────────────────
    private const double PomodoroHighFocus = 60;
    private const double PomodoroMedFocus  = 30;
    private const double PomodoroLowFocus  = 10;

    /// <summary>
    /// ترتيب الفترات بالأرقام لحساب "المجاورة".
    /// فترتان متجاورتان = فرق مطلق بين رقمَيهما يساوي 1.
    /// </summary>
    private static readonly IReadOnlyDictionary<PrayerPeriod, int> PeriodOrder =
        new Dictionary<PrayerPeriod, int>
        {
            [PrayerPeriod.AfterFajr]    = 0,
            [PrayerPeriod.Duha]         = 1,
            [PrayerPeriod.AfterDhuhr]   = 2,
            [PrayerPeriod.AfterAsr]     = 3,
            [PrayerPeriod.AfterMaghrib] = 4,
            [PrayerPeriod.AfterIsha]    = 5
        };

    /// <summary>
    /// مستوى التركيز لكل فترة لاحتساب PomodoroBonus.
    /// 3=عالٍ | 2=متوسط | 1=منخفض
    /// </summary>
    private static readonly IReadOnlyDictionary<PrayerPeriod, int> PeriodFocusLevel =
        new Dictionary<PrayerPeriod, int>
        {
            [PrayerPeriod.AfterFajr]    = 3, // سكون الفجر — أعمق تركيز
            [PrayerPeriod.Duha]         = 3, // ذروة الطاقة الصباحية
            [PrayerPeriod.AfterDhuhr]   = 2, // متوسط — بعد الغداء
            [PrayerPeriod.AfterAsr]     = 2, // موجة طاقة ثانية
            [PrayerPeriod.AfterMaghrib] = 1, // بداية التراجع
            [PrayerPeriod.AfterIsha]    = 1  // وقت الراحة والمراجعة
        };

    /// <inheritdoc />
    public TaskWeightBreakdown Calculate(TaskItem task, PrayerPeriod currentPeriod, DateTime asOf)
    {
        // ── 1. Base Score ──────────────────────────────────────────────────────
        double baseScore = task.Priority switch
        {
            TaskPriority.Critical => BaseScoreCritical,
            TaskPriority.High     => BaseScoreHigh,
            TaskPriority.Medium   => BaseScoreMedium,
            TaskPriority.Low      => BaseScoreLow,
            _                     => BaseScoreLow
        };

        // ── 2. Prayer Period Multiplier ────────────────────────────────────────
        double prayerMultiplier = ComputePrayerMultiplier(task.PreferredPrayerPeriod, currentPeriod);
        double weightedBase = baseScore * prayerMultiplier;

        // ── 3. Urgency Boost ───────────────────────────────────────────────────
        double urgencyBoost = ComputeUrgencyBoost(task.DueDate, asOf);

        // ── 4. Age Boost (Anti-Starvation) ────────────────────────────────────
        double ageBoost = ComputeAgeBoost(task.CreatedAt, asOf);

        // ── 5. Pomodoro Bonus ──────────────────────────────────────────────────
        double pomodoroBonus = ComputePomodoroBonus(task.IsPomodoroCompatible, currentPeriod);

        // ── 6. Total ───────────────────────────────────────────────────────────
        double total = weightedBase + urgencyBoost + ageBoost + pomodoroBonus;

        return new TaskWeightBreakdown
        {
            BaseScore              = baseScore,
            PrayerPeriodMultiplier = prayerMultiplier,
            WeightedBaseScore      = weightedBase,
            UrgencyBoost           = urgencyBoost,
            AgeBoost               = ageBoost,
            PomodoroBonus          = pomodoroBonus,
            TotalWeight            = total,
            Explanation            = BuildExplanation(task, prayerMultiplier, urgencyBoost, pomodoroBonus, currentPeriod)
        };
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// يحسب معامل ضرب فترة الصلاة.
    /// المنطق: كلما كانت الفترة أقرب لتفضيل المهمة، كان المعامل أعلى.
    /// </summary>
    private static double ComputePrayerMultiplier(PrayerPeriod? preferred, PrayerPeriod current)
    {
        // لا تفضيل → حيادي
        if (preferred is null)
            return MultiplierNoPref;

        // تطابق تام
        if (preferred.Value == current)
            return MultiplierExactMatch;

        // فترة مجاورة (±1 في الترتيب الزمني)
        int prefOrder = PeriodOrder[preferred.Value];
        int currOrder = PeriodOrder[current];
        if (Math.Abs(prefOrder - currOrder) == 1)
            return MultiplierAdjacent;

        // غير متوافق → تخفيض طفيف (لا نخفيها كلياً، فقط نخفّض وزنها)
        return MultiplierMismatch;
    }

    /// <summary>
    /// يحسب مكافأة الإلحاحية بناءً على المسافة الزمنية من الموعد النهائي.
    /// الإلحاحية تتصاعد بشكل غير خطي: القفزة الكبرى عند "اليوم" و"متأخرة".
    /// </summary>
    private static double ComputeUrgencyBoost(DateOnly? dueDate, DateTime asOf)
    {
        if (dueDate is null)
            return 0;

        var today = DateOnly.FromDateTime(asOf);
        int daysUntilDue = dueDate.Value.DayNumber - today.DayNumber;

        return daysUntilDue switch
        {
            < 0  => Math.Min(UrgencyOverdueBase + (Math.Abs(daysUntilDue) * UrgencyOverduePerDay), UrgencyOverdueMax),
            0    => UrgencyDueToday,
            1    => UrgencyDueTomorrow,
            <= 3 => UrgencyDue2to3Days,
            <= 7 => UrgencyDue4to7Days,
            <= 30 => UrgencyDue8to30Days,
            _    => 0
        };
    }

    /// <summary>
    /// يحسب مكافأة العمر لمنع "تجويع" المهام القديمة.
    /// مهمة عمرها 50 يوماً دون إنجاز تستحق أولوية إضافية.
    /// الحد الأقصى 75 نقطة حتى لا يطغى العمر على الأولوية الحقيقية.
    /// </summary>
    private static double ComputeAgeBoost(DateTime createdAt, DateTime asOf)
    {
        double daysPending = (asOf - createdAt).TotalDays;
        return Math.Min(daysPending * AgeBoostPerDay, AgeBoostMax);
    }

    /// <summary>
    /// يحسب مكافأة الطماطم بناءً على مستوى تركيز الفترة الحالية.
    /// المهام غير المتوافقة مع الطماطم لا تستفيد من مكافأة التركيز.
    /// </summary>
    private static double ComputePomodoroBonus(bool isCompatible, PrayerPeriod currentPeriod)
    {
        if (!isCompatible)
            return 0;

        return PeriodFocusLevel[currentPeriod] switch
        {
            3 => PomodoroHighFocus,
            2 => PomodoroMedFocus,
            _ => PomodoroLowFocus
        };
    }

    /// <summary>
    /// يبني تفسيراً نصياً إنسانياً لأبرز عوامل الترتيب.
    /// يُعرض للمستخدم حين يسأل "لماذا هذه المهمة أولاً؟"
    /// </summary>
    private static string BuildExplanation(
        TaskItem task,
        double prayerMultiplier,
        double urgencyBoost,
        double pomodoroBonus,
        PrayerPeriod currentPeriod)
    {
        var parts = new List<string>();

        // الأولوية الأساسية
        parts.Add(task.Priority switch
        {
            TaskPriority.Critical => "أولوية حرجة",
            TaskPriority.High     => "أولوية عالية",
            TaskPriority.Medium   => "أولوية متوسطة",
            TaskPriority.Low      => "أولوية منخفضة",
            _                     => "أولوية منخفضة"
        });

        // فترة الصلاة
        if (prayerMultiplier >= MultiplierExactMatch)
            parts.Add($"مثالية لفترة {GetPeriodNameAr(currentPeriod)} ×1.5");
        else if (prayerMultiplier >= MultiplierAdjacent)
            parts.Add($"مناسبة لفترة {GetPeriodNameAr(currentPeriod)} ×1.2");
        else if (prayerMultiplier <= MultiplierMismatch)
            parts.Add($"أنسب لفترة {GetPeriodNameAr(task.PreferredPrayerPeriod!.Value)}");

        // الإلحاحية
        if (urgencyBoost >= UrgencyOverdueBase)
            parts.Add("⚠ متأخرة عن موعدها");
        else if (urgencyBoost >= UrgencyDueToday)
            parts.Add("موعدها اليوم");
        else if (urgencyBoost >= UrgencyDueTomorrow)
            parts.Add("موعدها غداً");

        // الطماطم
        if (pomodoroBonus >= PomodoroHighFocus)
            parts.Add("مناسبة للتركيز العميق الآن");

        return string.Join(" | ", parts);
    }

    private static string GetPeriodNameAr(PrayerPeriod period) => period switch
    {
        PrayerPeriod.AfterFajr    => "بعد الفجر",
        PrayerPeriod.Duha         => "الضحى",
        PrayerPeriod.AfterDhuhr   => "بعد الظهر",
        PrayerPeriod.AfterAsr     => "بعد العصر",
        PrayerPeriod.AfterMaghrib => "بعد المغرب",
        PrayerPeriod.AfterIsha    => "بعد العشاء",
        _                         => period.ToString()
    };
}
