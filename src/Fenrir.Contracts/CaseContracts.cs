namespace Fenrir.Contracts;

public sealed record CaseCreateRequest(
    string Title,
    string? Description = null,
    string Severity = "Medium",
    string? AssignedTo = null,
    string? CreatedBy = null,
    Guid? EventId = null,
    Guid? IndicatorId = null);

public sealed record CaseUpdateRequest(
    string? Title = null,
    string? Description = null,
    string? Severity = null,
    string? Status = null,
    string? AssignedTo = null,
    string? Summary = null,
    string? Conclusion = null);

public sealed record CaseNoteCreateRequest(string Note, string Author = "analyst");

public sealed record CaseEvidenceCreateRequest(
    string EvidenceType,
    string FileName,
    string StorageReference,
    string? ContentType = null,
    string? Sha256 = null,
    string UploadedBy = "analyst");

public sealed record CaseEventLinkRequest(Guid EventId, string? Reason = null);

public sealed record CaseIndicatorLinkRequest(Guid IndicatorId, string? Reason = null);

public sealed record CaseAssetLinkRequest(string AssetReference, string? Reason = null);

public sealed record CaseUserLinkRequest(string UserReference, string? Reason = null);

public sealed record CaseTimelineItemCreateRequest(
    string ItemType,
    string Title,
    string? Description = null,
    DateTime? OccurredAtUtc = null,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null);

public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string? Description,
    string Severity,
    string Status,
    string? AssignedTo,
    string? CreatedBy,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    string? Summary,
    string? Conclusion,
    IReadOnlyList<CaseNoteDto> Notes,
    IReadOnlyList<CaseEvidenceDto> Evidence,
    IReadOnlyList<CaseEventLinkDto> EventLinks,
    IReadOnlyList<CaseIndicatorLinkDto> IndicatorLinks,
    IReadOnlyList<CaseAssetLinkDto> AssetLinks,
    IReadOnlyList<CaseUserLinkDto> UserLinks,
    IReadOnlyList<CaseTimelineItemDto> TimelineItems);

public sealed record CaseSummaryDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Severity,
    string Status,
    string? AssignedTo,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    int EventCount,
    int IndicatorCount,
    int NoteCount,
    int EvidenceCount);

public sealed record CaseNoteDto(Guid Id, Guid CaseId, string Author, string Note, DateTime CreatedAtUtc);

public sealed record CaseEvidenceDto(
    Guid Id,
    Guid CaseId,
    string EvidenceType,
    string FileName,
    string? ContentType,
    string StorageReference,
    string? Sha256,
    string UploadedBy,
    DateTime CreatedAtUtc);

public sealed record CaseEventLinkDto(Guid Id, Guid CaseId, Guid EventId, string? Reason, DateTime CreatedAtUtc);

public sealed record CaseIndicatorLinkDto(Guid Id, Guid CaseId, Guid IndicatorId, string? Reason, DateTime CreatedAtUtc);

public sealed record CaseAssetLinkDto(Guid Id, Guid CaseId, string AssetReference, string? Reason, DateTime CreatedAtUtc);

public sealed record CaseUserLinkDto(Guid Id, Guid CaseId, string UserReference, string? Reason, DateTime CreatedAtUtc);

public sealed record CaseTimelineItemDto(
    Guid Id,
    Guid CaseId,
    DateTime OccurredAtUtc,
    string ItemType,
    string Title,
    string? Description,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAtUtc);
