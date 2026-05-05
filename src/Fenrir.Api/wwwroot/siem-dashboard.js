const siemState = {
  sources: [],
  ingestionJobs: [],
  ingestionJobFilters: {
    status: "",
    source: "",
    parser: ""
  },
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

  injectIngestionJobFilters();

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

    eventRows.addEventListener("keydown", async (event) => {
      if (event.key !== "Enter" && event.key !== " ") return;
      const row = event.target.closest("tr[data-event-id]");
      if (!row) return;
      event.preventDefault();
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
      await withFormBusy(event.currentTarget, async () => {
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
      }, "Registering...");
    });
  }

  const agentBuilderForm = document.getElementById("siemAgentBuilderForm");
  if (agentBuilderForm) {
    agentBuilderForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      await withFormBusy(event.currentTarget, async () => {
        const formElement = event.currentTarget;
        const companyInput = formElement.querySelector("[name='companyName']");
        const serverUrlInput = formElement.querySelector("[name='serverUrl']");
        const sourceInput = formElement.querySelector("[name='sourceName']");
        const companyName = String(companyInput?.value || "").trim();
        const serverUrl = String(serverUrlInput?.value || "").trim();
        const sourceName = String(sourceInput?.value || "").trim();
        const target = document.getElementById("siemAgentBuilderResult");
        const validationTarget = document.getElementById("siemAgentBuilderValidation");

        if (validationTarget) {
          validationTarget.textContent = "";
        }

        if (!companyInput || !serverUrlInput) {
          if (validationTarget) {
            validationTarget.textContent = "Unable to read the agent builder form fields. Please refresh the page.";
          }
          target.innerHTML = "";
          return;
        }

        if (!companyName || !serverUrl) {
          if (validationTarget) {
            validationTarget.textContent = "Company name and server API URL / IP address are required.";
          }
          target.innerHTML = "";
          return;
        }

        const normalizedServerUrl = normalizeServerAddress(serverUrl);
        if (!normalizedServerUrl) {
          if (validationTarget) {
            validationTarget.textContent = "Server API URL must be a valid http(s) URL or IP address.";
          }
          target.innerHTML = "";
          return;
        }

        console.debug("Agent builder submit", { companyName, serverUrl, normalizedServerUrl, sourceName });
        target.innerHTML = `<div class="result-title">Building agent package...</div>`;

        try {
          const response = await fetch("/api/agents/build", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ companyName, serverUrl: normalizedServerUrl, sourceName: sourceName || null })
          });

          if (!response.ok) {
            const text = await response.text();
            throw new Error(text || response.statusText);
          }

          const blob = await response.blob();
          const fileName = `FenrirAgent-${companyName.replace(/[^a-z0-9_-]/gi, "_") || "agent"}.zip`;
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement("a");
          anchor.href = url;
          anchor.download = fileName;
          document.body.appendChild(anchor);
          anchor.click();
          anchor.remove();
          setTimeout(() => URL.revokeObjectURL(url), 15000);

          target.innerHTML = `<div class="result-title">Agent package created</div><p>Download should start automatically. If not, <a href="${url}" download="${fileName}">click here</a>.</p>`;
          showToast("Agent package created");
          event.currentTarget.reset();
        } catch (error) {
          target.innerHTML = renderError(error);
        }
      }, "Building...");
    });
  }

  const searchForm = document.getElementById("siemSearchForm");
  if (searchForm) {
    searchForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      await withFormBusy(event.currentTarget, async () => {
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
      }, "Searching...");
    });
  }
}

