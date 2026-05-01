const siemState = {
  sources: [],
  ingestionJobs: [],
  selectedEvent: null,
  autoRefreshHandle: null
};

document.addEventListener("DOMContentLoaded", () => {
  bindSiemDashboard();
  refreshSiemCollector();
  startSiemAutoRefresh();
});

function bindSiemDashboard() {
  const refreshCollectorButton = document.getElementById("refreshSiemCollectorButton");
  if (refreshCollectorButton) {
    refreshCollectorButton.addEventListener("click", refreshSiemCollector);
  }

  const eventRows = document.getElementById("eventRows");
  if (eventRows) {
    eventRows.addEventListener("click", async (event) => {
      const action = event.target?.dataset?.pivotAction;
      const value = event.target?.dataset?.pivotValue;
      if (action && value) {
        await pivotSiemEvents(action, value);
        return;
      }

      const row = event.target.closest("tr[data-event-id]");
      if (!row) {
        return;
      }

      const selected = state.events.find((item) => item.id === row.dataset.eventId);
      if (selected) {
        siemState.selectedEvent = selected;
        renderSiemEventDetail(selected);
      }
    });
  }

  const agentForm = document.getElementById("siemAgentRegistrationForm");
  if (agentForm) {
    agentForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      const form = new FormData(event.currentTarget);
      const hostname = String(form.get("hostname") || "").trim();
      const description = String(form.get("description") || "").trim();
      const sourceName = String(form.get("name") || "").trim();

      const payload = {
        name: sourceName,
        sourceType: form.get("sourceType") || "agent",
        vendor: form.get("vendor") || "Fenrir",
        product: form.get("product") || "Fenrir Agent",
        connectionType: "agent_push",
        parser: form.get("parser") || "generic_json_v1",
        description: description || (hostname ? `Agent telemetry source for ${hostname}` : "Agent telemetry source"),
        isEnabled: true
      };

      const target = document.getElementById("siemAgentRegistrationResult");
      target.innerHTML = `<div class="result-title">Registering agent source...</div>`;

      try {
        const source = await api("/api/siem/sources", { method: "POST", body: payload });
        target.innerHTML = `
          <div class="result-title">
            ${pill(source.status)}
            <span>${escapeHtml(source.name)}</span>
          </div>
          <p>Source registered and ready for agent telemetry.</p>
          <p class="muted-text">Configure the agent to push telemetry using Source: <strong>${escapeHtml(source.name)}</strong>.</p>
          ${jsonBlock(source)}
        `;
        showToast("SIEM agent source registered");
        event.currentTarget.reset();
        await refreshSiemCollector();
      } catch (error) {
        target.innerHTML = renderError(error);
      }
    });
  }

  const searchForm = document.getElementById("siemSearchForm");
  if (searchForm) {
    searchForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      const form = new FormData(event.currentTarget);
      const payload = {
        source: emptyToNull(form.get("source")),
        host: emptyToNull(form.get("host")),
        severity: emptyToNull(form.get("severity")),
        eventType: emptyToNull(form.get("eventType")),
        userName: emptyToNull(form.get("userName")),
        sourceIp: emptyToNull(form.get("sourceIp")),
        destinationIp: emptyToNull(form.get("destinationIp")),
        domain: emptyToNull(form.get("domain")),
        fileHashSha256: emptyToNull(form.get("hash")),
        eventCategory: emptyToNull(form.get("category")),
        indicator: emptyToNull(form.get("indicator")),
        take: 500
      };

      try {
        state.events = await api("/api/siem/events/search", { method: "POST", body: payload });
        renderEvents();
        renderDashboard();
        renderSiemCollector();
        showToast("Telemetry search completed");
      } catch (error) {
        showToast(`Telemetry search failed: ${error.message}`);
      }
    });
  }
}

