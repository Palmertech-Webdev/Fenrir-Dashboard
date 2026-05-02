namespace Fenrir.Domain.Entities;

public class InvestigationReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string ReportType { get; set; } = "InvestigationSummary";
    public string? Scope { get; set; }
    public string RequestedBy { get; set; } = "analyst";
    public string Status { get; set; } = "Queued";
    public Guid? CaseId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string ContentMarkdown { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string SignatureAlgorithm { get; set; } = "SHA256";
}

public class EvidenceIntegrityRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string SignatureAlgorithm { get; set; } = "SHA256";
    public string? Notes { get; set; }
    public string SealedBy { get; set; } = "analyst";
    public DateTime SealedAtUtc { get; set; } = DateTime.UtcNow;
}