function injectIngestionJobFilters() {
  const rows = document.getElementById("siemIngestionJobRows");
  if (!rows || document.getElementById("siemIngestionJobFilters")) {
    return;
  }

  const tableWrap = rows.closest(".table-wrap");
  if (!tableWrap) {
    return;
  }

  const filterBar = document.createElement("div");
  filterBar.id = "siemIngestionJobFilters";
  filterBar.className = "tool-form inline-form";
  filterBar.innerHTML = `
    <select id="siemJobStatusFilter" aria-label="Filter ingestion jobs by status">
      <option value="">All statuses</option>
      <option value="completed">Completed</option>
      <option value="partially_parsed">Partially parsed</option>
      <option value="processing">Processing</option>
      <option value="queued">Queued</option>
      <option value="failed">Failed</option>
    </select>
    <input id="siemJobSourceFilter" type="text" placeholder="Filter source / endpoint">
    <input id="siemJobParserFilter" type="text" placeholder="Filter parser">
    <button class="ghost" id="siemJobFilterClear" type="button">Clear</button>
  `;

  tableWrap.before(filterBar);

  const apply = () => {
    siemState.ingestionJobFilters.status = document.getElementById("siemJobStatusFilter")?.value || "";
    siemState.ingestionJobFilters.source = document.getElementById("siemJobSourceFilter")?.value || "";
    siemState.ingestionJobFilters.parser = document.getElementById("siemJobParserFilter")?.value || "";
    renderSiemCollector();
  };

  document.getElementById("siemJobStatusFilter")?.addEventListener("change", apply);
  document.getElementById("siemJobSourceFilter")?.addEventListener("input", apply);
  document.getElementById("siemJobParserFilter")?.addEventListener("input", apply);
  document.getElementById("siemJobFilterClear")?.addEventListener("click", () => {
    document.getElementById("siemJobStatusFilter").value = "";
    document.getElementById("siemJobSourceFilter").value = "";
    document.getElementById("siemJobParserFilter").value = "";
    apply();
  });
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
  const filteredJobs = getFilteredIngestionJobs();

  setMetric("metricSiemSources", siemState.sources.length);
  setMetric("metricSiemHealthy", healthySources.length);
  setMetric("metricSiemJobs", filteredJobs.length);
  setMetric("metricSiemTelemetry", state.events.length);

  renderRows("siemSourceRows", siemState.sources, (source) => {
    const health = latestSourceHealth(source);
    return `
      <tr>
        <td>${pill(source.status || health?.status || (source.isEnabled ? "Healthy" : "Disabled"))}</td>
        <td>
          <strong>${escapeHtml(source.name)}</strong>
          <div class="muted-text">${escapeHtml(source.vendor || "Generic")} / ${escapeHtml(source.product || "Generic")}</div>
          ${health?.lastError ? `<div class="error-text">${escapeHtml(health.lastError)}</div>` : ""}
        </td>
        <td>
          ${escapeHtml(source.sourceType)}
          <div class="muted-text">Parser: ${escapeHtml(source.parser)}</div>
        </td>
        <td>
          <strong>${Number(health?.eventsReceivedLast15Minutes || 0)}</strong> received
          <div class="muted-text">${Number(health?.eventsParsedLast15Minutes || 0)} parsed · ${Number(health?.eventsFailedLast15Minutes || 0)} failed</div>
        </td>
        <td>
          ${formatPercent(health?.parseFailureRate)}
          <div class="muted-text">Lag: ${formatDurationSeconds(health?.lagSeconds)} · Backlog: ${Number(health?.queueBacklog || 0)}</div>
        </td>
        <td>
          ${formatDate(health?.lastSuccessfulIngestAtUtc || source.lastSuccessfulIngestAtUtc)}
          <div class="muted-text">Poll: ${formatDate(health?.lastPollAtUtc || source.lastSeenAtUtc)}</div>
        </td>
      </tr>
    `;
  });

  renderRows("siemIngestionJobRows", filteredJobs, (job) => `
    <tr>
      <td>${pill(job.status)}</td>
      <td>
        ${escapeHtml(job.sourceName || "Unknown")}
        <div class="muted-text">${formatDate(job.startedAtUtc)}</div>
      </td>
      <td>${escapeHtml(job.parser || "")}</td>
      <td>${Number(job.eventsParsed || 0)}</td>
      <td>${Number(job.eventsFailed || 0)}</td>
    </tr>
  `);
}

function latestSourceHealth(source) {
  const snapshots = Array.isArray(source.recentHealth) ? source.recentHealth : [];
  return snapshots.length ? snapshots[0] : null;
}

function formatPercent(value) {
  const numeric = Number(value || 0);
  return `${Math.round(numeric * 1000) / 10}%`;
}

function formatDurationSeconds(value) {
  const seconds = Number(value || 0);
  if (seconds < 60) {
    return `${seconds}s`;
  }
  if (seconds < 3600) {
    return `${Math.round(seconds / 60)}m`;
  }
  return `${Math.round(seconds / 3600)}h`;
}

function getFilteredIngestionJobs() {
  const status = siemState.ingestionJobFilters.status.trim().toLowerCase();
  const source = siemState.ingestionJobFilters.source.trim().toLowerCase();
  const parser = siemState.ingestionJobFilters.parser.trim().toLowerCase();

  return siemState.ingestionJobs.filter((job) => {
    const jobStatus = String(job.status || "").toLowerCase();
    const jobSource = String(job.sourceName || "").toLowerCase();
    const jobParser = String(job.parser || "").toLowerCase();
    return (!status || jobStatus === status)
      && (!source || jobSource.includes(source))
      && (!parser || jobParser.includes(parser));
  });
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
    <tr data-event-id="${escapeHtml(event.id)}" tabindex="0" role="button" aria-label="Open event ${escapeHtml(event.eventType || "event")}">
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

function normalizeServerAddress(value) {
  if (!value) {
    return null;
  }

  const trimmed = String(value).trim();
  const hasScheme = /^[a-z][a-z0-9+.-]*:\/\//i.test(trimmed);
  let candidate = trimmed;

  if (!hasScheme) {
    const scheme = window.location.protocol || "http:";
    const port = window.location.port ? `:${window.location.port}` : "";
    const hostPort = trimmed.includes(":" ) ? trimmed : `${trimmed}${port}`;
    candidate = `${scheme}//${hostPort}`;
  }

  try {
    const url = new URL(candidate);
    if (url.protocol !== "http:" && url.protocol !== "https:") {
      return null;
    }
    return url.toString().replace(/\/$/, "");
  } catch {
    return null;
  }
}

function isValidServerAddress(value) {
  return normalizeServerAddress(value) !== null;
}

function isHealthySource(source) {
  const status = String(source.status || "").toLowerCase();
  return source.isEnabled !== false && (status === "healthy" || status === "warning" || status === "");
}
