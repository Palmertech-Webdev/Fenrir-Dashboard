const reportState = {
  reports: [],
  evidenceRecords: [],
  selectedReportId: null
};

document.addEventListener("DOMContentLoaded", () => {
  installReportsNavigation();
  installReportsView();
  bindReportsDashboard();
  refreshReportsDashboard();
});

function installReportsNavigation() {
  const nav = document.querySelector(".nav-list");
  if (!nav || document.querySelector('[data-view="reports"]')) return;
  const button = document.createElement("button");
  button.className = "nav-item";
  button.dataset.view = "reports";
  button.type = "button";
  button.textContent = "Reports / Integrity";
  button.addEventListener("click", showReportsView);
  nav.appendChild(button);
}

function installReportsView() {
  const main = document.querySelector(".main-content");
  if (!main || document.getElementById("view-reports")) return;

  main.insertAdjacentHTML("beforeend", `
    <section class="view" id="view-reports">
      <div class="siem-hero panel">
        <div>
          <p class="eyebrow">Phase 13</p>
          <h2>Investigation reporting and evidence integrity</h2>
          <p class="muted-text">Generate markdown investigation reports, seal evidence with SHA256 hashes and verify payload integrity later.</p>
        </div>
        <div class="siem-hero-actions">
          <button class="secondary" id="refreshReportsButton" type="button">Refresh</button>
        </div>
      </div>

      <div class="metric-grid siem-metrics">
        <article class="metric"><span>Reports</span><strong id="metricReports">0</strong></article>
        <article class="metric"><span>Completed Reports</span><strong id="metricCompletedReports">0</strong></article>
        <article class="metric"><span>Integrity Seals</span><strong id="metricIntegrityRecords">0</strong></article>
        <article class="metric"><span>Report Hashes</span><strong id="metricReportHashes">0</strong></article>
      </div>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Create Investigation Report</h2><span class="muted-text">Markdown export with SHA256 seal</span></div>
          <form id="reportCreateForm" class="tool-form">
            <label>Title<input name="title" type="text" placeholder="Suspicious login investigation summary" required></label>
            <div class="form-grid">
              <label>Report type<select name="reportType"><option>InvestigationSummary</option><option>CaseExport</option><option>ExecutiveSummary</option><option>TechnicalAppendix</option></select></label>
              <label>Case ID<input name="caseId" type="text" placeholder="Optional"></label>
            </div>
            <label>Scope<input name="scope" type="text" placeholder="Customer, incident scope, asset group or date range"></label>
            <label>Analyst summary<textarea name="analystSummary" rows="4" placeholder="Summarise what happened, current status and immediate risk."></textarea></label>
            <label>Conclusion<textarea name="conclusion" rows="3" placeholder="Conclude true positive, false positive, benign, or still under investigation."></textarea></label>
            <div class="checkbox-grid">
              <label><input name="includeFindings" type="checkbox" checked> Include findings</label>
              <label><input name="includeSiemSummary" type="checkbox" checked> Include SIEM summary</label>
              <label><input name="includeHuntRuns" type="checkbox" checked> Include hunt runs</label>
              <label><input name="includeResponseRuns" type="checkbox" checked> Include response runs</label>
            </div>
            <button type="submit">Generate Report</button>
          </form>
          <div class="result-box" id="reportCreateResult"><p class="muted-text">Generated reports are sealed automatically.</p></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Seal Evidence Payload</h2><span class="muted-text">Hash evidence without exposing secrets</span></div>
          <form id="evidenceSealForm" class="tool-form">
            <div class="form-grid">
              <label>Entity type<input name="entityType" type="text" placeholder="CaseEvidence" required></label>
              <label>Entity ID<input name="entityId" type="text" placeholder="Evidence/case/event id" required></label>
            </div>
            <label>Payload<textarea name="payload" rows="8" placeholder="Paste exact evidence text or JSON to seal" required></textarea></label>
            <label>Notes<input name="notes" type="text" placeholder="Optional chain-of-custody note"></label>
            <button type="submit">Seal Evidence</button>
          </form>
          <div class="result-box" id="evidenceSealResult"><p class="muted-text">The payload is not stored by this seal action; only its hash and metadata are stored.</p></div>
        </section>
      </div>

      <section class="panel">
        <div class="panel-heading"><h2>Generated Reports</h2><span class="muted-text">Click a report to preview markdown</span></div>
        <div class="table-wrap"><table><thead><tr><th>Status</th><th>Title</th><th>Type</th><th>SHA256</th><th>Created</th></tr></thead><tbody id="reportRows"></tbody></table></div>
      </section>

      <section class="panel">
        <div class="panel-heading"><h2>Report Preview</h2><span class="muted-text">Markdown content and integrity hash</span></div>
        <div id="reportDetail" class="result-box"><p class="muted-text">Select or generate a report.</p></div>
      </section>

      <div class="two-column">
        <section class="panel">
          <div class="panel-heading"><h2>Verify Evidence</h2><span class="muted-text">Compare payload against stored SHA256</span></div>
          <form id="evidenceVerifyForm" class="tool-form">
            <label>Integrity record<select name="integrityRecordId" id="evidenceRecordSelect"></select></label>
            <label>Payload<textarea name="payload" rows="8" placeholder="Paste the exact payload to verify" required></textarea></label>
            <button type="submit">Verify Payload</button>
          </form>
          <div class="result-box" id="evidenceVerifyResult"><p class="muted-text">Verification checks the submitted payload hash against the stored seal.</p></div>
        </section>

        <section class="panel">
          <div class="panel-heading"><h2>Evidence Integrity Records</h2><span class="muted-text">Chain-of-custody seals</span></div>
          <div class="table-wrap"><table><thead><tr><th>Entity</th><th>SHA256</th><th>Sealed By</th><th>Sealed</th></tr></thead><tbody id="evidenceRecordRows"></tbody></table></div>
        </section>
      </div>
    </section>
  `);
  installReportsDashboardMetric();
}