async function refreshSiemCollector() {
  await Promise.allSettled([
    refreshSiemSources(),
    refreshSiemIngestionJobs(),
    refreshEvents()
  ]);
  renderSiemCollector();
}

async function refreshSiemSources() {
  try {
    siemState.sources = await api("/api/siem/sources");
  } catch (error) {
    siemState.sources = [];
    showToast(`SIEM sources unavailable: ${error.message}`);
  }
}

async function refreshSiemIngestionJobs() {
  try {
    siemState.ingestionJobs = await api("/api/siem/ingestion-jobs");
  } catch (error) {
    siemState.ingestionJobs = [];
    showToast(`SIEM ingestion jobs unavailable: ${error.message}`);
  }
}

async function pivotSiemEvents(field, value) {
  const query = new URLSearchParams();
  query.set(field, value);
  query.set("take", "500");

  try {
    state.events = await api(`/api/siem/events?${query.toString()}`);
    renderEvents();
    renderDashboard();
    renderSiemCollector();
    showToast(`Pivoted telemetry by ${field}: ${value}`);
  } catch (error) {
    showToast(`Pivot failed: ${error.message}`);
  }
}

function renderSiemCollector() {
  const healthySources = siemState.sources.filter((source) => isHealthySource(source));

  setMetric("metricSiemSources", siemState.sources.length);
  setMetric("metricSiemHealthy", healthySources.length);
  setMetric("metricSiemJobs", siemState.ingestionJobs.length);
  setMetric("metricSiemTelemetry", state.events.length);

  renderRows("siemSourceRows", siemState.sources, (source) => `
    <tr>
      <td>${pill(source.status || (source.isEnabled ? "Healthy" : "Disabled"))}</td>
      <td>
        <strong>${escapeHtml(source.name)}</strong>
        <div class="muted-text">${escapeHtml(source.vendor || "Generic")} / ${escapeHtml(source.product || "Generic")}</div>
      </td>
      <td>${escapeHtml(source.sourceType)}</td>
      <td>${escapeHtml(source.parser)}</td>
      <td>${formatDate(source.lastSuccessfulIngestAtUtc)}</td>
    </tr>
  `);

  renderRows("siemIngestionJobRows", siemState.ingestionJobs, (job) => `
    <tr>
      <td>${pill(job.status)}</td>
      <td>${escapeHtml(job.sourceName || "Unknown")}</td>
      <td>${escapeHtml(job.parser || "")}</td>
      <td>${Number(job.eventsParsed || 0)}</td>
      <td>${Number(job.eventsFailed || 0)}</td>
    </tr>
  `);
}

function startSiemAutoRefresh() {
  if (siemState.autoRefreshHandle) {
    clearInterval(siemState.autoRefreshHandle);
  }

  siemState.autoRefreshHandle = setInterval(async () => {
    const siemView = document.getElementById("view-siem");
    if (!siemView || !siemView.classList.contains("active")) {
      return;
    }

    const status = document.getElementById("siemAutoRefreshStatus");
    if (status) {
      status.textContent = "Refreshing telemetry...";
    }

    await refreshSiemCollector();

    if (status) {
      status.textContent = `Auto-refresh enabled · Last checked ${new Date().toLocaleTimeString()}`;
    }
  }, 20000);
}

function renderEvents() {
  renderRows("eventRows", state.events, (event) => `
    <tr data-event-id="${escapeHtml(event.id)}">
      <td>${pill(event.severity)}</td>
      <td>
        ${escapeHtml(event.sourceName || event.source || "")}
        <div class="muted-text">${escapeHtml(event.vendor || "")} ${escapeHtml(event.product || "")}</div>
      </td>
      <td>${pivotButton("host", event.host, event.host || "")}</td>
      <td>
        ${escapeHtml(event.eventType || "")}
        <div class="muted-text">${pivotButton("category", event.eventCategory, event.eventCategory || "uncategorised")}</div>
      </td>
      <td>${renderSiemPivotSummary(event)}</td>
      <td>${formatDate(event.timestampUtc || event.ingestedAtUtc)}</td>
    </tr>
  `);
  renderSiemCollector();
}

