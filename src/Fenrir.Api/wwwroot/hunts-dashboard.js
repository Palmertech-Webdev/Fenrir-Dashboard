const huntState = {
  packs: [],
  runs: [],
  collections: [],
  selectedPackId: null,
  selectedRunId: null
};

document.addEventListener("DOMContentLoaded", () => {
  installHuntNavigation();
  installHuntView();
  bindHuntDashboard();
  refreshHuntDashboard();
});

function installHuntNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="hunts"]')) return;
  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "hunts";
  button.type = "button";
  button.textContent = "Hunts / DFIR";
  button.addEventListener("click", showHuntView);
  nav.appendChild(button);
}

function installHuntView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-hunts")) return;

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-hunts">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 12</p>
          <h2>Hunt packs and DFIR collection workflows</h2>
          <p class="muted-text">Run reusable threat hunts across normalised SIEM data and queue DFIR evidence collection requests for endpoints.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="refreshHuntsButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Hunt Packs</span><strong id="metricHuntPacks">0</strong></article>
        <article class="metric"><span>Enabled Packs</span><strong id="metricHuntEnabled">0</strong></article>
        <article class="metric"><span>Hunt Runs</span><strong id="metricHuntRuns">0</strong></article>
        <article class="metric"><span>DFIR Requests</span><strong id="metricDfirCollections">0</strong></article>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Create Hunt Pack</h2><span class="muted-text">Reusable detection hypothesis</span></div>
          <form id="huntPackForm" class="tool-form">
            <label>Name<input name="name" type="text" placeholder="Suspicious admin behaviour" required></label>
            <label>Description<textarea name="description" rows="3" placeholder="What should this hunt identify?" required></textarea></label>
            <div class="form-grid">
              <label>Category<select name="category"><option>endpoint</option><option>identity</option><option>email</option><option>cloud</option><option>network</option><option>general</option></select></label>
              <label>Severity<select name="severity"><option>Low</option><option selected>Medium</option><option>High</option><option>Critical</option></select></label>
            </div>
            <div class="form-grid">
              <label>MITRE tactic<input name="mitreTactic" type="text" value="Discovery"></label>
              <label>MITRE technique<input name="mitreTechnique" type="text" placeholder="T1059.001"></label>
            </div>
            <button type="submit">Create Hunt Pack</button>
          </form>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Add Hunt Query</h2><span class="muted-text">Structured field-focused query</span></div>
          <form id="huntQueryForm" class="tool-form">
            <label>Hunt pack<select name="huntPackId" id="huntQueryPackSelect"></select></label>
            <label>Name<input name="name" type="text" placeholder="Encoded PowerShell" required></label>
            <label>Description<textarea name="description" rows="3" placeholder="Describe the expected evidence" required></textarea></label>
            <div class="form-grid">
              <label>Target field<select name="targetField"><option>Message</option><option>CommandLine</option><option>Action</option><option>EventType</option><option>EventCategory</option><option>User</option><option>Host</option><option>SourceIp</option><option>DestinationIp</option><option>Domain</option><option>FileHashSha256</option></select></label>
              <label>Sort order<input name="sortOrder" type="number" value="10"></label>
            </div>
            <label>Query tokens<input name="queryDefinition" type="text" placeholder="encodedcommand downloadstring suspicious"></label>
            <label>Expected evidence<input name="expectedEvidence" type="text" placeholder="Encoded command line or suspicious action"></label>
            <button type="submit">Add Query</button>
          </form>
        </section>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Hunt Packs</h2><span class="muted-text">Click a pack to run it</span></div>
          <div class="table-wrap"><table><thead><tr><th>Severity</th><th>Name</th><th>Category</th><th>Queries</th><th>MITRE</th></tr></thead><tbody id="huntPackRows"></tbody></table></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Run Hunt</h2><span class="muted-text">Search recent SIEM telemetry</span></div>
          <form id="huntRunForm" class="tool-form">
            <label>Hunt pack<select name="huntPackId" id="huntRunPackSelect"></select></label>
            <div class="form-grid">
              <label>Lookback hours<input name="lookbackHours" type="number" min="1" max="2160" value="24"></label>
              <label>Case ID<input name="caseId" type="text" placeholder="Optional"></label>
            </div>
            <label>Scope<input name="scope" type="text" placeholder="Optional note, asset group or customer scope"></label>
            <button type="submit">Run Hunt</button>
          </form>
          <div class="result-box" id="huntRunResult"><p class="muted-text">Run a hunt pack to see matches.</p></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Hunt Runs</h2><span class="muted-text">Click a run to review matches</span></div>
        <div class="table-wrap"><table><thead><tr><th>Status</th><th>Pack</th><th>Matches</th><th>Lookback</th><th>Started</th></tr></thead><tbody id="huntRunRows"></tbody></table></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Run Results</h2><span class="muted-text">Evidence returned from hunt queries</span></div>
        <div id="huntRunDetail" class="result-box"><p class="muted-text">Select a hunt run.</p></div>
      </section>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Request DFIR Collection</h2><span class="muted-text">Queued for endpoint workflow</span></div>
          <form id="dfirCollectionForm" class="tool-form">
            <label>Hostname<input name="hostname" type="text" placeholder="LAPTOP-001" required></label>
            <div class="form-grid">
              <label>Collection type<select name="collectionType"><option>triage</option><option>deep</option><option>persistence</option><option>network</option><option>malware</option></select></label>
              <label>Case ID<input name="caseId" type="text" placeholder="Optional"></label>
            </div>
            <label>Artefacts<input name="artefacts" type="text" placeholder="processes, network_connections, services"></label>
            <label>Notes<textarea name="notes" rows="3" placeholder="Why is this collection required?"></textarea></label>
            <button type="submit">Queue DFIR Collection</button>
          </form>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>DFIR Collections</h2><span class="muted-text">Evidence workflow queue</span></div>
          <div class="table-wrap"><table><thead><tr><th>Status</th><th>Host</th><th>Type</th><th>Artefacts</th><th>Requested</th></tr></thead><tbody id="dfirCollectionRows"></tbody></table></div>
        </section>
      </div>
    </section>
  `);
  installHuntDashboardMetric();
}

function installHuntDashboardMetric() {
  const grid = document.querySelector("#view-dashboard .metric-grid");
  if (!grid || document.getElementById("metricHuntDashboardRuns")) return;
  grid.insertAdjacentHTML("beforeend", `<article class="metric"><span>Hunt Matches</span><strong id="metricHuntDashboardRuns">0</strong></article>`);
}

function bindHuntDashboard() {
  document.getElementById("refreshHuntsButton")?.addEventListener("click", refreshHuntDashboard);

  document.getElementById("huntPackForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      name: form.get("name"),
      description: form.get("description"),
      category: form.get("category") || "general",
      severity: form.get("severity") || "Medium",
      mitreTactic: form.get("mitreTactic") || "Discovery",
      mitreTechnique: emptyToNull(form.get("mitreTechnique")),
      isEnabled: true
    };
    try {
      const created = await api("/api/hunts/packs", { method: "POST", body: payload });
      huntState.selectedPackId = created.id;
      event.currentTarget.reset();
      await refreshHuntPacks();
      showToast("Hunt pack created");
    } catch (error) {
      showToast(`Hunt pack creation failed: ${error.message}`);
    }
  });

  document.getElementById("huntQueryForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const packId = form.get("huntPackId");
    if (!packId) return;
    const payload = {
      name: form.get("name"),
      description: form.get("description"),
      queryType: "siem_structured",
      queryDefinition: form.get("queryDefinition") || "",
      targetField: form.get("targetField") || "Message",
      expectedEvidence: emptyToNull(form.get("expectedEvidence")),
      sortOrder: Number(form.get("sortOrder") || 0)
    };
    try {
      await api(`/api/hunts/packs/${packId}/queries`, { method: "POST", body: payload });
      event.currentTarget.reset();
      await refreshHuntPacks();
      showToast("Hunt query added");
    } catch (error) {
      showToast(`Query creation failed: ${error.message}`);
    }
  });

  document.getElementById("huntRunForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      huntPackId: form.get("huntPackId"),
      lookbackHours: Number(form.get("lookbackHours") || 24),
      startedBy: "analyst",
      scope: emptyToNull(form.get("scope")),
      caseId: emptyToNull(form.get("caseId"))
    };
    try {
      const run = await api("/api/hunts/runs", { method: "POST", body: payload });
      huntState.selectedRunId = run.id;
      document.getElementById("huntRunResult").innerHTML = `<div class="result-title">${pill(run.status)} <span>${run.matches} matches</span></div><p>${escapeHtml(run.huntPackName)} completed across ${run.lookbackHours} hours.</p>`;
      await refreshHuntRuns();
      renderHuntRunDetail(run);
      showToast("Hunt run completed");
    } catch (error) {
      document.getElementById("huntRunResult").innerHTML = renderError(error);
      showToast(`Hunt run failed: ${error.message}`);
    }
  });

  document.getElementById("dfirCollectionForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const artefacts = String(form.get("artefacts") || "").split(",").map(item => item.trim()).filter(Boolean);
    const payload = {
      hostname: form.get("hostname"),
      collectionType: form.get("collectionType") || "triage",
      caseId: emptyToNull(form.get("caseId")),
      requestedBy: "analyst",
      artefacts: artefacts.length ? artefacts : null,
      notes: emptyToNull(form.get("notes"))
    };
    try {
      await api("/api/hunts/dfir-collections", { method: "POST", body: payload });
      event.currentTarget.reset();
      await refreshDfirCollections();
      showToast("DFIR collection queued");
    } catch (error) {
      showToast(`DFIR request failed: ${error.message}`);
    }
  });

  document.getElementById("huntPackRows")?.addEventListener("click", event => {
    const row = event.target.closest("tr[data-pack-id]");
    if (!row) return;
    huntState.selectedPackId = row.dataset.packId;
    updateHuntSelects();
  });

  document.getElementById("huntRunRows")?.addEventListener("click", event => {
    const row = event.target.closest("tr[data-run-id]");
    if (!row) return;
    const run = huntState.runs.find(item => item.id === row.dataset.runId);
    if (run) renderHuntRunDetail(run);
  });
}

async function refreshHuntDashboard() {
  await Promise.allSettled([refreshHuntPacks(), refreshHuntRuns(), refreshDfirCollections()]);
  renderHuntMetrics();
}

async function refreshHuntPacks() {
  try {
    huntState.packs = await api("/api/hunts/packs");
  } catch (error) {
    huntState.packs = [];
    showToast(`Hunt packs unavailable: ${error.message}`);
  }
  renderHuntPacks();
  updateHuntSelects();
  renderHuntMetrics();
}

async function refreshHuntRuns() {
  try {
    huntState.runs = await api("/api/hunts/runs");
  } catch (error) {
    huntState.runs = [];
    showToast(`Hunt runs unavailable: ${error.message}`);
  }
  renderHuntRuns();
  renderHuntMetrics();
}

async function refreshDfirCollections() {
  try {
    huntState.collections = await api("/api/hunts/dfir-collections");
  } catch (error) {
    huntState.collections = [];
    showToast(`DFIR collections unavailable: ${error.message}`);
  }
  renderDfirCollections();
  renderHuntMetrics();
}

function renderHuntPacks() {
  renderRows("huntPackRows", huntState.packs, pack => `
    <tr data-pack-id="${escapeHtml(pack.id)}">
      <td>${pill(pack.severity)}</td>
      <td><strong>${escapeHtml(pack.name)}</strong><div class="muted-text">${escapeHtml(pack.description)}</div></td>
      <td>${pill(pack.category)}</td>
      <td>${Number(pack.queries?.length || 0)}</td>
      <td>${escapeHtml(pack.mitreTactic || "")}<div class="muted-text">${escapeHtml(pack.mitreTechnique || "")}</div></td>
    </tr>
  `);
}

function renderHuntRuns() {
  renderRows("huntRunRows", huntState.runs, run => `
    <tr data-run-id="${escapeHtml(run.id)}">
      <td>${pill(run.status)}</td>
      <td><strong>${escapeHtml(run.huntPackName)}</strong><div class="muted-text">${escapeHtml(run.scope || "")}</div></td>
      <td>${Number(run.matches || 0)}</td>
      <td>${Number(run.lookbackHours || 0)}h</td>
      <td>${formatDate(run.startedAtUtc)}</td>
    </tr>
  `);
}

function renderHuntRunDetail(run) {
  huntState.selectedRunId = run.id;
  const target = document.getElementById("huntRunDetail");
  if (!target) return;
  target.innerHTML = `
    <div class="result-title">${pill(run.status)} <span>${escapeHtml(run.huntPackName)}</span><span>${Number(run.matches || 0)} matches</span></div>
    ${(run.results || []).length ? (run.results || []).map(result => `
      <div class="result-box compact-step">
        <div class="result-title">${pill(result.severity)} <span>${escapeHtml(result.queryName)}</span></div>
        <p>${escapeHtml(result.summary)}</p>
        <pre class="json-output">${escapeHtml(result.evidence)}</pre>
      </div>
    `).join("") : `<p class="muted-text">No matches were returned for this hunt.</p>`}
  `;
}

function renderDfirCollections() {
  renderRows("dfirCollectionRows", huntState.collections, item => `
    <tr>
      <td>${pill(item.status)}</td>
      <td><strong>${escapeHtml(item.hostname)}</strong><div class="muted-text">${escapeHtml(item.notes || "")}</div></td>
      <td>${escapeHtml(item.collectionType)}</td>
      <td>${escapeHtml((item.artefacts || []).join(", "))}</td>
      <td>${formatDate(item.requestedAtUtc)}</td>
    </tr>
  `);
}

function updateHuntSelects() {
  const options = huntState.packs.map(pack => `<option value="${escapeHtml(pack.id)}">${escapeHtml(pack.name)}</option>`).join("");
  ["huntQueryPackSelect", "huntRunPackSelect"].forEach(id => {
    const select = document.getElementById(id);
    if (!select) return;
    select.innerHTML = options;
    if (huntState.selectedPackId) select.value = huntState.selectedPackId;
  });
}

function renderHuntMetrics() {
  const enabled = huntState.packs.filter(pack => pack.isEnabled).length;
  const matches = huntState.runs.reduce((sum, run) => sum + Number(run.matches || 0), 0);
  setMetric("metricHuntPacks", huntState.packs.length);
  setMetric("metricHuntEnabled", enabled);
  setMetric("metricHuntRuns", huntState.runs.length);
  setMetric("metricDfirCollections", huntState.collections.length);
  setMetric("metricHuntDashboardRuns", matches);
}

function showHuntView() {
  document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.view === "hunts"));
  document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === "view-hunts"));
  refreshHuntDashboard();
}

function setMetric(id, value) {
  const element = document.getElementById(id);
  if (element) element.textContent = value;
}

function emptyToNull(value) {
  const text = String(value || "").trim();
  return text.length ? text : null;
}