function installReportsDashboardMetric() {
  const grid = document.querySelector("#view-dashboard .metric-grid");
  if (!grid || document.getElementById("metricDashboardReports")) return;
  grid.insertAdjacentHTML("beforeend", `<article class="metric"><span>Reports</span><strong id="metricDashboardReports">0</strong></article>`);
}

function bindReportsDashboard() {
  document.getElementById("refreshReportsButton")?.addEventListener("click", refreshReportsDashboard);

  document.getElementById("reportCreateForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      const payload = {
        title: form.get("title"),
        reportType: form.get("reportType") || "InvestigationSummary",
        scope: emptyToNull(form.get("scope")),
        requestedBy: "analyst",
        caseId: emptyToNull(form.get("caseId")),
        includeFindings: form.get("includeFindings") === "on",
        includeSiemSummary: form.get("includeSiemSummary") === "on",
        includeHuntRuns: form.get("includeHuntRuns") === "on",
        includeResponseRuns: form.get("includeResponseRuns") === "on",
        analystSummary: emptyToNull(form.get("analystSummary")),
        conclusion: emptyToNull(form.get("conclusion"))
      };

      try {
        const report = await api("/api/reports", { method: "POST", body: payload });
        reportState.selectedReportId = report.id;
        document.getElementById("reportCreateResult").innerHTML = `<div class="result-title">${pill(report.status)} <span>${escapeHtml(report.title)}</span></div><p>SHA256: <code>${escapeHtml(report.sha256)}</code></p>`;
        event.currentTarget.reset();
        await refreshReportsDashboard();
        renderReportDetail(report);
        showToast("Report generated and sealed");
      } catch (error) {
        document.getElementById("reportCreateResult").innerHTML = renderError(error);
        showToast(`Report generation failed: ${error.message}`);
      }
    }, "Generating...");
  });

  document.getElementById("evidenceSealForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      const payload = {
        entityType: form.get("entityType"),
        entityId: form.get("entityId"),
        payload: form.get("payload"),
        notes: emptyToNull(form.get("notes")),
        sealedBy: "analyst"
      };
      try {
        const seal = await api("/api/reports/evidence-integrity", { method: "POST", body: payload });
        document.getElementById("evidenceSealResult").innerHTML = `<div class="result-title">${pill("Sealed")} <span>${escapeHtml(seal.entityType)}:${escapeHtml(seal.entityId)}</span></div><p>SHA256: <code>${escapeHtml(seal.sha256)}</code></p>`;
        event.currentTarget.reset();
        await refreshEvidenceRecords();
        showToast("Evidence sealed");
      } catch (error) {
        document.getElementById("evidenceSealResult").innerHTML = renderError(error);
        showToast(`Evidence seal failed: ${error.message}`);
      }
    }, "Sealing...");
  });

  document.getElementById("evidenceVerifyForm")?.addEventListener("submit", async event => {
    event.preventDefault();
    await withFormBusy(event.currentTarget, async () => {
      const form = new FormData(event.currentTarget);
      const payload = {
        integrityRecordId: form.get("integrityRecordId"),
        payload: form.get("payload")
      };
      try {
        const result = await api("/api/reports/evidence-integrity/verify", { method: "POST", body: payload });
        document.getElementById("evidenceVerifyResult").innerHTML = `
          <div class="result-title">${pill(result.isValid ? "Valid" : "Mismatch")} <span>${escapeHtml(result.summary)}</span></div>
          <p>Expected: <code>${escapeHtml(result.expectedSha256)}</code></p>
          <p>Actual: <code>${escapeHtml(result.actualSha256)}</code></p>`;
      } catch (error) {
        document.getElementById("evidenceVerifyResult").innerHTML = renderError(error);
      }
    }, "Verifying...");
  });

  document.getElementById("reportRows")?.addEventListener("click", event => {
    const row = event.target.closest("tr[data-report-id]");
    if (!row) return;
    const report = reportState.reports.find(item => item.id === row.dataset.reportId);
    if (report) renderReportDetail(report);
  });

  document.getElementById("reportRows")?.addEventListener("keydown", event => {
    if (event.key !== "Enter" && event.key !== " ") return;
    const row = event.target.closest("tr[data-report-id]");
    if (!row) return;
    event.preventDefault();
    const report = reportState.reports.find(item => item.id === row.dataset.reportId);
    if (report) renderReportDetail(report);
  });
}