function renderSiemPivotSummary(event) {
  const pivots = [
    ["sourceIp", event.sourceIp, "Src IP"],
    ["destinationIp", event.destinationIp, "Dst IP"],
    ["user", event.user, "User"],
    ["domain", event.domain, "Domain"],
    ["hash", event.fileHashSha256, "Hash"]
  ].filter(([, value]) => value);

  if (!pivots.length) {
    return escapeHtml(event.message || "");
  }

  return `
    <div>${escapeHtml(event.message || "")}</div>
    <div class="pivot-list">
      ${pivots.map(([field, value, label]) => pivotButton(field, value, `${label}: ${value}`)).join(" ")}
    </div>
  `;
}

function renderSiemEventDetail(event) {
  const target = document.getElementById("siemEventDetail");
  if (!target) {
    return;
  }

  const parsedFields = {
    id: event.id,
    timestampUtc: event.timestampUtc,
    sourceId: event.sourceId,
    sourceName: event.sourceName,
    vendor: event.vendor,
    product: event.product,
    eventType: event.eventType,
    eventCategory: event.eventCategory,
    severity: event.severity,
    host: event.host,
    user: event.user,
    sourceIp: event.sourceIp,
    destinationIp: event.destinationIp,
    sourcePort: event.sourcePort,
    destinationPort: event.destinationPort,
    domain: event.domain,
    url: event.url,
    fileName: event.fileName,
    filePath: event.filePath,
    fileHashSha256: event.fileHashSha256,
    processName: event.processName,
    commandLine: event.commandLine,
    parentProcessName: event.parentProcessName,
    mailbox: event.mailbox,
    cloudTenantId: event.cloudTenantId,
    cloudResourceId: event.cloudResourceId,
    action: event.action,
    outcome: event.outcome
  };

  target.innerHTML = `
    <div class="result-title">
      ${pill(event.severity)}
      <span>${escapeHtml(event.eventType || "Security event")}</span>
    </div>
    <p>${escapeHtml(event.message || "")}</p>
    <div class="pivot-list">
      ${pivotButton("host", event.host, "Search this host")}
      ${pivotButton("user", event.user, "Search this user")}
      ${pivotButton("sourceIp", event.sourceIp, "Search source IP")}
      ${pivotButton("destinationIp", event.destinationIp, "Search destination IP")}
      ${pivotButton("domain", event.domain, "Search this domain")}
      ${pivotButton("hash", event.fileHashSha256, "Search this hash")}
      <button class="ghost" type="button" disabled>Create case from event</button>
      <button class="ghost" type="button" disabled>Add event to case</button>
    </div>
    <h3>Parsed fields</h3>
    ${jsonBlock(parsedFields)}
    <h3>Raw JSON</h3>
    <pre class="json-output">${escapeHtml(formatRawJson(event.rawJson))}</pre>
  `;
}

function pivotButton(field, value, label) {
  if (!value) {
    return "";
  }

  return `<button class="ghost pivot-button" type="button" data-pivot-action="${escapeHtml(field)}" data-pivot-value="${escapeHtml(value)}">${escapeHtml(label)}</button>`;
}

function formatRawJson(rawJson) {
  if (!rawJson) {
    return "{}";
  }

  try {
    return JSON.stringify(JSON.parse(rawJson), null, 2);
  } catch {
    return rawJson;
  }
}

function isHealthySource(source) {
  const status = String(source.status || "").toLowerCase();
  return source.isEnabled !== false && (status === "healthy" || status === "warning" || status === "");
}

function setMetric(id, value) {
  const element = document.getElementById(id);
  if (element) {
    element.textContent = value;
  }
}

function emptyToNull(value) {
  const text = String(value || "").trim();
  return text.length ? text : null;
}
