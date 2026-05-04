const roadmapState = {
  readiness: null,
  improvements: []
};

document.addEventListener("DOMContentLoaded", () => {
  installRoadmapNavigation();
  installRoadmapView();
  bindRoadmapDashboard();
  refreshRoadmapDashboard();
});

function installRoadmapNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="roadmap"]')) return;
  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "roadmap";
  button.type = "button";
  button.textContent = "Roadmap Status";
  button.addEventListener("click", showRoadmapView);
  nav.appendChild(button);
}

function installRoadmapView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-roadmap")) return;

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-roadmap">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Completion tracker</p>
          <h2>Fenrir phase readiness and improvement backlog</h2>
          <p class="muted-text">Track phases 0-15, what is implemented, what is connected to the dashboard, and what should be hardened next.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="refreshRoadmapButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Total Phases</span><strong id="metricRoadmapTotal">0</strong></article>
        <article class="metric"><span>Implemented</span><strong id="metricRoadmapImplemented">0</strong></article>
        <article class="metric"><span>Needs Hardening</span><strong id="metricRoadmapHardening">0</strong></article>
        <article class="metric"><span>Improvement Ideas</span><strong id="metricRoadmapImprovements">0</strong></article>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Phase Readiness</h2><span class="muted-text">All planned phases</span></div>
        <div class="table-wrap"><table><thead><tr><th>Phase</th><th>Status</th><th>Dashboard Surface</th><th>Summary</th><th>Next Hardening</th></tr></thead><tbody id="roadmapPhaseRows"></tbody></table></div>
      </section>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Add Improvement</h2><span class="muted-text">Capture your next ideas</span></div>
          <form id="roadmapImprovementForm" class="tool-form">
            <label>Title<input name="title" type="text" placeholder="Add PDF export for case reports" required></label>
            <div class="form-grid">
              <label>Area<select name="area"><option>SIEM</option><option>Cases</option><option>Reports</option><option>Threat Intel</option><option>Agent</option><option>Dashboard</option><option>Security Hardening</option><option>General</option></select></label>
              <label>Priority<select name="priority"><option>Medium</option><option>High</option><option>Critical</option><option>Low</option></select></label>
            </div>
            <label>Description<textarea name="description" rows="4" placeholder="Describe the improvement and why it matters"></textarea></label>
            <button type="submit">Add Improvement</button>
          </form>
          <div class="result-box" id="roadmapImprovementResult"><p class="muted-text">Use this to capture your post-phase improvement list before we build the next enhancement wave.</p></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Improvement Backlog</h2><span class="muted-text">New ideas</span></div>
          <div class="table-wrap"><table><thead><tr><th>Priority</th><th>Area</th><th>Title</th><th>Status</th></tr></thead><tbody id="roadmapImprovementRows"></tbody></table></div>
        </section>
      </div>
    </section>
  `);
  installRoadmapDashboardMetric();
}

function installRoadmapDashboardMetric() {
  const grid = document.querySelector("#view-dashboard .metric-grid");
  if (!grid || document.getElementById("metricDashboardPhases")) return;
  grid.insertAdjacentHTML("beforeend", `<article class="metric"><span>Completed Phases</span><strong id="metricDashboardPhases">0</strong></article>`);
}

function bindRoadmapDashboard() {
  document.getElementById("refreshRoadmapButton")?.addEventListener("click", refreshRoadmapDashboard);
  document.getElementById("roadmapImprovementForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      const payload = {
        title: form.get("title"),
        area: form.get("area"),
        priority: form.get("priority"),
        description: form.get("description") || ""
      };

      try {
        const item = await api("/api/roadmap/improvements", { method: "POST", body: payload });
        document.getElementById("roadmapImprovementResult").innerHTML = `<div class="result-title">${pill(item.priority)} <span>${escapeHtml(item.title)}</span></div>`;
        event.currentTarget.reset();
        await refreshRoadmapDashboard();
        showToast("Improvement captured");
      } catch (error) {
        document.getElementById("roadmapImprovementResult").innerHTML = renderError(error);
      }
    }, "Saving...");
  });
}

async function refreshRoadmapDashboard() {
  try {
    const [readiness, improvements] = await Promise.all([
      api("/api/roadmap/readiness"),
      api("/api/roadmap/improvements")
    ]);
    roadmapState.readiness = readiness;
    roadmapState.improvements = improvements;
  } catch (error) {
    showToast(`Roadmap status unavailable: ${error.message}`);
    roadmapState.readiness = null;
    roadmapState.improvements = [];
  }
  renderRoadmapDashboard();
}

function renderRoadmapDashboard() {
  const readiness = roadmapState.readiness || { totalPhases: 0, completedPhases: 0, needsHardeningPhases: 0, phases: [] };
  setRoadmapMetric("metricRoadmapTotal", readiness.totalPhases);
  setRoadmapMetric("metricRoadmapImplemented", readiness.completedPhases);
  setRoadmapMetric("metricRoadmapHardening", readiness.needsHardeningPhases);
  setRoadmapMetric("metricRoadmapImprovements", roadmapState.improvements.length);
  setRoadmapMetric("metricDashboardPhases", `${readiness.completedPhases}/${readiness.totalPhases}`);

  renderRows("roadmapPhaseRows", readiness.phases || [], phase => `
    <tr>
      <td><strong>${escapeHtml(phase.phase)}</strong><div class="muted-text">${escapeHtml(phase.title)}</div></td>
      <td>${pill(phase.status)}</td>
      <td>${escapeHtml(phase.dashboardSurface)}</td>
      <td>${escapeHtml(phase.summary)}<div class="muted-text">APIs: ${escapeHtml((phase.apiSurfaces || []).join(", "))}</div></td>
      <td>${escapeHtml((phase.nextHardeningItems || []).join(" | "))}</td>
    </tr>
  `);

  renderRows("roadmapImprovementRows", roadmapState.improvements, item => `
    <tr>
      <td>${pill(item.priority)}</td>
      <td>${escapeHtml(item.area)}</td>
      <td><strong>${escapeHtml(item.title)}</strong><div class="muted-text">${escapeHtml(item.description)}</div></td>
      <td>${escapeHtml(item.status)}</td>
    </tr>
  `);
}

function showRoadmapView() {
  document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.view === "roadmap"));
  document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === "view-roadmap"));
  refreshRoadmapDashboard();
}

function setRoadmapMetric(id, value) {
  const element = document.getElementById(id);
  if (element) element.textContent = value;
}