async function refreshReportsDashboard() {
  await Promise.allSettled([refreshReports(), refreshEvidenceRecords()]);
  renderReportMetrics();
}

async function refreshReports() {
  try {
    reportState.reports = await api("/api/reports");
  } catch (error) {
    reportState.reports = [];
    showToast(`Reports unavailable: ${error.message}`);
  }
  renderReports();
  renderReportMetrics();
}

async function refreshEvidenceRecords() {
  try {
    reportState.evidenceRecords = await api("/api/reports/evidence-integrity");
  } catch (error) {
    reportState.evidenceRecords = [];
    showToast(`Evidence records unavailable: ${error.message}`);
  }
  renderEvidenceRecords();
  updateEvidenceRecordSelect();
  renderReportMetrics();
}

function renderReports() {
  renderRows("reportRows", reportState.reports, report => `
    <tr data-report-id="${escapeHtml(report.id)}" tabindex="0" role="button" aria-label="Open report ${escapeHtml(report.title)}">
      <td>${pill(report.status)}</td>
      <td><strong>${escapeHtml(report.title)}</strong><div class="muted-text">${escapeHtml(report.scope || "No scope")}</div></td>
      <td>${escapeHtml(report.reportType)}</td>
      <td><code>${escapeHtml(shortHash(report.sha256))}</code></td>
      <td>${formatDate(report.createdAtUtc)}</td>
    </tr>
  `);
}

function renderReportDetail(report) {
  reportState.selectedReportId = report.id;
  const target = document.getElementById("reportDetail");
  if (!target) return;
  target.innerHTML = `
    <div class="result-title">${pill(report.status)} <span>${escapeHtml(report.title)}</span></div>
    <p>SHA256: <code>${escapeHtml(report.sha256)}</code></p>
    <p><a href="/api/reports/${encodeURIComponent(report.id)}/markdown" target="_blank" rel="noopener">Download markdown</a></p>
    <pre class="json-output report-preview">${escapeHtml(report.contentMarkdown)}</pre>
  `;
}

function renderEvidenceRecords() {
  renderRows("evidenceRecordRows", reportState.evidenceRecords, record => `
    <tr>
      <td><strong>${escapeHtml(record.entityType)}</strong><div class="muted-text">${escapeHtml(record.entityId)}</div></td>
      <td><code>${escapeHtml(shortHash(record.sha256))}</code></td>
      <td>${escapeHtml(record.sealedBy)}</td>
      <td>${formatDate(record.sealedAtUtc)}</td>
    </tr>
  `);
}

function updateEvidenceRecordSelect() {
  const select = document.getElementById("evidenceRecordSelect");
  if (!select) return;
  select.innerHTML = reportState.evidenceRecords.map(record => `<option value="${escapeHtml(record.id)}">${escapeHtml(record.entityType)}:${escapeHtml(record.entityId)} - ${escapeHtml(shortHash(record.sha256))}</option>`).join("");
}

function renderReportMetrics() {
  const completed = reportState.reports.filter(report => report.status === "Completed").length;
  const reportHashes = reportState.reports.filter(report => report.sha256).length;
  setMetric("metricReports", reportState.reports.length);
  setMetric("metricCompletedReports", completed);
  setMetric("metricIntegrityRecords", reportState.evidenceRecords.length);
  setMetric("metricReportHashes", reportHashes);
  setMetric("metricDashboardReports", reportState.reports.length);
}

function showReportsView() {
  document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.view === "reports"));
  document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === "view-reports"));
  refreshReportsDashboard();
}

function shortHash(value) {
  const text = String(value || "");
  return text.length > 16 ? `${text.slice(0, 12)}...${text.slice(-6)}` : text;
}
