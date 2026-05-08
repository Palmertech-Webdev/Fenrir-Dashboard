using System.Text.Json;
using System.Text.RegularExpressions;
using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Application.Siem.Parsing;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed partial class SiemService(IFenrirDataStore dataStore, ISiemParserRegistry parserRegistry) : ISiemService, ISiemIngestionWorker
{
    private const string DefaultParser = "generic_json_v1";
    private static readonly JsonSerializerOptions QueueJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SiemEventIngestResponse> IngestAsync(SiemEventRequest request, CancellationToken cancellationToken)
    {
        var rawInput = new SiemRawEventInput(
            ParserName: DefaultParser,
            SourceId: null,
            SourceName: request.Source,
            Vendor: null,
            Product: null,
            RawJson: request.Raw,
            RawText: request.Message,
            ReceivedAtUtc: DateTime.UtcNow);

        var securityEvent = await ParseSecurityEventAsync(rawInput, request, request.Source, DefaultParser, cancellationToken);
        await dataStore.AddSecurityEventAsync(securityEvent, cancellationToken);

        var findings = await CreateFindingsForEventAsync(securityEvent, cancellationToken);
        return new SiemEventIngestResponse(securityEvent.ToDto(), findings.Select(finding => finding.ToDto()).ToArray());
    }

    public async Task<SiemBatchIngestResponse> IngestBatchAsync(SiemBatchIngestRequest request, CancellationToken cancellationToken)
    {
        var queuedAtUtc = DateTime.UtcNow;
        var sourceName = string.IsNullOrWhiteSpace(request.Source) ? "Manual Upload" : request.Source.Trim();
        var parser = string.IsNullOrWhiteSpace(request.Parser) ? DefaultParser : request.Parser.Trim();

        if (request.SourceId.HasValue)
        {
            var source = await dataStore.GetSiemLogSourceAsync(request.SourceId.Value, cancellationToken);
            if (source is not null)
            {
                sourceName = source.Name;
                parser = string.IsNullOrWhiteSpace(request.Parser) ? source.Parser : parser;
            }
        }

        var job = new SiemIngestionJob
        {
            SourceId = request.SourceId,
            CaseId = request.CaseId,
            SourceName = sourceName,
            InputType = string.IsNullOrWhiteSpace(request.InputType) ? "json" : request.InputType.Trim(),
            Parser = parser,
            Status = "Queued",
            EventsReceived = request.Events.Count,
            StartedAtUtc = queuedAtUtc
        };

        await dataStore.AddSiemIngestionJobAsync(job, cancellationToken);

        await dataStore.AddSiemRawIngestionBatchAsync(new SiemRawIngestionBatch
        {
            JobId = job.Id,
            SourceId = request.SourceId,
            CaseId = request.CaseId,
            SourceName = sourceName,
            InputType = job.InputType,
            Parser = parser,
            Status = "Queued",
            EventsReceived = request.Events.Count,
            PayloadJson = JsonSerializer.Serialize(request.Events, QueueJsonOptions),
            CreatedAtUtc = queuedAtUtc
        }, cancellationToken);

        return new SiemBatchIngestResponse(job.ToDto(), 0, 0, []);
    }

    public async Task<bool> ProcessNextQueuedBatchAsync(CancellationToken cancellationToken)
    {
        var batch = await dataStore.ClaimNextQueuedSiemRawIngestionBatchAsync(cancellationToken);
        if (batch is null)
        {
            return false;
        }

        var job = await dataStore.GetSiemIngestionJobAsync(batch.JobId, cancellationToken);
        if (job is null)
        {
            batch.Status = "Failed";
            batch.CompletedAtUtc = DateTime.UtcNow;
            batch.LastError = "Queued batch referenced a missing ingestion job.";
            await dataStore.UpdateSiemRawIngestionBatchAsync(batch, cancellationToken);
            return true;
        }

        try
        {
            var events = JsonSerializer.Deserialize<IReadOnlyList<SiemEventRequest>>(batch.PayloadJson, QueueJsonOptions) ?? [];
            var request = new SiemBatchIngestRequest(
                Source: batch.SourceName,
                InputType: batch.InputType,
                Parser: batch.Parser,
                SourceId: batch.SourceId,
                CaseId: batch.CaseId,
                Events: events);

            await ProcessBatchAsync(request, job, batch.ProcessingStartedAtUtc ?? DateTime.UtcNow, cancellationToken);

            batch.Status = job.Status is "completed" or "partially_parsed" ? "Completed" : "Failed";
            batch.CompletedAtUtc = job.CompletedAtUtc ?? DateTime.UtcNow;
            batch.LastError = job.ErrorSummary;
            await dataStore.UpdateSiemRawIngestionBatchAsync(batch, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.EventsFailed = job.EventsReceived;
            job.ErrorSummary = ex.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            await dataStore.UpdateSiemIngestionJobAsync(job, cancellationToken);

            batch.Status = "Failed";
            batch.CompletedAtUtc = job.CompletedAtUtc;
            batch.LastError = ex.Message;
            await dataStore.UpdateSiemRawIngestionBatchAsync(batch, cancellationToken);
            return true;
        }
    }

    private async Task ProcessBatchAsync(SiemBatchIngestRequest request, SiemIngestionJob job, DateTime batchStartedAtUtc, CancellationToken cancellationToken)
    {
        var sourceName = string.IsNullOrWhiteSpace(request.Source) ? job.SourceName : request.Source.Trim();
        var parser = string.IsNullOrWhiteSpace(request.Parser) ? job.Parser : request.Parser.Trim();
        SiemLogSource? source = null;

        if (request.SourceId.HasValue)
        {
            source = await dataStore.GetSiemLogSourceAsync(request.SourceId.Value, cancellationToken);
            if (source is not null)
            {
                sourceName = source.Name;
                parser = string.IsNullOrWhiteSpace(request.Parser) ? source.Parser : parser;
            }
        }

        job.Status = "Processing";
        job.SourceName = sourceName;
        job.InputType = string.IsNullOrWhiteSpace(request.InputType) ? "json" : request.InputType.Trim();
        job.Parser = parser;
        job.EventsReceived = request.Events.Count;
        job.StartedAtUtc = batchStartedAtUtc;
        await dataStore.UpdateSiemIngestionJobAsync(job, cancellationToken);

        var acceptedEvents = new List<SecurityEvent>();
        var findings = new List<Finding>();
        var failed = 0;

        foreach (var eventRequest in request.Events)
        {
            try
            {
                var rawInput = new SiemRawEventInput(
                    ParserName: parser,
                    SourceId: request.SourceId,
                    SourceName: sourceName,
                    Vendor: source?.Vendor,
                    Product: source?.Product,
                    RawJson: eventRequest.Raw,
                    RawText: eventRequest.Message,
                    ReceivedAtUtc: DateTime.UtcNow);

                var securityEvent = await ParseSecurityEventAsync(rawInput, eventRequest, sourceName, parser, cancellationToken);
                acceptedEvents.Add(securityEvent);
            }
            catch
            {
                failed++;
            }
        }

        if (acceptedEvents.Count > 0)
        {
            await dataStore.AddSecurityEventsAsync(acceptedEvents, cancellationToken);

            foreach (var securityEvent in acceptedEvents)
            {
                findings.AddRange(await CreateFindingsForEventAsync(securityEvent, cancellationToken));
            }
        }

        var completedAtUtc = DateTime.UtcNow;
        job.EventsParsed = acceptedEvents.Count;
        job.EventsFailed = failed;
        job.Status = failed == 0 ? "completed" : acceptedEvents.Count > 0 ? "partially_parsed" : "failed";
        job.ErrorSummary = failed == 0 ? null : $"{failed} event(s) could not be parsed or normalised.";
        job.CompletedAtUtc = completedAtUtc;
        await dataStore.UpdateSiemIngestionJobAsync(job, cancellationToken);

        if (request.SourceId.HasValue)
        {
            source ??= await dataStore.GetSiemLogSourceAsync(request.SourceId.Value, cancellationToken);
            if (source is not null)
            {
                var lastEventTimestampUtc = acceptedEvents.Count > 0
                    ? acceptedEvents.Max(securityEvent => securityEvent.TimestampUtc)
                    : source.State?.LastEventTimestampUtc;
                var lagSeconds = lastEventTimestampUtc.HasValue
                    ? Math.Max(0, (int)Math.Round((completedAtUtc - lastEventTimestampUtc.Value).TotalSeconds))
                    : 0;
                var latencyMs = Math.Max(0, (int)Math.Round((completedAtUtc - batchStartedAtUtc).TotalMilliseconds));
                var parseFailureRate = request.Events.Count == 0 ? 0 : Math.Round((double)failed / request.Events.Count, 4);
                var status = ResolveSourceHealthStatus(source.IsEnabled, request.Events.Count, failed, acceptedEvents.Count);

                source.LastSeenAtUtc = completedAtUtc;
                source.LastSuccessfulIngestAtUtc = acceptedEvents.Count > 0 ? completedAtUtc : source.LastSuccessfulIngestAtUtc;
                source.Status = status;
                source.UpdatedAtUtc = completedAtUtc;
                await dataStore.UpdateSiemLogSourceAsync(source, cancellationToken);

                await dataStore.UpsertSiemSourceStateAsync(new SiemSourceState
                {
                    SourceId = source.Id,
                    ConnectorState = status,
                    LastPollStartedAtUtc = batchStartedAtUtc,
                    LastPollCompletedAtUtc = completedAtUtc,
                    LastEventTimestampUtc = lastEventTimestampUtc,
                    ConsecutiveFailureCount = failed == 0 ? 0 : (source.State?.ConsecutiveFailureCount ?? 0) + 1,
                    LastError = failed == 0 ? null : $"{failed} event(s) failed parsing during batch ingest."
                }, cancellationToken);

                await dataStore.AddSiemSourceHealthSnapshotAsync(new SiemSourceHealthSnapshot
                {
                    SourceId = source.Id,
                    CapturedAtUtc = completedAtUtc,
                    Status = status,
                    LastPollAtUtc = completedAtUtc,
                    LastSuccessfulIngestAtUtc = acceptedEvents.Count > 0 ? completedAtUtc : source.LastSuccessfulIngestAtUtc,
                    EventsReceivedLastInterval = request.Events.Count,
                    EventsParsedLastInterval = acceptedEvents.Count,
                    EventsFailedLastInterval = failed,
                    EventsReceivedLast15Minutes = request.Events.Count,
                    EventsParsedLast15Minutes = acceptedEvents.Count,
                    EventsFailedLast15Minutes = failed,
                    ParseFailureRate = parseFailureRate,
                    AverageIngestLatencyMs = latencyMs,
                    LagSeconds = lagSeconds,
                    QueueBacklog = 0,
                    LastError = failed == 0 ? null : $"{failed} event(s) failed parsing during batch ingest.",
                    Message = failed == 0 ? "Queued batch processed successfully." : $"Queued batch processed with {failed} failed event(s)."
                }, cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<SiemEventDto>> ListAsync(string? source, string? host, string? severity, CancellationToken cancellationToken)
    {
        var events = await dataStore.ListSecurityEventsAsync(source, host, severity, cancellationToken);
        return events.Select(securityEvent => securityEvent.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<SiemEventDto>> SearchAsync(SiemEventSearchRequest request, CancellationToken cancellationToken)
    {
        var events = await dataStore.SearchSecurityEventsAsync(
            request.Source,
            request.Host,
            request.Severity,
            request.EventType,
            request.UserName,
            request.IpAddress,
            request.Indicator,
            request.EventCategory,
            request.Domain,
            request.FileHashSha256,
            request.CloudAction,
            request.SourceId,
            request.SourceIp,
            request.DestinationIp,
            request.FromUtc,
            request.ToUtc,
            request.Take,
            cancellationToken);

        return events.Select(securityEvent => securityEvent.ToDto()).ToArray();
    }

    public async Task<SiemSourceDto> RegisterSourceAsync(SiemSourceRegistrationRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var existing = await dataStore.GetSiemLogSourceByNameAsync(name, cancellationToken);
        SiemLogSource source;

        if (existing is null)
        {
            source = new SiemLogSource
            {
                Name = name,
                SourceType = NormaliseOrDefault(request.SourceType, "manual_upload"),
                Vendor = NormaliseOrDefault(request.Vendor, "generic"),
                Product = NormaliseOrDefault(request.Product, "generic"),
                ConnectionType = NormaliseOrDefault(request.ConnectionType, "manual"),
                Parser = NormaliseOrDefault(request.Parser, DefaultParser),
                Description = request.Description,
                IsEnabled = request.IsEnabled,
                Status = request.IsEnabled ? "Healthy" : "Disabled"
            };

            await dataStore.AddSiemLogSourceAsync(source, cancellationToken);
        }
        else
        {
            source = existing;
            source.SourceType = NormaliseOrDefault(request.SourceType, source.SourceType);
            source.Vendor = NormaliseOrDefault(request.Vendor, source.Vendor);
            source.Product = NormaliseOrDefault(request.Product, source.Product);
            source.ConnectionType = NormaliseOrDefault(request.ConnectionType, source.ConnectionType);
            source.Parser = NormaliseOrDefault(request.Parser, source.Parser);
            source.Description = request.Description;
            source.IsEnabled = request.IsEnabled;
            source.Status = request.IsEnabled ? source.Status == "Disabled" ? "Healthy" : source.Status : "Disabled";
            source.UpdatedAtUtc = DateTime.UtcNow;
            await dataStore.UpdateSiemLogSourceAsync(source, cancellationToken);
        }

        if (request.Config is not null)
        {
            await dataStore.UpsertSiemSourceConfigAsync(BuildSourceConfig(source.Id, request.Config, source.Config), cancellationToken);
        }

        if (request.SecretRefs is not null)
        {
            foreach (var secretRef in request.SecretRefs)
            {
                await dataStore.UpsertSiemSourceSecretRefAsync(BuildSecretRef(source.Id, secretRef), cancellationToken);
            }
        }

        await dataStore.UpsertSiemSourceStateAsync(new SiemSourceState
        {
            SourceId = source.Id,
            ConnectorState = source.IsEnabled ? "Ready" : "Disabled",
            ConsecutiveFailureCount = 0
        }, cancellationToken);

        var saved = await dataStore.GetSiemLogSourceAsync(source.Id, cancellationToken) ?? source;
        return saved.ToDto();
    }

    public async Task<IReadOnlyList<SiemSourceDto>> ListSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await dataStore.ListSiemLogSourcesAsync(cancellationToken);
        return sources.Select(source => source.ToDto()).ToArray();
    }

    public async Task<SiemSourceDto?> GetSourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        return source?.ToDto();
    }

    public async Task<SiemSourceDto?> UpdateSourceAsync(Guid id, SiemSourceUpdateRequest request, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        source.Name = NormaliseOrDefault(request.Name, source.Name);
        source.SourceType = NormaliseOrDefault(request.SourceType, source.SourceType);
        source.Vendor = NormaliseOrDefault(request.Vendor, source.Vendor);
        source.Product = NormaliseOrDefault(request.Product, source.Product);
        source.ConnectionType = NormaliseOrDefault(request.ConnectionType, source.ConnectionType);
        source.Parser = NormaliseOrDefault(request.Parser, source.Parser);
        source.Description = request.Description ?? source.Description;

        if (request.IsEnabled.HasValue)
        {
            source.IsEnabled = request.IsEnabled.Value;
            source.Status = source.IsEnabled ? source.Status == "Disabled" ? "Healthy" : source.Status : "Disabled";
        }

        source.Status = NormaliseOrDefault(request.Status, source.Status);
        source.UpdatedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateSiemLogSourceAsync(source, cancellationToken);

        if (request.IsEnabled.HasValue)
        {
            await dataStore.UpsertSiemSourceStateAsync(new SiemSourceState
            {
                SourceId = source.Id,
                ConnectorState = source.IsEnabled ? "Ready" : "Disabled",
                ConsecutiveFailureCount = source.State?.ConsecutiveFailureCount ?? 0,
                LastError = source.IsEnabled ? source.State?.LastError : null
            }, cancellationToken);
        }

        return (await dataStore.GetSiemLogSourceAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<SiemSourceDto?> UpdateSourceConfigAsync(Guid id, SiemSourceConfigRequest request, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        await dataStore.UpsertSiemSourceConfigAsync(BuildSourceConfig(id, request, source.Config), cancellationToken);
        return (await dataStore.GetSiemLogSourceAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<SiemSourceDto?> AddOrUpdateSecretRefAsync(Guid id, SiemSourceSecretRefRequest request, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        await dataStore.UpsertSiemSourceSecretRefAsync(BuildSecretRef(id, request), cancellationToken);
        return (await dataStore.GetSiemLogSourceAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<SiemSourceDto?> RemoveSecretRefAsync(Guid id, string secretPurpose, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        await dataStore.RemoveSiemSourceSecretRefAsync(id, secretPurpose, cancellationToken);
        return (await dataStore.GetSiemLogSourceAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<SiemSourceDto?> UpdateSourceStateAsync(Guid id, SiemSourceStateRequest request, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        await dataStore.UpsertSiemSourceStateAsync(new SiemSourceState
        {
            SourceId = id,
            ConnectorState = NormaliseOrDefault(request.ConnectorState, source.State?.ConnectorState ?? "Ready"),
            CursorValue = request.CursorValue ?? source.State?.CursorValue,
            LastPollStartedAtUtc = request.LastPollStartedAtUtc ?? source.State?.LastPollStartedAtUtc,
            LastPollCompletedAtUtc = request.LastPollCompletedAtUtc ?? source.State?.LastPollCompletedAtUtc,
            LastEventTimestampUtc = request.LastEventTimestampUtc ?? source.State?.LastEventTimestampUtc,
            NextPollAfterUtc = request.NextPollAfterUtc ?? source.State?.NextPollAfterUtc,
            ConsecutiveFailureCount = request.ConsecutiveFailureCount ?? source.State?.ConsecutiveFailureCount ?? 0,
            LastError = request.LastError ?? source.State?.LastError
        }, cancellationToken);

        return (await dataStore.GetSiemLogSourceAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<SiemSourceDto?> AddHealthSnapshotAsync(Guid id, SiemSourceHealthSnapshotRequest request, CancellationToken cancellationToken)
    {
        var source = await dataStore.GetSiemLogSourceAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var status = NormaliseOrDefault(request.Status, source.Status);
        await dataStore.AddSiemSourceHealthSnapshotAsync(new SiemSourceHealthSnapshot
        {
            SourceId = id,
            Status = status,
            LastPollAtUtc = request.LastPollAtUtc,
            LastSuccessfulIngestAtUtc = request.LastSuccessfulIngestAtUtc,
            EventsReceivedLastInterval = request.EventsReceivedLastInterval,
            EventsParsedLastInterval = request.EventsParsedLastInterval,
            EventsFailedLastInterval = request.EventsFailedLastInterval,
            EventsReceivedLast15Minutes = request.EventsReceivedLast15Minutes,
            EventsParsedLast15Minutes = request.EventsParsedLast15Minutes,
            EventsFailedLast15Minutes = request.EventsFailedLast15Minutes,
            ParseFailureRate = request.ParseFailureRate,
            AverageIngestLatencyMs = request.AverageIngestLatencyMs,
            LagSeconds = request.LagSeconds,
            QueueBacklog = request.QueueBacklog,
            LastError = request.LastError,
            Message = request.Message
        }, cancellationToken);

        source.Status = status;
        source.LastSeenAtUtc = request.LastPollAtUtc ?? source.LastSeenAtUtc;
        source.LastSuccessfulIngestAtUtc = request.LastSuccessfulIngestAtUtc ?? source.LastSuccessfulIngestAtUtc;
        source.UpdatedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateSiemLogSourceAsync(source, cancellationToken);

        return (await dataStore.GetSiemLogSourceAsync(id, cancellationToken))?.ToDto();
    }

    public async Task<IReadOnlyList<SiemIngestionJobDto>> ListIngestionJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await dataStore.ListSiemIngestionJobsAsync(cancellationToken);
        return jobs.Select(job => job.ToDto()).ToArray();
    }

    public async Task<SiemIngestionJobDto?> GetIngestionJobAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dataStore.GetSiemIngestionJobAsync(id, cancellationToken);
        return job?.ToDto();
    }

    private async Task<SecurityEvent> ParseSecurityEventAsync(SiemRawEventInput rawInput, SiemEventRequest fallbackRequest, string fallbackSource, string parserName, CancellationToken cancellationToken)
    {
        var parser = parserRegistry.GetParser(parserName);
        var parsed = await parser.ParseAsync(rawInput, cancellationToken);

        if (parsed is null)
        {
            parsed = await parserRegistry.GetParser(DefaultParser).ParseAsync(rawInput with { ParserName = DefaultParser }, cancellationToken);
        }

        if (parsed is null)
        {
            return BuildFallbackSecurityEvent(fallbackRequest, fallbackSource, parserName);
        }

        return new SecurityEvent
        {
            TimestampUtc = parsed.TimestampUtc,
            SourceId = parsed.SourceId,
            Source = string.IsNullOrWhiteSpace(parsed.SourceName) ? fallbackSource : parsed.SourceName,
            SourceName = parsed.SourceName,
            Vendor = parsed.Vendor,
            Product = parsed.Product,
            Host = string.IsNullOrWhiteSpace(parsed.Host) ? "unknown" : parsed.Host,
            EventType = string.IsNullOrWhiteSpace(parsed.EventType) ? "generic_event" : parsed.EventType,
            EventCategory = parsed.EventCategory,
            Severity = string.IsNullOrWhiteSpace(parsed.Severity) ? FindingSeverity.Low : parsed.Severity,
            User = parsed.User,
            SourceIp = parsed.SourceIp,
            DestinationIp = parsed.DestinationIp,
            SourcePort = parsed.SourcePort,
            DestinationPort = parsed.DestinationPort,
            Domain = parsed.Domain,
            Url = parsed.Url,
            FileName = parsed.FileName,
            FilePath = parsed.FilePath,
            FileHashSha256 = parsed.FileHashSha256,
            ProcessName = parsed.ProcessName,
            CommandLine = parsed.CommandLine,
            ParentProcessName = parsed.ParentProcessName,
            Mailbox = parsed.Mailbox,
            CloudTenantId = parsed.CloudTenantId,
            CloudResourceId = parsed.CloudResourceId,
            Action = parsed.Action,
            Outcome = parsed.Outcome,
            Message = parsed.Message,
            RawJson = parsed.RawJson,
            IngestedAtUtc = DateTime.UtcNow
        };
    }

    private static string ResolveSourceHealthStatus(bool isEnabled, int received, int failed, int parsed)
    {
        if (!isEnabled)
        {
            return "Disabled";
        }

        if (received == 0)
        {
            return "Warning";
        }

        if (parsed == 0 && failed > 0)
        {
            return "Error";
        }

        return failed == 0 ? "Healthy" : "Warning";
    }

    private static SiemSourceConfig BuildSourceConfig(Guid sourceId, SiemSourceConfigRequest request, SiemSourceConfig? existing)
    {
        return new SiemSourceConfig
        {
            SourceId = sourceId,
            PollingIntervalSeconds = Math.Clamp(request.PollingIntervalSeconds ?? existing?.PollingIntervalSeconds ?? 300, 30, 86_400),
            EndpointUrl = request.EndpointUrl ?? existing?.EndpointUrl,
            TenantId = request.TenantId ?? existing?.TenantId,
            Region = request.Region ?? existing?.Region,
            BucketName = request.BucketName ?? existing?.BucketName,
            StreamName = request.StreamName ?? existing?.StreamName,
            QueryFilter = request.QueryFilter ?? existing?.QueryFilter,
            MaxBatchSize = Math.Clamp(request.MaxBatchSize ?? existing?.MaxBatchSize ?? 1000, 1, 100_000),
            EnabledFromUtc = request.EnabledFromUtc ?? existing?.EnabledFromUtc,
            ConfigJson = SanitiseJsonOrDefault(request.ConfigJson ?? existing?.ConfigJson, "{}")
        };
    }

    private static SiemSourceSecretRef BuildSecretRef(Guid sourceId, SiemSourceSecretRefRequest request)
    {
        return new SiemSourceSecretRef
        {
            SourceId = sourceId,
            SecretPurpose = NormaliseOrDefault(request.SecretPurpose, "credential"),
            SecretProvider = NormaliseOrDefault(request.SecretProvider, "LocalUserSecrets"),
            SecretKey = NormaliseOrDefault(request.SecretKey, string.Empty)
        };
    }

    private static SecurityEvent BuildFallbackSecurityEvent(SiemEventRequest request, string fallbackSource, string parser)
    {
        return new SecurityEvent
        {
            TimestampUtc = request.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow,
            Source = string.IsNullOrWhiteSpace(request.Source) ? fallbackSource : request.Source.Trim(),
            SourceName = string.IsNullOrWhiteSpace(request.Source) ? fallbackSource : request.Source.Trim(),
            Host = string.IsNullOrWhiteSpace(request.Host) ? "unknown" : request.Host.Trim(),
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? "generic_event" : request.EventType.Trim(),
            EventCategory = "generic",
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? FindingSeverity.Low : request.Severity.Trim(),
            Message = request.Message?.Trim() ?? string.Empty,
            RawJson = request.Raw?.GetRawText() ?? "{}",
            IngestedAtUtc = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<Finding>> CreateFindingsForEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        var findings = new List<Finding>();
        if (IsHighSeverity(securityEvent.Severity))
        {
            findings.Add(new Finding
            {
                Module = "SIEM",
                Type = "SiemFinding",
                Title = $"High-severity event received: {securityEvent.EventType}",
                Severity = securityEvent.Severity,
                RiskScore = SecurityHelpers.SeverityWeight(securityEvent.Severity),
                Summary = securityEvent.Message,
                Evidence = $"Source={securityEvent.Source}; Host={securityEvent.Host}; EventType={securityEvent.EventType}; Category={securityEvent.EventCategory}",
                Recommendation = "Review the event, identify related assets and IOCs, and open an incident workflow if confirmed.",
                RelatedEntityId = securityEvent.Id,
                RelatedEntityType = nameof(SecurityEvent)
            });
        }

        var extractedIndicators = ExtractPotentialIndicators(securityEvent.Message + " " + securityEvent.RawJson);
        foreach (var indicator in extractedIndicators)
        {
            var match = await dataStore.FindIndicatorAsync(indicator.Normalized, cancellationToken);
            if (match is null || !IsFindingWorthy(match))
            {
                continue;
            }

            findings.Add(new Finding
            {
                Module = "SIEM",
                Type = "SiemFinding",
                Title = "Security event contains matched IOC",
                Severity = match.Severity,
                RiskScore = SecurityHelpers.SeverityWeight(match.Severity),
                Summary = $"{indicator.Original} in event matched local IOC storage.",
                Evidence = $"Indicator={match.IndicatorValue}; Source={match.Source}; EventId={securityEvent.Id}",
                Recommendation = "Pivot from this event into related endpoint, DNS, and identity telemetry.",
                RelatedEntityId = securityEvent.Id,
                RelatedEntityType = nameof(SecurityEvent)
            });
        }

        foreach (var finding in findings)
        {
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }

        return findings;
    }

    private static string NormaliseOrDefault(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string SanitiseJsonOrDefault(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
            return value;
        }
        catch
        {
            return fallback;
        }
    }

    private static bool IsHighSeverity(string severity) =>
        string.Equals(severity, FindingSeverity.High, StringComparison.OrdinalIgnoreCase)
        || string.Equals(severity, FindingSeverity.Critical, StringComparison.OrdinalIgnoreCase);

    private static bool IsFindingWorthy(Indicator indicator) =>
        string.Equals(indicator.Verdict, IndicatorVerdicts.Malicious, StringComparison.OrdinalIgnoreCase)
        || string.Equals(indicator.Verdict, IndicatorVerdicts.Suspicious, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<ClassifiedIndicator> ExtractPotentialIndicators(string text)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in UrlRegex().Matches(text))
        {
            var url = match.Value.TrimEnd('.', ',', ';', ')', ']');
            candidates.Add(url);
            var host = IndicatorClassifier.ExtractUrlHost(url);
            if (!string.IsNullOrWhiteSpace(host))
            {
                candidates.Add(host);
            }
        }

        foreach (Match match in IpRegex().Matches(text))
        {
            candidates.Add(match.Value);
        }

        foreach (Match match in EmailRegex().Matches(text))
        {
            candidates.Add(match.Value);
        }

        foreach (Match match in DomainRegex().Matches(text))
        {
            candidates.Add(match.Value.TrimEnd('.'));
        }

        return candidates.Select(IndicatorClassifier.Classify).Where(indicator => !string.IsNullOrWhiteSpace(indicator.Normalized));
    }

    [GeneratedRegex(@"https?://[^\s<>'""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex IpRegex();

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?\.)+[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();
}
