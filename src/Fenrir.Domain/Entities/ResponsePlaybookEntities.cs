namespace Fenrir.Domain.Entities;

public class ResponsePlaybook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "general";
    public string Severity { get; set; } = "Medium";
    public string TriggerType { get; set; } = "manual";
    public string? MitreTactic { get; set; }
    public string? MitreTechnique { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ResponsePlaybookStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlaybookId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ActionType { get; set; } = "manual";
    public string TargetType { get; set; } = "analyst";
    public string? CommandPreview { get; set; }
    public string? IntegrationKey { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public int SortOrder { get; set; }
}

public class ResponsePlaybookRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlaybookId { get; set; }
    public string PlaybookName { get; set; } = "";
    public Guid? CaseId { get; set; }
    public Guid? AlertId { get; set; }
    public Guid? EventId { get; set; }
    public string Status { get; set; } = "Started";
    public string StartedBy { get; set; } = "analyst";
    public string? Notes { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class ResponsePlaybookRunStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RunId { get; set; }
    public Guid PlaybookStepId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? Result { get; set; }
    public string? ExecutedBy { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public int SortOrder { get; set; }
}
