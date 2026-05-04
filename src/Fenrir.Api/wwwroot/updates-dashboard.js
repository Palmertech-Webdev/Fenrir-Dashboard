const updateState = {
  channels: [],
  packages: [],
  manifest: null
};

document.addEventListener("DOMContentLoaded", () => {
  installUpdatesNavigation();
  installUpdatesView();
  bindUpdatesDashboard();
  refreshUpdatesDashboard();
});

function installUpdatesNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="updates"]')) return;
  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "updates";
  button.type = "button";
  button.textContent = "Signed Updates";
  button.addEventListener("click", showUpdatesView);
  nav.appendChild(button);
}

function installUpdatesView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-updates")) return;

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-updates">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 15</p>
          <h2>Signed update and rule distribution</h2>
          <p class="muted-text">Manage update channels, signed rule bundles, manifests and package trust checks before distribution to agents or modules.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="refreshUpdatesButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Channels</span><strong id="metricUpdateChannels">0</strong></article>
        <article class="metric"><span>Packages</span><strong id="metricUpdatePackages">0</strong></article>
        <article class="metric"><span>Published</span><strong id="metricUpdatePublished">0</strong></article>
        <article class="metric"><span>Revoked</span><strong id="metricUpdateRevoked">0</strong></article>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Create Channel</h2><span class="muted-text">stable, preview, customer-specific</span></div>
          <form id="updateChannelForm" class="tool-form">
            <label>Name<input name="name" type="text" placeholder="stable" required></label>
            <label>Description<input name="description" type="text" placeholder="Stable signed updates and rule bundles"></label>
            <label><input name="isEnabled" type="checkbox" checked> Channel enabled</label>
            <button type="submit">Create Channel</button>
          </form>
          <div class="result-box" id="updateChannelResult"><p class="muted-text">Channels control which manifest agents should request.</p></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Verify Package Metadata</h2><span class="muted-text">Pre-publish safety check</span></div>
          <form id="updateVerifyForm" class="tool-form">
            <label>Download URL<input name="downloadUrl" type="url" placeholder="https://updates.example/fenrir-rules.zip" required></label>
            <label>SHA256<input name="sha256" type="text" placeholder="64 character SHA256" required></label>
            <div class="form-grid">
              <label>Signature algorithm<input name="signatureAlgorithm" type="text" value="SHA256-RSA"></label>
              <label>Public key ID<input name="publicKeyId" type="text" value="local-dev-key"></label>
            </div>
            <label>Signature<textarea name="signature" rows="3" placeholder="Detached signature or signature reference" required></textarea></label>
            <button type="submit">Verify Metadata</button>
          </form>
          <div class="result-box" id="updateVerifyResult"><p class="muted-text">This validates package metadata structure. Full cryptographic verification needs the public-key trust store in a later hardening pass.</p></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Create Update Package</h2><span class="muted-text">Rule bundle, parser pack, agent update or content pack</span></div>
        <form id="updatePackageForm" class="tool-form">
          <div class="form-grid">
            <label>Channel<select name="channelName" id="updatePackageChannel"></select></label>
            <label>Package type<select name="packageType"><option>Rules</option><option>ParserPack</option><option>Agent</option><option>ThreatIntel</option><option>ContentPack</option></select></label>
          </div>
          <div class="form-grid">
            <label>Name<input name="name" type="text" placeholder="Fenrir Rules Bundle" required></label>
            <label>Version<input name="version" type="text" placeholder="1.0.0" required></label>
          </div>
          <div class="form-grid">
            <label>Minimum app version<input name="minimumAppVersion" type="text" value="0.0.1"></label>
            <label>Target platform<input name="targetPlatform" type="text" value="any"></label>
          </div>
          <label>Download URL<input name="downloadUrl" type="url" placeholder="https://updates.example/fenrir-rules-1.0.0.zip" required></label>
          <div class="form-grid">
            <label>SHA256<input name="sha256" type="text" placeholder="64 character SHA256" required></label>
            <label>Size bytes<input name="sizeBytes" type="number" min="0" value="0"></label>
          </div>
          <div class="form-grid">
            <label>Signature algorithm<input name="signatureAlgorithm" type="text" value="SHA256-RSA"></label>
            <label>Public key ID<input name="publicKeyId" type="text" value="local-dev-key"></label>
          </div>
          <label>Signature<textarea name="signature" rows="3" placeholder="Detached package signature" required></textarea></label>
          <label>Release notes<textarea name="releaseNotes" rows="3" placeholder="What changed in this package?"></textarea></label>
          <label>Status<select name="status"><option>Draft</option><option>Published</option></select></label>
          <button type="submit">Create Package</button>
        </form>
        <div class="result-box" id="updatePackageResult"><p class="muted-text">Published packages appear in the channel manifest.</p></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Update Channels</h2><span class="muted-text">Distribution lanes</span></div>
        <div class="table-wrap"><table><thead><tr><th>Name</th><th>Description</th><th>Enabled</th><th>Updated</th></tr></thead><tbody id="updateChannelRows"></tbody></table></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Update Packages</h2><span class="muted-text">Publish or revoke packages</span></div>
        <div class="table-wrap"><table><thead><tr><th>Status</th><th>Package</th><th>Channel</th><th>Version</th><th>SHA256</th><th>Actions</th></tr></thead><tbody id="updatePackageRows"></tbody></table></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Manifest Preview</h2><span class="muted-text">What agents/modules receive</span></div>
        <form id="updateManifestForm" class="tool-form inline-form"><select name="channelName" id="updateManifestChannel"></select><button type="submit">Load Manifest</button></form>
        <div class="result-box" id="updateManifestResult"><p class="muted-text">Select a channel to preview the published update manifest.</p></div>
      </section>
    </section>
  `);
  installUpdatesDashboardMetric();
}

function installUpdatesDashboardMetric() {
  const grid = document.querySelector("#view-dashboard .metric-grid");
  if (!grid || document.getElementById("metricDashboardUpdates")) return;
  grid.insertAdjacentHTML("beforeend", `<article class="metric"><span>Published Updates</span><strong id="metricDashboardUpdates">0</strong></article>`);
}

function bindUpdatesDashboard() {
  document.getElementById("refreshUpdatesButton")?.addEventListener("click", refreshUpdatesDashboard);

  document.getElementById("updateChannelForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      try {
        const channel = await api("/api/updates/channels", { method: "POST", body: { name: form.get("name"), description: form.get("description") || "", isEnabled: form.get("isEnabled") === "on" } });
        document.getElementById("updateChannelResult").innerHTML = `<div class="result-title">${pill(channel.isEnabled ? "Enabled" : "Disabled")} <span>${escapeHtml(channel.name)}</span></div>`;
        event.currentTarget.reset();
        await refreshUpdatesDashboard();
        showToast("Update channel saved");
      } catch (error) {
        document.getElementById("updateChannelResult").innerHTML = renderError(error);
      }
    }, "Saving...");
  });

  document.getElementById("updateVerifyForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const payload = updatePackageMetadataFromForm(new FormData(event.currentTarget));
      try {
        const result = await api("/api/updates/verify", { method: "POST", body: payload });
        document.getElementById("updateVerifyResult").innerHTML = renderVerificationResult(result);
      } catch (error) {
        document.getElementById("updateVerifyResult").innerHTML = renderError(error);
      }
    }, "Verifying...");
  });

  document.getElementById("updatePackageForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      const payload = {
        channelName: form.get("channelName"),
        packageType: form.get("packageType"),
        name: form.get("name"),
        version: form.get("version"),
        minimumAppVersion: form.get("minimumAppVersion") || "0.0.1",
        targetPlatform: form.get("targetPlatform") || "any",
        downloadUrl: form.get("downloadUrl"),
        sha256: form.get("sha256"),
        sizeBytes: Number(form.get("sizeBytes") || 0),
        signatureAlgorithm: form.get("signatureAlgorithm") || "SHA256-RSA",
        signature: form.get("signature"),
        publicKeyId: form.get("publicKeyId") || "local-dev-key",
        releaseNotes: form.get("releaseNotes") || "",
        status: form.get("status") || "Draft"
      };
      try {
        const packageDto = await api("/api/updates/packages", { method: "POST", body: payload });
        document.getElementById("updatePackageResult").innerHTML = `<div class="result-title">${pill(packageDto.status)} <span>${escapeHtml(packageDto.name)} ${escapeHtml(packageDto.version)}</span></div><p>SHA256: <code>${escapeHtml(packageDto.sha256)}</code></p>`;
        event.currentTarget.reset();
        await refreshUpdatesDashboard();
        showToast("Update package saved");
      } catch (error) {
        document.getElementById("updatePackageResult").innerHTML = renderError(error);
      }
    }, "Publishing...");
  });

  document.getElementById("updatePackageRows")?.addEventListener("click", async event => {
    const button = event.target.closest("button[data-package-action]");
    if (!button) return;
    const id = button.dataset.packageId;
    const action = button.dataset.packageAction;
    try {
      await api(`/api/updates/packages/${encodeURIComponent(id)}/${action}`, { method: "POST", body: {} });
      await refreshUpdatesDashboard();
      showToast(`Package ${action}ed`);
    } catch (error) {
      showToast(`Package action failed: ${error.message}`);
    }
  });

  document.getElementById("updateManifestForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      await loadManifest(form.get("channelName"));
    }, "Loading...");
  });
}

async function refreshUpdatesDashboard() {
  try {
    updateState.channels = await api("/api/updates/channels");
    updateState.packages = await api("/api/updates/packages");
  } catch (error) {
    showToast(`Updates unavailable: ${error.message}`);
    updateState.channels = [];
    updateState.packages = [];
  }
  renderUpdatesDashboard();
}

function renderUpdatesDashboard() {
  renderRows("updateChannelRows", updateState.channels, channel => `
    <tr>
      <td><strong>${escapeHtml(channel.name)}</strong></td>
      <td>${escapeHtml(channel.description)}</td>
      <td>${channel.isEnabled ? "Yes" : "No"}</td>
      <td>${formatDate(channel.updatedAtUtc)}</td>
    </tr>
  `);

  renderRows("updatePackageRows", updateState.packages, pkg => `
    <tr>
      <td>${pill(pkg.status)}</td>
      <td><strong>${escapeHtml(pkg.name)}</strong><div class="muted-text">${escapeHtml(pkg.packageType)} / ${escapeHtml(pkg.targetPlatform)}</div></td>
      <td>${escapeHtml(pkg.channelName)}</td>
      <td>${escapeHtml(pkg.version)}</td>
      <td><code>${escapeHtml(shortUpdateHash(pkg.sha256))}</code></td>
      <td>
        <button class="ghost" type="button" data-package-action="publish" data-package-id="${escapeHtml(pkg.id)}">Publish</button>
        <button class="ghost" type="button" data-package-action="revoke" data-package-id="${escapeHtml(pkg.id)}">Revoke</button>
      </td>
    </tr>
  `);

  updateChannelSelects();
  const published = updateState.packages.filter(pkg => pkg.status === "Published").length;
  const revoked = updateState.packages.filter(pkg => pkg.status === "Revoked").length;
  setUpdateMetric("metricUpdateChannels", updateState.channels.length);
  setUpdateMetric("metricUpdatePackages", updateState.packages.length);
  setUpdateMetric("metricUpdatePublished", published);
  setUpdateMetric("metricUpdateRevoked", revoked);
  setUpdateMetric("metricDashboardUpdates", published);
}

function updateChannelSelects() {
  const options = updateState.channels.map(channel => `<option value="${escapeHtml(channel.name)}">${escapeHtml(channel.name)}</option>`).join("");
  ["updatePackageChannel", "updateManifestChannel"].forEach(id => {
    const select = document.getElementById(id);
    if (select) select.innerHTML = options;
  });
}

async function loadManifest(channelName) {
  const target = document.getElementById("updateManifestResult");
  target.innerHTML = `<div class="result-title">Loading manifest...</div>`;
  try {
    updateState.manifest = await api(`/api/updates/manifest/${encodeURIComponent(channelName || "stable")}`);
    target.innerHTML = `
      <div class="result-title">${pill(updateState.manifest.channelName)} <span>${updateState.manifest.packages.length} published package(s)</span></div>
      ${jsonBlock(updateState.manifest)}
    `;
  } catch (error) {
    target.innerHTML = renderError(error);
  }
}

function updatePackageMetadataFromForm(form) {
  return {
    downloadUrl: form.get("downloadUrl"),
    sha256: form.get("sha256"),
    signatureAlgorithm: form.get("signatureAlgorithm") || "SHA256-RSA",
    signature: form.get("signature"),
    publicKeyId: form.get("publicKeyId") || "local-dev-key"
  };
}

function renderVerificationResult(result) {
  const checks = (result.checks || []).map(item => `<li>${escapeHtml(item)}</li>`).join("");
  const warnings = (result.warnings || []).map(item => `<li>${escapeHtml(item)}</li>`).join("");
  return `
    <div class="result-title">${pill(result.isValid ? "Valid" : "Invalid")} <span>${escapeHtml(result.verdict)}</span></div>
    <h4>Checks</h4><ul>${checks || "<li>No checks passed.</li>"}</ul>
    <h4>Warnings</h4><ul>${warnings || "<li>No warnings.</li>"}</ul>
  `;
}

function showUpdatesView() {
  document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.view === "updates"));
  document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === "view-updates"));
  refreshUpdatesDashboard();
}

function setUpdateMetric(id, value) {
  const element = document.getElementById(id);
  if (element) element.textContent = value;
}

function shortUpdateHash(value) {
  const text = String(value || "");
  return text.length > 16 ? `${text.slice(0, 12)}...${text.slice(-6)}` : text;
}
