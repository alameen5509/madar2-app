using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.Sync;

// ── Push (Client → Server) ──────────────────────────────────────────────────

/// <summary>طلب رفع التغييرات منذ آخر مزامنة</summary>
public class PushSyncRequest
{
    /// <summary>معرّف اللوحة</summary>
    [Required]
    public Guid   BoardId    { get; set; }

    /// <summary>معرّف جلسة العميل — لمنع استقبال الأحداث الخاصة به</summary>
    [Required]
    [MaxLength(64)]
    public string SessionId  { get; set; } = string.Empty;

    /// <summary>وقت آخر مزامنة ناجحة</summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>قائمة التغييرات المرتبة زمنياً</summary>
    [Required]
    public List<SyncChangeDto> Changes { get; set; } = [];
}

/// <summary>حدث تغيير واحد في اللوحة</summary>
public class SyncChangeDto
{
    /// <summary>node_created | node_deleted | node_moved | text_changed | connection_added</summary>
    [Required]
    [MaxLength(30)]
    public string   Type      { get; set; } = string.Empty;

    /// <summary>معرّف البطاقة المتأثرة</summary>
    public string?  NodeId    { get; set; }

    /// <summary>وقت الحدث على العميل</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ── node_moved ──
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? W { get; set; }
    public double? H { get; set; }

    // ── text_changed ──
    public string? Title   { get; set; }
    public string? Content { get; set; }

    // ── node_created ──
    public NodeDataDto? Node { get; set; }
}

/// <summary>بيانات بطاقة جديدة عند الإنشاء</summary>
public class NodeDataDto
{
    public string?  Title    { get; set; }
    public string?  Content  { get; set; }
    public int      CardType { get; set; }
    public double   X        { get; set; }
    public double   Y        { get; set; }
    public double   W        { get; set; } = 240;
    public double   H        { get; set; } = 160;
}

/// <summary>استجابة رفع التغييرات</summary>
public record PushSyncResponse(
    int      AcceptedCount,
    int      SkippedCount,
    DateTime ServerTime
);

// ── Pull (Server → Client) ──────────────────────────────────────────────────

/// <summary>استجابة جلب التغييرات منذ تاريخ معين</summary>
public class PullSyncResponse
{
    public List<SyncEventResponse> Changes   { get; set; } = [];
    public DateTime                ServerTime { get; set; } = DateTime.UtcNow;
}

/// <summary>حدث واحد من السيرفر</summary>
public class SyncEventResponse
{
    public Guid     Id        { get; set; }
    public string   EventType { get; set; } = string.Empty;
    public string   Payload   { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid     UserId    { get; set; }
    public string?  SessionId { get; set; }
}
