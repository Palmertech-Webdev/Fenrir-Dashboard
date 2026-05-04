const correlationState = {
  rules: [],
  alerts: [],
  graph: null
};

document.addEventListener("DOMContentLoaded", () => {
  installCorrelationNavigation();
  installCorrelationView();
  bindCorrelationDashboard();
  refreshCorrelationDashboard();
});

function installCorrelationNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="correlation"]')) {
    return;
  }

  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "correlation";
  button.type = "button";
  button.textContent = "Correlation";
  button.addEventListener("click", () => showCorrelationView());
  nav.appendChild(button);
}

function installCorrelationView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-correlation")) {
    return;
  }

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-correlation">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 10</p>
          <h2>Correlation, entity graph and incident stitching</h2>
          <p class="muted-text">Run built-in correlation rules across normalised telemetry, generate alerts, map related entities and pivot suspicious activity into investigations.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="runCorrelationButton" type="button">Run Correlation</button>
          <button class="ghost" id="refreshCorrelationButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Rules</span><strong id="metricCorrelationRules">0</strong></article>
        <article class="metric"><span>Enabled Rules</span><strong id="metricCorrelationEnabled">0</strong></article>
        <article class="metric"><span>Open Alerts</span><strong id="metricCorrelationAlerts">0</strong></article>
        <article class="metric"><span>Graph Nodes</span><strong id="metricCorrelationNodes">0</strong></article>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Rule Runner</h2><span class="pill Informational">SIEM/XDR layer</span></div>
          <form id="correlationRunForm" class="tool-form">
            <div class="form-grid">
              <label>Lookback minutes<input name="lookbackMinutes" type="number" min="5" max="10080" value="1440"></label>
              <label>Event limit<input name="take" type="number" min="50" max="5000" value="1000"></label>
            </div>
            <label>Specific rule<select name="ruleId" id="correlationRuleSelect"><option value="">All enabled rules</option></select></label>
            <button type="submit">Run Correlation</button>
          </form>
          <div class="result-box" id="correlationRunResult"></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Create Custom Rule</h2><span class="muted-text">Stored for later expansion</span></div>
          <form id="correlationRuleForm" class="tool-form">
            <label>Name<input name="name" type="text" placeholder="Repeated suspicious activity" required></label>
            <label>Description<textarea name="description" rows="3" placeholder="Describe what this rule should catch" required></textarea></label>
            <div class="form-grid">
              <label>Severity<select name="severity"><option>Low</option><option selected>Medium</option><option>High</option><option>Critical</option></select></label>
              <label>Threshold<input name="threshold" type="number" min="1" value="3"></label>
            </div>
            <div class="form-grid">
              <label>Window minutes<input name="timeWindowMinutes" type="number" min="1" value="60"></label>
              <label>Group by<input name="groupByFields" type="text" placeholder="User,SourceIp"></label>
            </div>
            <label>Query definition<input name="queryDefinition" type="text" placeholder="custom"></label>
            <div class="form-grid">
              <label>MITRE tactic<input name="mitreTactic" type="text" placeholder="Credential Access"></label>
              <label>MITRE technique<input name="mitreTechnique" type="text" placeholder="T1110"></label>
            </div>
            <button type="submit">Create Rule</button>
          </form>
        </section>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Correlation Rules</h2><span class="muted-text">Built-in rules are seeded automatically</span></div>
          <div class="table-wrap"><table><thead><tr><th>Enabled</th><th>Name</th><th>Severity</th><th>Logic</th><th>MITRE</th></tr></thead><tbody id="correlationRuleRows"></tbody></table></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Correlation Alerts</h2><span class="muted-text">Click alert to build graph</span></div>
          <div class="table-wrap"><table><thead><tr><th>Severity</th><th>Alert</th><th>Rule</th><th>Events</th><th>Last Seen</th></tr></thead><tbody id="correlationAlertRows"></tbody></table></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading">
          <h2>Entity Graph</h2>
          <button class="ghost" id="loadGlobalGraphButton" type="button">Build Global Graph</button>
        </div>
        <div id="correlationGraphNarrative" class="result-box"><p class="muted-text">Run correlation or select an alert to build an entity graph.</p></div>
        <div class="table-wrap correlation-graph-grid">
          <table><thead><tr><th>Node</th><th>Type</th><th>Weight</th></tr></thead><tbody id="correlationGraphNodeRows"></tbody></table>
          <table><thead><tr><th>From</th><th>Relationship</th><th>To</th><th>Weight</th></tr></thead><tbody id="correlationGraphEdgeRows"></tbody></table>
        </div>
      </section>
    </section>
  `);

  installCorrelationMetricsOnMainDashboard();
}

function installCorrelationMetricsOnMainDashboard() {
  const dashboardMetricGrid = document.querySelector("#view-dashboard .metric-grid");
  if (!dashboardMetricGrid || document.getElementById("metricCorrelationDashboardAlerts")) {
    return;
  }

  dashboardMetricGrid.insertAdjacentHTML("beforeend", `
    <article class="metric"><span>Correlation Alerts</span><strong id="metricCorrelationDashboardAlerts">0</strong></article>
  `);
}

function bindCorrelationDashboard() {
  document.getElementById("refreshCorrelationButton")?.addEventListener("click", refreshCorrelationDashboard);
  document.getElementById("runCorrelationButton")?.addEventListener("click", runCorrelation);
  document.getElementById("loadGlobalGraphButton")?.addEventListener("click", () => loadEntityGraph(null));

  document.getElementById("correlationRunForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      await runCorrelation();
    }, "Running...");
  });

  document.getElementById("correlationRuleForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      const payload = {
        name: form.get("name"),
        description: form.get("description"),
        severity: form.get("severity") || "Medium",
        enabled: true,
        ruleType: "custom",
        queryDefinition: emptyToNull(form.get("queryDefinition")) || "custom",
        timeWindowMinutes: Number(form.get("timeWindowMinutes") || 60),
        groupByFields: emptyToNull(form.get("groupByFields")),
        threshold: Number(form.get("threshold") || 1),
        mitreTactic: emptyToNull(form.get("mitreTactic")),
        mitreTechnique: emptyToNull(form.get("mitreTechnique"))
      };

      try {
        await api("/api/correlation/rules", { method: "POST", body: payload });
        event.currentTarget.reset();
        await refreshCorrelationRules();
        showToast("Correlation rule created");
      } catch (error) {
        showToast(`Rule creation failed: ${error.message}`);
      }
    }, "Creating...");
  });

  document.getElementById("correlationAlertRows")?.addEventListener("click", async (event) => {
    const row = event.target.closest("tr[data-alert-id]");
    if (!row) return;
    await loadEntityGraph(row.dataset.alertId);
  });

  document.getElementById("correlationAlertRows")?.addEventListener("keydown", async (event) => {
    if (event.key !== "Enter" && event.key !== " ") return;
    const row = event.target.closest("tr[data-alert-id]");
    if (!row) return;
    event.preventDefault();
    await loadEntityGraph(row.dataset.alertId);
  });
}

async function refreshCorrelationDashboard() {
  await Promise.allSettled([refreshCorrelationRules(), refreshCorrelationAlerts(), loadEntityGraph(null, false)]);
  renderCorrelationMetrics();
}

async function refreshCorrelationRules() {
  try {
    correlationState.rules = await api("/api/correlation/rules");
  } catch (error) {
    correlationState.rules = [];
    showToast(`Correlation rules unavailable: ${error.message}`);
  }
  renderCorrelationRules();
  renderCorrelationMetrics();
}

async function refreshCorrelationAlerts() {
  try {
    correlationState.alerts = await api("/api/correlation/alerts");
  } catch (error) {
    correlationState.alerts = [];
    showToast(`Correlation alerts unavailable: ${error.message}`);
  }
  renderCorrelationAlerts();
  renderCorrelationMetrics();
}

async function runCorrelation() {
  const form = document.getElementById("correlationRunForm");
  const formData = form ? new FormData(form) : new FormData();
  const payload = {
    ruleId: emptyToNull(formData.get("ruleId")),
    lookbackMinutes: Number(formData.get("lookbackMinutes") || 1440),
    take: Number(formData.get("take") || 1000)
  };

  if (!payload.ruleId) {
    payload.ruleId = null;
  }

  const target = document.getElementById("correlationRunResult");
  if (target) target.innerHTML = `<div class="result-title">Running correlation...</div>`;

  try {
    const result = await api("/api/correlation/run", { method: "POST", body: payload });
    if (target) {
      target.innerHTML = `
        <div class="result-title">${pill("Completed")} <span>${result.alertsCreated} alerts created</span></div>
        <p>${result.rulesEvaluated} rules evaluated between ${formatDate(result.startedAtUtc)} and ${formatDate(result.completedAtUtc)}.</p>
      `;
    }
    await refreshCorrelationAlerts();
    await loadEntityGraph(result.alerts?.[0]?.id || null);
    showToast("Correlation run completed");
  } catch (error) {
    if (target) target.innerHTML = renderError(error);
    showToast(`Correlation failed: ${error.message}`);
  }
}

async function loadEntityGraph(alertId, showErrors = true) {
  const query = new URLSearchParams();
  query.set("lookbackMinutes", "1440");
  if (alertId) query.set("alertId", alertId);

  try {
    correlationState.graph = await api(`/api/correlation/graph?${query.toString()}`);
    renderCorrelationGraph();
    renderCorrelationMetrics();
  } catch (error) {
    correlationState.graph = null;
    if (showErrors) showToast(`Entity graph unavailable: ${error.message}`);
  }
}

function renderCorrelationRules() {
  renderRows("correlationRuleRows", correlationState.rules, (rule) => `
    <tr>
      <td>${rule.enabled ? "Yes" : "No"}</td>
      <td><strong>${escapeHtml(rule.name)}</strong><div class="muted-text">${escapeHtml(rule.description)}</div></td>
      <td>${pill(rule.severity)}</td>
      <td>${escapeHtml(rule.queryDefinition)}<div class="muted-text">Window: ${rule.timeWindowMinutes}m · Threshold: ${rule.threshold}</div></td>
      <td>${escapeHtml(rule.mitreTactic || "")}<div class="muted-text">${escapeHtml(rule.mitreTechnique || "")}</div></td>
    </tr>
  `);

  const select = document.getElementById("correlationRuleSelect");
  if (select) {
    const current = select.value;
    select.innerHTML = `<option value="">All enabled rules</option>` + correlationState.rules.map((rule) => `<option value="${escapeHtml(rule.id)}">${escapeHtml(rule.name)}</option>`).join("");
    select.value = current;
  }
}

function renderCorrelationAlerts() {
  renderRows("correlationAlertRows", correlationState.alerts, (alert) => `
    <tr data-alert-id="${escapeHtml(alert.id)}" tabindex="0" role="button" aria-label="Open correlation alert ${escapeHtml(alert.title)}">
      <td>${pill(alert.severity)}</td>
      <td><strong>${escapeHtml(alert.title)}</strong><div class="muted-text">${escapeHtml(alert.description)}</div></td>
      <td>${escapeHtml(alert.ruleName)}<div class="muted-text">${escapeHtml(alert.mitreTactic || "")} ${escapeHtml(alert.mitreTechnique || "")}</div></td>
      <td>${Number(alert.eventIds?.length || 0)}</td>
      <td>${formatDate(alert.lastSeenUtc)}</td>
    </tr>
  `);
}

function renderCorrelationGraph() {
  const graph = correlationState.graph || { nodes: [], edges: [], narrative: [] };
  const narrative = document.getElementById("correlationGraphNarrative");
  if (narrative) {
    narrative.innerHTML = `<div class="result-title">Entity graph</div>${renderSimpleList((graph.narrative || []).map((text) => ({ text })), (item) => item.text)}`;
  }

  renderRows("correlationGraphNodeRows", graph.nodes || [], (node) => `
    <tr><td><strong>${escapeHtml(node.label)}</strong></td><td>${pill(node.type)}</td><td>${Number(node.weight || 0)}</td></tr>
  `);

  renderRows("correlationGraphEdgeRows", graph.edges || [], (edge) => `
    <tr><td>${escapeHtml(edge.from)}</td><td>${escapeHtml(edge.relationship)}</td><td>${escapeHtml(edge.to)}</td><td>${Number(edge.weight || 0)}</td></tr>
  `);
}

function renderCorrelationMetrics() {
  const enabledRules = correlationState.rules.filter((rule) => rule.enabled).length;
  const openAlerts = correlationState.alerts.filter((alert) => String(alert.status || "").toLowerCase() === "open").length;
  const graphNodes = correlationState.graph?.nodes?.length || 0;

  setMetric("metricCorrelationRules", correlationState.rules.length);
  setMetric("metricCorrelationEnabled", enabledRules);
  setMetric("metricCorrelationAlerts", openAlerts);
  setMetric("metricCorrelationNodes", graphNodes);
  setMetric("metricCorrelationDashboardAlerts", openAlerts);
}

function showCorrelationView() {
  document.querySelectorAll(".nav-item").forEach((item) => item.classList.toggle("active", item.dataset.view === "correlation"));
  document.querySelectorAll(".view").forEach((view) => view.classList.toggle("active", view.id === "view-correlation"));
  refreshCorrelationDashboard();
}
