using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;

namespace Fenrir.Application.Services;

public sealed class CaseService(IFenrirDataStore dataStore) : ICaseService
{
    public async Task<CaseDto> CreateAsync(CaseCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var investigationCase = new Case
        {
            CaseNumber = BuildCaseNumber(now),
            Title = NormaliseOrDefault(request.Title, "Untitled investigation"),
            Description = request.Description,
            Severity = NormaliseOrDefault(request.Severity, "Medium"),
            Status = "New",
            AssignedTo = request.AssignedTo,
            CreatedBy = NormaliseOrDefault(request.CreatedBy, "analyst"),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        investigationCase.TimelineItems.Add(new CaseTimelineItem
        {
            CaseId = investigationCase.Id,
            OccurredAtUtc = now,
            ItemType = "case_created",
            Title = "Case created",
            Description = investigationCase.Description,
            CreatedAtUtc = now
        });

        if (request.EventId.HasValue)
        {
            investigationCase.EventLinks.Add(new CaseEventLink
            {
                CaseId = investigationCase.Id,
                EventId = request.EventId.Value,
                Reason = "Initial event used to create case.",
                CreatedAtUtc = now
            });

            investigationCase.TimelineItems.Add(new CaseTimelineItem
            {
                CaseId = investigationCase.Id,
                OccurredAtUtc = now,
                ItemType = "event_linked",
                Title = "Initial SIEM event linked",
                RelatedEntityId = request.EventId.Value,
                RelatedEntityType = "SecurityEvent",
                CreatedAtUtc = now
            });
        }

        if (request.IndicatorId.HasValue)
        {
            investigationCase.IndicatorLinks.Add(new CaseIndicatorLink
            {
                CaseId = investigationCase.Id,
                IndicatorId = request.IndicatorId.Value,
                Reason = "Initial IOC used to create case.",
                CreatedAtUtc = now
            });

            investigationCase.TimelineItems.Add(new CaseTimelineItem
            {
                CaseId = investigationCase.Id,
                OccurredAtUtc = now,
                ItemType = "indicator_linked",
                Title = "Initial IOC linked",
                RelatedEntityId = request.IndicatorId.Value,
                RelatedEntityType = "Indicator",
                CreatedAtUtc = now
            });
        }

        await dataStore.AddCaseAsync(investigationCase, cancellationToken);
        var saved = await dataStore.GetCaseAsync(investigationCase.Id, cancellationToken) ?? investigationCase;
        return saved.ToDto();
    }

    public async Task<IReadOnlyList<CaseSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var cases = await dataStore.ListCasesAsync(cancellationToken);
        return cases.Select(c => c.ToSummaryDto()).ToArray();
    }

