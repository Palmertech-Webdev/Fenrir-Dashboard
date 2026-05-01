const siemState = {
  sources: [],
  ingestionJobs: [],
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
        parser: form.get("parser") || "fenrir_agent_v1",
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
    <tr>
      <td>${pill(event.severity)}</td>
      <td>${escapeHtml(event.source || "")}</td>
      <td>${escapeHtml(event.host || "")}</td>
      <td>${escapeHtml(event.eventType || "")}</td>
      <td>${escapeHtml(event.message || "")}</td>
      <td>${formatDate(event.timestampUtc || event.ingestedAtUtc)}</td>
    </tr>
  `);
  renderSiemCollector();
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
