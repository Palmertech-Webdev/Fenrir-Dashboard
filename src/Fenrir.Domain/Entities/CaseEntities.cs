namespace Fenrir.Domain.Entities;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "New";
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public string? Summary { get; set; }
    public string? Conclusion { get; set; }

    public List<CaseNote> Notes { get; set; } = [];
    public List<CaseEvidence> Evidence { get; set; } = [];
    public List<CaseEventLink> EventLinks { get; set; } = [];
    public List<CaseIndicatorLink> IndicatorLinks { get; set; } = [];
    public List<CaseAssetLink> AssetLinks { get; set; } = [];
    public List<CaseUserLink> UserLinks { get; set; } = [];
    public List<CaseTimelineItem> TimelineItems { get; set; } = [];
}

public class CaseNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string Author { get; set; } = "analyst";
    public string Note { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}

public class CaseEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string EvidenceType { get; set; } = "artifact";
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    public string StorageReference { get; set; } = "";
    public string? Sha256 { get; set; }
    public string UploadedBy { get; set; } = "analyst";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}

public class CaseEventLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Guid EventId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}

public class CaseIndicatorLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Guid IndicatorId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}

public class CaseAssetLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string AssetReference { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}

public class CaseUserLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string UserReference { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}

public class CaseTimelineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string ItemType { get; set; } = "note";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}
