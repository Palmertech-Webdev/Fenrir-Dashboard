namespace Fenrir.Contracts;

public sealed record InvestigationReportCreateRequest(
    string Title,
    string ReportType = "InvestigationSummary",
    string? Scope = null,
    string RequestedBy = "analyst",
    Guid? CaseId = null,
    bool IncludeFindings = true,
    bool IncludeSiemSummary = true,
    bool IncludeHuntRuns = true,
    bool IncludeResponseRuns = true,
    string? AnalystSummary = null,
    string? Conclusion = null);

public sealed record InvestigationReportDto(
    Guid Id,
    string Title,
    string ReportType,
    string? Scope,
    string RequestedBy,
    string Status,
    Guid? CaseId,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string ContentMarkdown,
    string Sha256,
    string SignatureAlgorithm);

public sealed record EvidenceSealRequest(
    string EntityType,
    string EntityId,
    string Payload,
    string? Notes = null,
    string SealedBy = "analyst");

public sealed record EvidenceVerifyRequest(
    Guid IntegrityRecordId,
    string Payload);

public sealed record EvidenceIntegrityRecordDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Sha256,
    string SignatureAlgorithm,
    string? Notes,
    string SealedBy,
    DateTime SealedAtUtc);

public sealed record EvidenceVerifyResponse(
    Guid IntegrityRecordId,
    bool IsValid,
    string ExpectedSha256,
    string ActualSha256,
    string Summary);
