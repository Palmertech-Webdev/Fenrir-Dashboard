const responseState = {
  playbooks: [],
  runs: [],
  selectedPlaybookId: null,
  selectedRunId: null
};

document.addEventListener("DOMContentLoaded", () => {
  installResponseNavigation();
  installResponseView();
  bindResponseDashboard();
  refreshResponseDashboard();
});

function installResponseNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="response"]')) return;
  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "response";
  button.type = "button";
  button.textContent = "Response";
  button.addEventListener("click", showResponseView);
  nav.appendChild(button);
}

function installResponseView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-response")) return;

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-response">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 11</p>
          <h2>Response integrations and analyst-approved playbooks</h2>
          <p class="muted-text">Turn correlated alerts and cases into controlled response workflows. Destructive actions are intentionally approval-gated and recorded as playbook steps.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="refreshResponseButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Playbooks</span><strong id="metricResponsePlaybooks">0</strong></article>
        <article class="metric"><span>Enabled</span><strong id="metricResponseEnabled">0</strong></article>
        <article class="metric"><span>Runs</span><strong id="metricResponseRuns">0</strong></article>
        <article class="metric"><span>Active Runs</span><strong id="metricResponseActive">0</strong></article>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Create Playbook</h2><span class="muted-text">Analyst-approved by default</span></div>
          <form id="responsePlaybookForm" class="tool-form">
            <label>Name<input name="name" type="text" placeholder="Suspicious sign-in containment" required></label>
            <label>Description<textarea name="description" rows="3" placeholder="Describe the response workflow" required></textarea></label>
            <div class="form-grid">
              <label>Category<select name="category"><option>identity</option><option>endpoint</option><option>email</option><option>cloud</option><option>network</option><option>general</option></select></label>
              <label>Severity<select name="severity"><option>Low</option><option selected>Medium</option><option>High</option><option>Critical</option></select></label>
            </div>
            <div class="form-grid">
              <label>MITRE tactic<input name="mitreTactic" type="text" placeholder="Credential Access"></label>
              <label>MITRE technique<input name="mitreTechnique" type="text" placeholder="T1078"></label>
            </div>
            <button type="submit">Create Playbook</button>
          </form>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Add Step</h2><span class="muted-text">Select a playbook first</span></div>
          <form id="responseStepForm" class="tool-form">
            <label>Playbook<select name="playbookId" id="responseStepPlaybookSelect"></select></label>
            <label>Title<input name="title" type="text" placeholder="Validate the evidence" required></label>
            <label>Description<textarea name="description" rows="3" placeholder="What should the analyst do?" required></textarea></label>
            <div class="form-grid">
              <label>Action type<select name="actionType"><option>manual</option><option>approval_required</option><option>integration</option><option>evidence_collection</option></select></label>
              <label>Target type<select name="targetType"><option>analyst</option><option>case</option><option>identity</option><option>endpoint</option><option>mailbox</option><option>cloud</option></select></label>
            </div>
            <label>Command preview<input name="commandPreview" type="text" placeholder="Human-readable approved action only"></label>
            <div class="form-grid">
              <label>Integration key<input name="integrationKey" type="text" placeholder="manual_identity"></label>
              <label>Sort order<input name="sortOrder" type="number" value="10"></label>
            </div>
            <label class="checkbox-row"><input name="requiresApproval" type="checkbox" checked> Requires approval</label>
            <button type="submit">Add Step</button>
          </form>
        </section>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Playbooks</h2><span class="muted-text">Click a playbook to prepare a run</span></div>
          <div class="table-wrap"><table><thead><tr><th>Severity</th><th>Name</th><th>Category</th><th>Steps</th><th>MITRE</th></tr></thead><tbody id="responsePlaybookRows"></tbody></table></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Start Run</h2><span class="muted-text">Link to alert, case or event</span></div>
          <form id="responseRunForm" class="tool-form">
            <label>Playbook<select name="playbookId" id="responseRunPlaybookSelect"></select></label>
            <div class="form-grid">
              <label>Case ID<input name="caseId" type="text" placeholder="Optional"></label>
              <label>Alert ID<input name="alertId" type="text" placeholder="Optional"></label>
            </div>
            <label>Event ID<input name="eventId" type="text" placeholder="Optional"></label>
            <label>Notes<textarea name="notes" rows="3" placeholder="Why is this response being started?"></textarea></label>
            <button type="submit">Start Playbook Run</button>
          </form>
          <div class="result-box" id="responseRecommendationBox"><p class="muted-text">Recommendations will appear here when started from an alert or correlation context.</p></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Playbook Runs</h2><span class="muted-text">Click a run to execute/record steps</span></div>
        <div class="table-wrap"><table><thead><tr><th>Status</th><th>Playbook</th><th>Linked Entity</th><th>Started By</th><th>Started</th></tr></thead><tbody id="responseRunRows"></tbody></table></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Run Detail</h2><span class="muted-text">Record outcomes without hiding analyst approval</span></div>
        <div id="responseRunDetail" class="result-box"><p class="muted-text">Select a playbook run.</p></div>
      </section>
    </section>
  `);
  installResponseDashboardMetric();
}

function installResponseDashboardMetric() {
  const grid = document.querySelector("#view-dashboard .metric-grid");
  if (!grid || document.getElementById("metricResponseDashboardActive")) return;
  grid.insertAdjacentHTML("beforeend", `<article class="metric"><span>Active Responses</span><strong id="metricResponseDashboardActive">0</strong></article>`);
}

function bindResponseDashboard() {
  document.getElementById("refreshResponseButton")?.addEventListener("click", refreshResponseDashboard);

  document.getElementById("responsePlaybookForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      name: form.get("name"),
      description: form.get("description"),
      category: form.get("category") || "general",
      severity: form.get("severity") || "Medium",
      triggerType: "manual",
      mitreTactic: emptyToNull(form.get("mitreTactic")),
      mitreTechnique: emptyToNull(form.get("mitreTechnique")),
      isEnabled: true
    };
    try {
      const created = await api("/api/response-playbooks", { method: "POST", body: payload });
      event.currentTarget.reset();
      responseState.selectedPlaybookId = created.id;
      await refreshResponsePlaybooks();
      showToast("Response playbook created");
    } catch (error) {
      showToast(`Playbook creation failed: ${error.message}`);
    }
  });

  document.getElementById("responseStepForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const playbookId = form.get("playbookId");
    if (!playbookId) return;
    const payload = {
      title: form.get("title"),
      description: form.get("description"),
      actionType: form.get("actionType") || "manual",
      targetType: form.get("targetType") || "analyst",
      commandPreview: emptyToNull(form.get("commandPreview")),
      integrationKey: emptyToNull(form.get("integrationKey")),
      requiresApproval: form.get("requiresApproval") === "on",
      sortOrder: Number(form.get("sortOrder") || 0)
    };
    try {
      await api(`/api/response-playbooks/${playbookId}/steps`, { method: "POST", body: payload });
      event.currentTarget.reset();
      await refreshResponsePlaybooks();
      showToast("Step added");
    } catch (error) {
      showToast(`Step creation failed: ${error.message}`);
    }
  });

  document.getElementById("responseRunForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      playbookId: form.get("playbookId"),
      caseId: emptyToNull(form.get("caseId")),
      alertId: emptyToNull(form.get("alertId")),
      eventId: emptyToNull(form.get("eventId")),
      startedBy: "analyst",
      notes: emptyToNull(form.get("notes"))
    };
    try {
      const run = await api("/api/response-playbooks/runs", { method: "POST", body: payload });
      responseState.selectedRunId = run.id;
      await refreshResponseRuns();
      renderResponseRunDetail(run);
      showToast("Playbook run started");
    } catch (error) {
      showToast(`Run failed: ${error.message}`);
    }
  });

  document.getElementById("responsePlaybookRows")?.addEventListener("click", event => {
    const row = event.target.closest("tr[data-playbook-id]");
    if (!row) return;
    responseState.selectedPlaybookId = row.dataset.playbookId;
    updateResponseSelects();
  });

  document.getElementById("responseRunRows")?.addEventListener("click", event => {
    const row = event.target.closest("tr[data-run-id]");
    if (!row) return;
    const run = responseState.runs.find(item => item.id === row.dataset.runId);
    if (run) renderResponseRunDetail(run);
  });

  document.getElementById("responseRunDetail")?.addEventListener("click", async event => {
    const button = event.target.closest("button[data-run-step-id]");
    if (!button) return;
    const runId = button.dataset.runId;
    const stepId = button.dataset.runStepId;
    const status = button.dataset.status;
    const result = document.getElementById(`responseStepResult-${stepId}`)?.value || `${status} by analyst`;
    try {
      const run = await api(`/api/response-playbooks/runs/${runId}/steps/${stepId}`, { method: "PATCH", body: { status, result, executedBy: "analyst" } });
      await refreshResponseRuns();
      renderResponseRunDetail(run);
      showToast("Run step updated");
    } catch (error) {
      showToast(`Step update failed: ${error.message}`);
    }
  });
}

async function refreshResponseDashboard() {
  await Promise.allSettled([refreshResponsePlaybooks(), refreshResponseRuns()]);
  renderResponseMetrics();
}

async function refreshResponsePlaybooks() {
  try {
    responseState.playbooks = await api("/api/response-playbooks");
  } catch (error) {
    responseState.playbooks = [];
    showToast(`Response playbooks unavailable: ${error.message}`);
  }
  renderResponsePlaybooks();
  updateResponseSelects();
  renderResponseMetrics();
}

async function refreshResponseRuns() {
  try {
    responseState.runs = await api("/api/response-playbooks/runs");
  } catch (error) {
    responseState.runs = [];
    showToast(`Response runs unavailable: ${error.message}`);
  }
  renderResponseRuns();
  renderResponseMetrics();
}

function renderResponsePlaybooks() {
  renderRows("responsePlaybookRows", responseState.playbooks, item => `
    <tr data-playbook-id="${escapeHtml(item.id)}">
      <td>${pill(item.severity)}</td>
      <td><strong>${escapeHtml(item.name)}</strong><div class="muted-text">${escapeHtml(item.description)}</div></td>
      <td>${pill(item.category)}</td>
      <td>${Number(item.steps?.length || 0)}</td>
      <td>${escapeHtml(item.mitreTactic || "")}<div class="muted-text">${escapeHtml(item.mitreTechnique || "")}</div></td>
    </tr>
  `);
}

function renderResponseRuns() {
  renderRows("responseRunRows", responseState.runs, item => `
    <tr data-run-id="${escapeHtml(item.id)}">
      <td>${pill(item.status)}</td>
      <td><strong>${escapeHtml(item.playbookName)}</strong></td>
      <td>${item.caseId ? `Case: ${escapeHtml(item.caseId)}` : item.alertId ? `Alert: ${escapeHtml(item.alertId)}` : item.eventId ? `Event: ${escapeHtml(item.eventId)}` : "Manual"}</td>
      <td>${escapeHtml(item.startedBy || "analyst")}</td>
      <td>${formatDate(item.startedAtUtc)}</td>
    </tr>
  `);
}

function renderResponseRunDetail(run) {
  responseState.selectedRunId = run.id;
  const target = document.getElementById("responseRunDetail");
  if (!target) return;
  target.innerHTML = `
    <div class="result-title">${pill(run.status)} <span>${escapeHtml(run.playbookName)}</span></div>
    <p>${escapeHtml(run.notes || "No run notes provided.")}</p>
    <h3>Steps</h3>
    ${(run.steps || []).map(step => `
      <div class="result-box compact-step">
        <div class="result-title">${pill(step.status)} <span>${escapeHtml(step.title)}</span></div>
        <p>${step.requiresApproval ? "Approval required before completion." : "No approval required."}</p>
        <p class="muted-text">${escapeHtml(step.result || "No result recorded.")}</p>
        <textarea id="responseStepResult-${escapeHtml(step.id)}" rows="2" placeholder="Record action outcome"></textarea>
        <div class="pivot-list">
          <button class="ghost" type="button" data-run-id="${escapeHtml(run.id)}" data-run-step-id="${escapeHtml(step.id)}" data-status="Completed">Mark Completed</button>
          <button class="ghost" type="button" data-run-id="${escapeHtml(run.id)}" data-run-step-id="${escapeHtml(step.id)}" data-status="Skipped">Skip</button>
          <button class="ghost" type="button" data-run-id="${escapeHtml(run.id)}" data-run-step-id="${escapeHtml(step.id)}" data-status="Blocked">Blocked</button>
        </div>
      </div>
    `).join("")}
  `;
}

function updateResponseSelects() {
  const options = responseState.playbooks.map(item => `<option value="${escapeHtml(item.id)}">${escapeHtml(item.name)}</option>`).join("");
  ["responseStepPlaybookSelect", "responseRunPlaybookSelect"].forEach(id => {
    const select = document.getElementById(id);
    if (!select) return;
    select.innerHTML = options;
    if (responseState.selectedPlaybookId) select.value = responseState.selectedPlaybookId;
  });
}

function renderResponseMetrics() {
  const enabled = responseState.playbooks.filter(item => item.isEnabled).length;
  const active = responseState.runs.filter(item => !["Completed", "Cancelled"].includes(item.status)).length;
  setMetric("metricResponsePlaybooks", responseState.playbooks.length);
  setMetric("metricResponseEnabled", enabled);
  setMetric("metricResponseRuns", responseState.runs.length);
  setMetric("metricResponseActive", active);
  setMetric("metricResponseDashboardActive", active);
}

function showResponseView() {
  document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.view === "response"));
  document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === "view-response"));
  refreshResponseDashboard();
}

window.startResponseForAlert = async function startResponseForAlert(alertId) {
  showResponseView();
  const form = document.getElementById("responseRunForm");
  if (form) form.elements.alertId.value = alertId || "";
  try {
    const recommendation = await api("/api/response-playbooks/recommendations", { method: "POST", body: { alertId } });
    const box = document.getElementById("responseRecommendationBox");
    if (box) {
      box.innerHTML = `<div class="result-title">${pill(recommendation.severity)} <span>${escapeHtml(recommendation.title)}</span></div><p>${escapeHtml(recommendation.rationale)}</p>${renderSimpleList((recommendation.recommendedActions || []).map(text => ({ text })), item => item.text)}`;
    }
    if (recommendation.recommendedPlaybookIds?.length && form) {
      form.elements.playbookId.value = recommendation.recommendedPlaybookIds[0];
    }
  } catch (error) {
    showToast(`Recommendation unavailable: ${error.message}`);
  }
};
