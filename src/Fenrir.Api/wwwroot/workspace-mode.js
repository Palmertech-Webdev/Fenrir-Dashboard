const workspaceState = {
  mode: null,
  presets: [],
  features: []
};

document.addEventListener("DOMContentLoaded", () => {
  installWorkspaceNavigation();
  installWorkspaceModeView();
  bindWorkspaceModeDashboard();
  refreshWorkspaceMode();
});

function installWorkspaceNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="workspace"]')) return;
  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "workspace";
  button.type = "button";
  button.textContent = "Workspace Mode";
  button.addEventListener("click", showWorkspaceView);
  nav.appendChild(button);
}

function installWorkspaceModeView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-workspace")) return;

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-workspace">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 14</p>
          <h2>Role-based analyst and home user modes</h2>
          <p class="muted-text">Switch Fenrir between a full SOC analyst workspace and a simplified home user validation workspace.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="refreshWorkspaceButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Current Mode</span><strong id="metricWorkspaceMode">-</strong></article>
        <article class="metric"><span>Advanced Features</span><strong id="metricWorkspaceAdvanced">-</strong></article>
        <article class="metric"><span>Response Actions</span><strong id="metricWorkspaceResponse">-</strong></article>
        <article class="metric"><span>Source Config</span><strong id="metricWorkspaceSourceConfig">-</strong></article>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Switch Workspace Mode</h2><span class="muted-text">UI and workflow guardrails</span></div>
          <form id="workspaceModeForm" class="tool-form">
            <label>Mode<select name="mode" id="workspaceModeSelect"><option value="Analyst">Analyst Mode</option><option value="HomeUser">Home User Mode</option></select></label>
            <label>Role<input name="role" type="text" placeholder="Analyst, Senior Analyst, Home User"></label>
            <div class="checkbox-grid">
              <label><input name="showAdvancedFeatures" type="checkbox"> Show advanced features</label>
              <label><input name="allowResponseActions" type="checkbox"> Allow response actions</label>
              <label><input name="allowEvidenceExports" type="checkbox"> Allow evidence exports</label>
              <label><input name="allowSourceConfiguration" type="checkbox"> Allow source configuration</label>
            </div>
            <button type="submit">Apply Workspace Mode</button>
          </form>
          <div class="result-box" id="workspaceModeResult"><p class="muted-text">Changing mode affects visible dashboard areas only. Backend permissions should still be enforced before production use.</p></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Current Mode</h2><span class="muted-text">Active workspace policy</span></div>
          <div id="workspaceCurrentMode" class="result-box"><p class="muted-text">Loading mode...</p></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Mode Presets</h2><span class="muted-text">Recommended feature exposure</span></div>
        <div class="table-wrap"><table><thead><tr><th>Mode</th><th>Description</th><th>Enabled Areas</th><th>Hidden Areas</th></tr></thead><tbody id="workspacePresetRows"></tbody></table></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Feature Access Matrix</h2><span class="muted-text">What each mode should expose</span></div>
        <div class="table-wrap"><table><thead><tr><th>Feature</th><th>Category</th><th>Analyst</th><th>Home User</th><th>Rationale</th></tr></thead><tbody id="workspaceFeatureRows"></tbody></table></div>
      </section>
    </section>
  `);
  installWorkspaceModeBadge();
}

function installWorkspaceModeBadge() {
  const actions = document.querySelector(".topbar-actions");
  if (!actions || document.getElementById("workspaceModeBadge")) return;
  actions.insertAdjacentHTML("afterbegin", `<span class="pill Informational" id="workspaceModeBadge">Mode: Loading</span>`);
}

function bindWorkspaceModeDashboard() {
  document.getElementById("refreshWorkspaceButton")?.addEventListener("click", refreshWorkspaceMode);
  document.getElementById("workspaceModeForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      mode: form.get("mode"),
      role: form.get("role") || (form.get("mode") === "HomeUser" ? "HomeUser" : "Analyst"),
      userKey: "local",
      showAdvancedFeatures: form.get("showAdvancedFeatures") === "on",
      allowResponseActions: form.get("allowResponseActions") === "on",
      allowEvidenceExports: form.get("allowEvidenceExports") === "on",
      allowSourceConfiguration: form.get("allowSourceConfiguration") === "on"
    };

    try {
      workspaceState.mode = await api("/api/workspace/mode", { method: "PUT", body: payload });
      document.getElementById("workspaceModeResult").innerHTML = `<div class="result-title">${pill(workspaceState.mode.mode)} <span>${escapeHtml(workspaceState.mode.description)}</span></div>`;
      renderWorkspaceMode();
      applyWorkspaceMode();
      showToast("Workspace mode updated");
    } catch (error) {
      document.getElementById("workspaceModeResult").innerHTML = renderError(error);
    }
  });

  document.getElementById("workspaceModeSelect")?.addEventListener("change", event => {
    const preset = workspaceState.presets.find(item => item.mode === event.target.value);
    if (preset) applyPresetToForm(preset);
  });
}

async function refreshWorkspaceMode() {
  try {
    const [mode, presets, features] = await Promise.all([
      api("/api/workspace/mode"),
      api("/api/workspace/mode/presets"),
      api("/api/workspace/features")
    ]);
    workspaceState.mode = mode;
    workspaceState.presets = presets;
    workspaceState.features = features;
    renderWorkspaceMode();
    applyWorkspaceMode();
  } catch (error) {
    showToast(`Workspace mode unavailable: ${error.message}`);
  }
}

function renderWorkspaceMode() {
  const mode = workspaceState.mode;
  if (!mode) return;
  setWorkspaceMetric("metricWorkspaceMode", mode.displayName);
  setWorkspaceMetric("metricWorkspaceAdvanced", mode.showAdvancedFeatures ? "Yes" : "No");
  setWorkspaceMetric("metricWorkspaceResponse", mode.allowResponseActions ? "Yes" : "No");
  setWorkspaceMetric("metricWorkspaceSourceConfig", mode.allowSourceConfiguration ? "Yes" : "No");

  const badge = document.getElementById("workspaceModeBadge");
  if (badge) badge.textContent = `Mode: ${mode.displayName}`;

  const current = document.getElementById("workspaceCurrentMode");
  if (current) {
    current.innerHTML = `
      <div class="result-title">${pill(mode.mode)} <span>${escapeHtml(mode.role)}</span></div>
      <p>${escapeHtml(mode.description)}</p>
      <ul>
        <li>Advanced features: ${mode.showAdvancedFeatures ? "enabled" : "hidden"}</li>
        <li>Response actions: ${mode.allowResponseActions ? "enabled" : "disabled"}</li>
        <li>Evidence exports: ${mode.allowEvidenceExports ? "enabled" : "disabled"}</li>
        <li>Source configuration: ${mode.allowSourceConfiguration ? "enabled" : "disabled"}</li>
      </ul>
    `;
  }

  const form = document.getElementById("workspaceModeForm");
  if (form) {
    form.elements.mode.value = mode.mode;
    form.elements.role.value = mode.role;
    form.elements.showAdvancedFeatures.checked = mode.showAdvancedFeatures;
    form.elements.allowResponseActions.checked = mode.allowResponseActions;
    form.elements.allowEvidenceExports.checked = mode.allowEvidenceExports;
    form.elements.allowSourceConfiguration.checked = mode.allowSourceConfiguration;
  }

  renderRows("workspacePresetRows", workspaceState.presets, preset => `
    <tr>
      <td>${pill(preset.mode)}<div class="muted-text">${escapeHtml(preset.role)}</div></td>
      <td><strong>${escapeHtml(preset.displayName)}</strong><div class="muted-text">${escapeHtml(preset.description)}</div></td>
      <td>${escapeHtml((preset.enabledAreas || []).join(", "))}</td>
      <td>${escapeHtml((preset.hiddenAreas || []).join(", "))}</td>
    </tr>
  `);

  renderRows("workspaceFeatureRows", workspaceState.features, feature => `
    <tr>
      <td><strong>${escapeHtml(feature.displayName)}</strong><div class="muted-text">${escapeHtml(feature.featureKey)}</div></td>
      <td>${escapeHtml(feature.category)}</td>
      <td>${feature.analystMode ? "Yes" : "No"}</td>
      <td>${feature.homeUserMode ? "Yes" : "No"}</td>
      <td>${escapeHtml(feature.rationale)}</td>
    </tr>
  `);
}

function applyWorkspaceMode() {
  const mode = workspaceState.mode;
  if (!mode) return;
  const isHome = mode.mode === "HomeUser";
  const hiddenInHome = ["network", "siem", "correlation", "response", "hunts", "reports", "jobs"];

  document.querySelectorAll(".nav-item").forEach(item => {
    const key = item.dataset.view;
    const shouldHide = isHome && hiddenInHome.includes(key);
    item.classList.toggle("mode-hidden", shouldHide);
    item.hidden = shouldHide;
  });

  if (isHome) {
    const active = document.querySelector(".nav-item.active");
    if (active && hiddenInHome.includes(active.dataset.view)) {
      if (typeof showView === "function") showView("dashboard");
    }
  }

  document.body.dataset.workspaceMode = mode.mode;
}

function applyPresetToForm(preset) {
  const form = document.getElementById("workspaceModeForm");
  if (!form) return;
  form.elements.role.value = preset.role;
  form.elements.showAdvancedFeatures.checked = preset.showAdvancedFeatures;
  form.elements.allowResponseActions.checked = preset.allowResponseActions;
  form.elements.allowEvidenceExports.checked = preset.allowEvidenceExports;
  form.elements.allowSourceConfiguration.checked = preset.allowSourceConfiguration;
}

function showWorkspaceView() {
  document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.view === "workspace"));
  document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === "view-workspace"));
  refreshWorkspaceMode();
}

function setWorkspaceMetric(id, value) {
  const element = document.getElementById(id);
  if (element) element.textContent = value;
}