    public async Task<CaseDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        return investigationCase?.ToDto();
    }

    public async Task<CaseDto?> UpdateAsync(Guid id, CaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        investigationCase.Title = NormaliseOrDefault(request.Title, investigationCase.Title);
        investigationCase.Description = request.Description ?? investigationCase.Description;
        investigationCase.Severity = NormaliseOrDefault(request.Severity, investigationCase.Severity);
        investigationCase.AssignedTo = request.AssignedTo ?? investigationCase.AssignedTo;
        investigationCase.Summary = request.Summary ?? investigationCase.Summary;
        investigationCase.Conclusion = request.Conclusion ?? investigationCase.Conclusion;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            investigationCase.Status = request.Status.Trim();
            if (IsClosedStatus(investigationCase.Status) && investigationCase.ClosedAtUtc is null)
            {
                investigationCase.ClosedAtUtc = DateTime.UtcNow;
            }
            else if (!IsClosedStatus(investigationCase.Status))
            {
                investigationCase.ClosedAtUtc = null;
            }
        }

        investigationCase.UpdatedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateCaseAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> AddNoteAsync(Guid id, CaseNoteCreateRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        await dataStore.AddCaseNoteAsync(new CaseNote
        {
            CaseId = id,
            Author = NormaliseOrDefault(request.Author, "analyst"),
            Note = NormaliseOrDefault(request.Note, string.Empty),
            CreatedAtUtc = now
        }, cancellationToken);

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = now,
            ItemType = "note_added",
            Title = "Analyst note added",
            Description = request.Note,
            CreatedAtUtc = now
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> AddEvidenceAsync(Guid id, CaseEvidenceCreateRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        await dataStore.AddCaseEvidenceAsync(new CaseEvidence
        {
            CaseId = id,
            EvidenceType = NormaliseOrDefault(request.EvidenceType, "artifact"),
            FileName = NormaliseOrDefault(request.FileName, "evidence"),
            ContentType = request.ContentType,
            StorageReference = NormaliseOrDefault(request.StorageReference, string.Empty),
            Sha256 = request.Sha256,
            UploadedBy = NormaliseOrDefault(request.UploadedBy, "analyst"),
            CreatedAtUtc = now
        }, cancellationToken);

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = now,
            ItemType = "evidence_added",
            Title = "Evidence added",
            Description = request.FileName,
            CreatedAtUtc = now
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> LinkEventAsync(Guid id, CaseEventLinkRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        await dataStore.AddCaseEventLinkAsync(new CaseEventLink
        {
            CaseId = id,
            EventId = request.EventId,
            Reason = request.Reason,
            CreatedAtUtc = now
        }, cancellationToken);

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = now,
            ItemType = "event_linked",
            Title = "SIEM event linked",
            Description = request.Reason,
            RelatedEntityId = request.EventId,
            RelatedEntityType = "SecurityEvent",
            CreatedAtUtc = now
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> LinkIndicatorAsync(Guid id, CaseIndicatorLinkRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        await dataStore.AddCaseIndicatorLinkAsync(new CaseIndicatorLink
        {
            CaseId = id,
            IndicatorId = request.IndicatorId,
            Reason = request.Reason,
            CreatedAtUtc = now
        }, cancellationToken);

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = now,
            ItemType = "indicator_linked",
            Title = "IOC linked",
            Description = request.Reason,
            RelatedEntityId = request.IndicatorId,
            RelatedEntityType = "Indicator",
            CreatedAtUtc = now
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> LinkAssetAsync(Guid id, CaseAssetLinkRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        await dataStore.AddCaseAssetLinkAsync(new CaseAssetLink
        {
            CaseId = id,
            AssetReference = NormaliseOrDefault(request.AssetReference, string.Empty),
            Reason = request.Reason,
            CreatedAtUtc = now
        }, cancellationToken);

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = now,
            ItemType = "asset_linked",
            Title = "Asset linked",
            Description = request.AssetReference,
            CreatedAtUtc = now
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> LinkUserAsync(Guid id, CaseUserLinkRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        await dataStore.AddCaseUserLinkAsync(new CaseUserLink
        {
            CaseId = id,
            UserReference = NormaliseOrDefault(request.UserReference, string.Empty),
            Reason = request.Reason,
            CreatedAtUtc = now
        }, cancellationToken);

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = now,
            ItemType = "user_linked",
            Title = "User linked",
            Description = request.UserReference,
            CreatedAtUtc = now
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<CaseDto?> AddTimelineItemAsync(Guid id, CaseTimelineItemCreateRequest request, CancellationToken cancellationToken)
    {
        var investigationCase = await dataStore.GetCaseAsync(id, cancellationToken);
        if (investigationCase is null)
        {
            return null;
        }

        await dataStore.AddCaseTimelineItemAsync(new CaseTimelineItem
        {
            CaseId = id,
            OccurredAtUtc = request.OccurredAtUtc?.ToUniversalTime() ?? DateTime.UtcNow,
            ItemType = NormaliseOrDefault(request.ItemType, "manual"),
            Title = NormaliseOrDefault(request.Title, "Timeline item"),
            Description = request.Description,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await TouchAsync(investigationCase, cancellationToken);
        return (await dataStore.GetCaseAsync(id, cancellationToken))?.ToDto();
    }

    private async Task TouchAsync(Case investigationCase, CancellationToken cancellationToken)
    {
        investigationCase.UpdatedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateCaseAsync(investigationCase, cancellationToken);
    }

    private static string BuildCaseNumber(DateTime utcNow) => $"CASE-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static string NormaliseOrDefault(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static bool IsClosedStatus(string status) => status.StartsWith("Closed", StringComparison.OrdinalIgnoreCase);
}
