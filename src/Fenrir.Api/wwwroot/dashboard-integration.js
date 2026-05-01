const integrationState = {
  agents: [],
  cases: [],
  selectedCase: null,
  investigationViews: {
    email: null,
    cloud: null,
    windows: null
  }
};

document.addEventListener("DOMContentLoaded", () => {
  installPhaseDashboardNavigation();
  installPhaseDashboardViews();
  installPhaseDashboardBindings();
  refreshPhaseDashboardData();
});

function installPhaseDashboardNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="agents"]')) {
    return;
  }

  const additions = [
    ["agents", "Agents"],
    ["cases", "Cases"],
    ["investigations", "Investigations"]
  ];

  for (const [view, label] of additions) {
    const button = document.createElement("button");
    button.className = "nav-item";
    button.dataset.view = view;
    button.type = "button";
    button.textContent = label;
    button.addEventListener("click", () => showIntegratedView(view));
    nav.appendChild(button);
  }
}

function installPhaseDashboardViews() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-cases")) {
    return;
  }

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-agents">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 4</p>
          <h2>Agent enrolment, heartbeat and source truthfulness</h2>
          <p class="muted-text">Track enrolled endpoints, last heartbeat, last telemetry, queued events and source mapping.</p>
        </div>
        <button class="secondary" id="refreshAgentsButton" type="button">Refresh Agents</button>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Total Agents</span><strong id="metricAgentsTotal">0</strong></article>
        <article class="metric"><span>Healthy</span><strong id="metricAgentsHealthy">0</strong></article>
        <article class="metric"><span>Warning</span><strong id="metricAgentsWarning">0</strong></article>
        <article class="metric"><span>Offline / Disabled</span><strong id="metricAgentsOffline">0</strong></article>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Agent Endpoints</h2><span class="muted-text">Status is based on heartbeat timing, not database existence.</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Status</th><th>Agent</th><th>Host</th><th>Version</th><th>Last Heartbeat</th><th>Queued</th></tr></thead>
            <tbody id="agentRows"></tbody>
          </table>
        </div>
      </section>
    </section>

    <section class="view" id="view-cases">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 8</p>
          <h2>Investigation case workbench</h2>
          <p class="muted-text">Create cases, link events and IOCs, add notes, track evidence references and build an investigation timeline.</p>
        </div>
        <button class="secondary" id="refreshCasesButton" type="button">Refresh Cases</button>
      </div>

      <div class="two-column">
        <section class="panel">
          <h2>Create Case</h2>
          <form id="caseCreateForm" class="tool-form">
            <label>Title<input name="title" type="text" placeholder="Suspicious admin activity" required></label>
            <label>Description<textarea name="description" rows="3" placeholder="What triggered the investigation?"></textarea></label>
            <div class="form-grid">
              <label>Severity<select name="severity"><option>Low</option><option selected>Medium</option><option>High</option><option>Critical</option></select></label>
              <label>Assigned to<input name="assignedTo" type="text" placeholder="analyst@example.com"></label>
            </div>
            <button type="submit">Create Case</button>
          </form>
          <div class="result-box" id="caseCreateResult"></div>
        </section>

        <section class="panel">
          <h2>Case Detail</h2>
          <div class="result-box" id="caseDetail"><p class="muted-text">Select a case to view notes, linked entities and timeline.</p></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Cases</h2><span class="muted-text">Click a case to open it.</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Case</th><th>Severity</th><th>Status</th><th>Assigned</th><th>Links</th><th>Updated</th></tr></thead>
            <tbody id="caseRows"></tbody>
          </table>
        </div>
      </section>
    </section>

    <section class="view" id="view-investigations">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 9</p>
          <h2>Dedicated investigation views</h2>
          <p class="muted-text">Focused email, cloud and Windows pivots over the normalised SIEM schema, with related-case awareness.</p>
        </div>
        <button class="secondary" id="runInvestigationViewsButton" type="button">Run Views</button>
      </div>

      <section class="panel">
        <h2>Investigation Scope</h2>
        <form id="investigationScopeForm" class="tool-form">
          <div class="form-grid"><label>User<input name="user" type="text" placeholder="admin@example.com"></label><label>Host<input name="host" type="text" placeholder="DESKTOP-01"></label></div>
          <div class="form-grid"><label>IP address<input name="ipAddress" type="text" placeholder="1.2.3.4"></label><label>Domain<input name="domain" type="text" placeholder="evil.example"></label></div>
          <div class="form-grid"><label>Process<input name="process" type="text" placeholder="powershell.exe"></label><label>Cloud action<input name="action" type="text" placeholder="CreateAccessKey"></label></div>
          <div class="form-grid"><label>Tenant ID<input name="tenantId" type="text"></label><label>Resource ID<input name="resourceId" type="text"></label></div>
          <button type="submit">Run Investigation Views</button>
        </form>
      </section>

      <div class="three-column investigation-grid">
        <section class="panel"><div class="panel-heading"><h2>Email</h2><span class="pill Informational">M365 / Mailbox</span></div><div id="emailInvestigationView" class="result-box"></div></section>
        <section class="panel"><div class="panel-heading"><h2>Cloud</h2><span class="pill Informational">Control plane</span></div><div id="cloudInvestigationView" class="result-box"></div></section>
        <section class="panel"><div class="panel-heading"><h2>Windows</h2><span class="pill Informational">Endpoint</span></div><div id="windowsInvestigationView" class="result-box"></div></section>
      </div>
    </section>
  `);

  installDashboardMetricExtensions();
}

function installDashboardMetricExtensions() {
  const dashboardMetricGrid = document.querySelector("#view-dashboard .metric-grid");
  if (!dashboardMetricGrid || document.getElementById("metricCases")) {
    return;
  }

  dashboardMetricGrid.insertAdjacentHTML("beforeend", `
    <article class="metric"><span>Cases</span><strong id="metricCases">0</strong></article>
    <article class="metric"><span>Agents</span><strong id="metricAgents">0</strong></article>
    <article class="metric"><span>Sources</span><strong id="metricSources">0</strong></article>
    <article class="metric"><span>Queued Jobs</span><strong id="metricQueuedJobs">0</strong></article>
  `);
}

function installPhaseDashboardBindings() {
  document.getElementById("refreshAgentsButton")?.addEventListener("click", refreshAgents);
  document.getElementById("refreshCasesButton")?.addEventListener("click", refreshCases);
  document.getElementById("runInvestigationViewsButton")?.addEventListener("click", runInvestigationViews);

  document.getElementById("caseRows")?.addEventListener("click", async (event) => {
    const row = event.target.closest("tr[data-case-id]");
    if (!row) return;
    await openCase(row.dataset.caseId);
  });

  document.getElementById("caseCreateForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      title: form.get("title"),
      description: emptyToNull(form.get("description")),
      severity: form.get("severity") || "Medium",
      assignedTo: emptyToNull(form.get("assignedTo")),
      createdBy: "analyst"
    };

    try {
      const created = await api("/api/cases", { method: "POST", body: payload });
      document.getElementById("caseCreateResult").innerHTML = `<div class="result-title">${pill(created.severity)} <span>${escapeHtml(created.caseNumber)}</span></div><p>Case created.</p>`;
      event.currentTarget.reset();
      await refreshCases();
      await openCase(created.id);
      showToast("Case created");
    } catch (error) {
      document.getElementById("caseCreateResult").innerHTML = renderError(error);
    }
  });

  document.getElementById("investigationScopeForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    await runInvestigationViews();
  });

  document.getElementById("caseDetail")?.addEventListener("click", async (event) => {
    const caseId = integrationState.selectedCase?.id;
    if (!caseId) return;

    if (event.target.matches("[data-add-note]")) {
      const note = document.getElementById("caseNoteInput")?.value || "";
      if (!note.trim()) return;
      await api(`/api/cases/${caseId}/notes`, { method: "POST", body: { note, author: "analyst" } });
      await openCase(caseId);
      showToast("Note added");
    }

    if (event.target.matches("[data-add-timeline]")) {
      const title = document.getElementById("caseTimelineTitleInput")?.value || "";
      const description = document.getElementById("caseTimelineDescriptionInput")?.value || "";
      if (!title.trim()) return;
      await api(`/api/cases/${caseId}/timeline`, { method: "POST", body: { itemType: "manual", title, description } });
      await openCase(caseId);
      showToast("Timeline item added");
    }
  });
}

async function refreshPhaseDashboardData() {
  await Promise.allSettled([refreshAgents(), refreshCases()]);
  renderIntegratedMetrics();
}

async function refreshAgents() {
  try {
    integrationState.agents = await api("/api/agents");
  } catch (error) {
    integrationState.agents = [];
    showToast(`Agents unavailable: ${error.message}`);
  }
  renderAgents();
  renderIntegratedMetrics();
}

async function refreshCases() {
  try {
    integrationState.cases = await api("/api/cases");
  } catch (error) {
    integrationState.cases = [];
    showToast(`Cases unavailable: ${error.message}`);
  }
  renderCases();
  renderIntegratedMetrics();
}

function renderAgents() {
  const healthy = integrationState.agents.filter((agent) => agent.status === "Healthy");
  const warning = integrationState.agents.filter((agent) => agent.status === "Warning");
  const offline = integrationState.agents.filter((agent) => ["Offline", "Disabled", "Unenrolled"].includes(agent.status));

  setMetric("metricAgentsTotal", integrationState.agents.length);
  setMetric("metricAgentsHealthy", healthy.length);
  setMetric("metricAgentsWarning", warning.length);
  setMetric("metricAgentsOffline", offline.length);

  renderRows("agentRows", integrationState.agents, (agent) => `
    <tr>
      <td>${pill(agent.status)}</td>
      <td><strong>${escapeHtml(agent.agentId || "")}</strong><div class="muted-text">${escapeHtml(agent.operatingSystem || "")}</div></td>
      <td>${escapeHtml(agent.hostname || "")}<div class="muted-text">${escapeHtml(agent.ipAddress || "")}</div></td>
      <td>${escapeHtml(agent.agentVersion || "")}</td>
      <td>${formatDate(agent.lastHeartbeatAtUtc)}<div class="muted-text">Telemetry: ${formatDate(agent.lastTelemetryAtUtc)}</div></td>
      <td>${Number(agent.queuedEventsCount || 0)}</td>
    </tr>
  `);
}

function renderCases() {
  renderRows("caseRows", integrationState.cases, (item) => `
    <tr data-case-id="${escapeHtml(item.id)}">
      <td><strong>${escapeHtml(item.caseNumber)}</strong><div class="muted-text">${escapeHtml(item.title)}</div></td>
      <td>${pill(item.severity)}</td>
      <td>${pill(item.status)}</td>
      <td>${escapeHtml(item.assignedTo || "Unassigned")}</td>
      <td>${Number(item.eventCount || 0)} events · ${Number(item.indicatorCount || 0)} IOCs · ${Number(item.noteCount || 0)} notes</td>
      <td>${formatDate(item.updatedAtUtc)}</td>
    </tr>
  `);
}

async function openCase(caseId) {
  try {
    const detail = await api(`/api/cases/${caseId}`);
    integrationState.selectedCase = detail;
    renderCaseDetail(detail);
    showIntegratedView("cases");
  } catch (error) {
    document.getElementById("caseDetail").innerHTML = renderError(error);
  }
}

function renderCaseDetail(detail) {
  const target = document.getElementById("caseDetail");
  if (!target) return;

  target.innerHTML = `
    <div class="result-title">${pill(detail.severity)} ${pill(detail.status)} <span>${escapeHtml(detail.caseNumber)}</span></div>
    <h3>${escapeHtml(detail.title)}</h3>
    <p>${escapeHtml(detail.description || "No description provided.")}</p>
    <div class="two-column compact-panels">
      <div><strong>Assigned</strong><p>${escapeHtml(detail.assignedTo || "Unassigned")}</p></div>
      <div><strong>Updated</strong><p>${formatDate(detail.updatedAtUtc)}</p></div>
    </div>
    <h3>Linked Events</h3>
    ${renderSimpleList(detail.eventLinks, (link) => `${link.eventId} — ${link.reason || "linked event"}`)}
    <h3>Linked IOCs</h3>
    ${renderSimpleList(detail.indicatorLinks, (link) => `${link.indicatorId} — ${link.reason || "linked IOC"}`)}
    <h3>Notes</h3>
    ${renderSimpleList(detail.notes, (note) => `${note.author}: ${note.note}`)}
    <textarea id="caseNoteInput" rows="3" placeholder="Add analyst note"></textarea>
    <button class="ghost" type="button" data-add-note>Add Note</button>
    <h3>Timeline</h3>
    ${renderSimpleList(detail.timelineItems, (item) => `${formatDate(item.occurredAtUtc)} — ${item.title}${item.description ? `: ${item.description}` : ""}`)}
    <input id="caseTimelineTitleInput" type="text" placeholder="Timeline title">
    <textarea id="caseTimelineDescriptionInput" rows="2" placeholder="Timeline description"></textarea>
    <button class="ghost" type="button" data-add-timeline>Add Timeline Item</button>
  `;
}

function renderSimpleList(items, renderer) {
  if (!items || !items.length) {
    return `<p class="muted-text">None yet.</p>`;
  }
  return `<ul class="compact-list">${items.map((item) => `<li>${escapeHtml(renderer(item))}</li>`).join("")}</ul>`;
}

async function runInvestigationViews() {
  const form = document.getElementById("investigationScopeForm");
  const formData = form ? new FormData(form) : new FormData();
  const user = emptyToNull(formData.get("user"));
  const host = emptyToNull(formData.get("host"));
  const ipAddress = emptyToNull(formData.get("ipAddress"));
  const domain = emptyToNull(formData.get("domain"));
  const process = emptyToNull(formData.get("process"));
  const action = emptyToNull(formData.get("action"));
  const tenantId = emptyToNull(formData.get("tenantId"));
  const resourceId = emptyToNull(formData.get("resourceId"));

  const emailQuery = buildQuery({ user, domain });
  const cloudQuery = buildQuery({ user, ipAddress, tenantId, resourceId, action });
  const windowsQuery = buildQuery({ host, user, process, ipAddress });

  await Promise.allSettled([
    loadInvestigationView("email", `/api/investigations/views/email?${emailQuery}`, "emailInvestigationView"),
    loadInvestigationView("cloud", `/api/investigations/views/cloud?${cloudQuery}`, "cloudInvestigationView"),
    loadInvestigationView("windows", `/api/investigations/views/windows?${windowsQuery}`, "windowsInvestigationView")
  ]);

  showToast("Investigation views refreshed");
}

async function loadInvestigationView(type, url, targetId) {
  const target = document.getElementById(targetId);
  if (target) target.innerHTML = `<div class="result-title">Loading ${type} investigation...</div>`;
  try {
    const data = await api(url);
    integrationState.investigationViews[type] = data;
    if (target) target.innerHTML = renderInvestigationView(data);
  } catch (error) {
    if (target) target.innerHTML = renderError(error);
  }
}

function renderInvestigationView(data) {
  const summary = data.summary || {};
  return `
    <div class="result-title">${pill(data.viewType)} <span>${escapeHtml(data.title)}</span></div>
    <p>${escapeHtml(data.scopeDescription || "")}</p>
    <div class="mini-metrics">
      <span><strong>${Number(summary.totalEvents || 0)}</strong> events</span>
      <span><strong>${Number(summary.highOrCriticalEvents || 0)}</strong> high/critical</span>
      <span><strong>${Number(summary.uniqueUsers || 0)}</strong> users</span>
      <span><strong>${Number(summary.uniqueHosts || 0)}</strong> hosts</span>
    </div>
    <h3>Pivots</h3>
    <div class="pivot-list">${(data.pivots || []).slice(0, 12).map((pivot) => `<button class="ghost" type="button" data-integrated-pivot-url="${escapeHtml(pivot.searchUrl)}">${escapeHtml(pivot.label)}: ${escapeHtml(pivot.value)} (${pivot.eventCount})</button>`).join(" ") || `<span class="muted-text">No pivots yet.</span>`}</div>
    <h3>Timeline</h3>
    ${renderSimpleList((data.timeline || []).slice(0, 10), (event) => `${formatDate(event.timestampUtc)} — ${event.severity} — ${event.eventType} — ${event.message}`)}
    <h3>Related Cases</h3>
    ${renderSimpleList(data.relatedCases || [], (item) => `${item.caseNumber} — ${item.title} — ${item.status}`)}
    <h3>Next Actions</h3>
    ${renderSimpleList((data.recommendedNextActions || []).map((text) => ({ text })), (item) => item.text)}
  `;
}

function buildQuery(values) {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    if (value) params.set(key, value);
  }
  params.set("take", "250");
  return params.toString();
}

function renderIntegratedMetrics() {
  setMetric("metricCases", integrationState.cases.length);
  setMetric("metricAgents", integrationState.agents.length);
  setMetric("metricSources", window.siemState?.sources?.length || 0);
  setMetric("metricQueuedJobs", (window.siemState?.ingestionJobs || []).filter((job) => String(job.status || "").toLowerCase() === "queued").length);
}

function showIntegratedView(name) {
  document.querySelectorAll(".nav-item").forEach((item) => item.classList.toggle("active", item.dataset.view === name));
  document.querySelectorAll(".view").forEach((view) => view.classList.toggle("active", view.id === `view-${name}`));

  if (name === "agents") refreshAgents();
  if (name === "cases") refreshCases();
  if (name === "investigations") runInvestigationViews();
}

function createCaseFromSelectedEvent() {
  const event = window.siemState?.selectedEvent;
  if (!event) {
    showToast("Select a SIEM event first");
    return;
  }

  showIntegratedView("cases");
  const form = document.getElementById("caseCreateForm");
  if (!form) return;
  form.elements.title.value = `${event.severity || "Security"} event: ${event.eventType || "SIEM event"}`;
  form.elements.description.value = `${event.message || ""}\n\nSource: ${event.sourceName || event.source || "Unknown"}\nHost: ${event.host || "Unknown"}`;
  form.elements.severity.value = event.severity || "Medium";

  document.getElementById("caseCreateResult").innerHTML = `<p class="muted-text">Case form populated from selected event. Submit to create the case, then link the event from the case detail.</p>`;
}

const originalRenderSiemEventDetail = window.renderSiemEventDetail;
if (typeof originalRenderSiemEventDetail === "function") {
  window.renderSiemEventDetail = function integratedRenderSiemEventDetail(event) {
    originalRenderSiemEventDetail(event);
    const target = document.getElementById("siemEventDetail");
    if (!target) return;
    const actions = document.createElement("div");
    actions.className = "pivot-list integrated-event-actions";
    actions.innerHTML = `
      <button class="ghost" type="button" id="createCaseFromEventButton">Create case from event</button>
      <button class="ghost" type="button" id="openEmailInvestigationButton">Email view</button>
      <button class="ghost" type="button" id="openCloudInvestigationButton">Cloud view</button>
      <button class="ghost" type="button" id="openWindowsInvestigationButton">Windows view</button>
    `;
    target.prepend(actions);
    document.getElementById("createCaseFromEventButton")?.addEventListener("click", createCaseFromSelectedEvent);
    document.getElementById("openEmailInvestigationButton")?.addEventListener("click", () => { seedInvestigationForm(event); showIntegratedView("investigations"); loadInvestigationView("email", `/api/investigations/views/email?${buildQuery({ user: event.user, domain: event.domain })}`, "emailInvestigationView"); });
    document.getElementById("openCloudInvestigationButton")?.addEventListener("click", () => { seedInvestigationForm(event); showIntegratedView("investigations"); loadInvestigationView("cloud", `/api/investigations/views/cloud?${buildQuery({ user: event.user, ipAddress: event.sourceIp, tenantId: event.cloudTenantId, resourceId: event.cloudResourceId, action: event.action })}`, "cloudInvestigationView"); });
    document.getElementById("openWindowsInvestigationButton")?.addEventListener("click", () => { seedInvestigationForm(event); showIntegratedView("investigations"); loadInvestigationView("windows", `/api/investigations/views/windows?${buildQuery({ host: event.host, user: event.user, process: event.processName, ipAddress: event.sourceIp })}`, "windowsInvestigationView"); });
  };
}

function seedInvestigationForm(event) {
  const form = document.getElementById("investigationScopeForm");
  if (!form) return;
  form.elements.user.value = event.user || "";
  form.elements.host.value = event.host || "";
  form.elements.ipAddress.value = event.sourceIp || event.destinationIp || "";
  form.elements.domain.value = event.domain || "";
  form.elements.process.value = event.processName || "";
  form.elements.action.value = event.action || "";
  form.elements.tenantId.value = event.cloudTenantId || "";
  form.elements.resourceId.value = event.cloudResourceId || "";
}
