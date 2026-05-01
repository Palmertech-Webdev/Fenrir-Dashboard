const state = {
  findings: [],
  jobs: [],
  iocs: [],
  events: []
};

const views = document.querySelectorAll(".view");
const navItems = document.querySelectorAll(".nav-item");
const toast = document.getElementById("toast");

document.addEventListener("DOMContentLoaded", () => {
  bindNavigation();
  bindForms();
  refreshAll();
});

function bindNavigation() {
  navItems.forEach((item) => {
    item.addEventListener("click", () => showView(item.dataset.view));
  });

  document.querySelectorAll("[data-view-jump]").forEach((button) => {
    button.addEventListener("click", () => showView(button.dataset.viewJump));
  });
}

function showView(name) {
  navItems.forEach((item) => item.classList.toggle("active", item.dataset.view === name));
  views.forEach((view) => view.classList.toggle("active", view.id === `view-${name}`));
}

function bindForms() {
  document.getElementById("refreshAllButton").addEventListener("click", refreshAll);
  document.getElementById("refreshFindingsButton").addEventListener("click", refreshFindings);
  document.getElementById("refreshJobsButton").addEventListener("click", refreshJobs);
  document.getElementById("refreshIocsButton").addEventListener("click", refreshIocs);
  document.getElementById("refreshEventsButton").addEventListener("click", refreshEvents);

  document.getElementById("emailVerifyForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      email: form.get("email"),
      dkimSelector: form.get("dkimSelector") || null
    };

    await runTool("/api/email/verify", payload, "emailVerifyResult", (data) => renderEmailResult(data));
  });

  document.getElementById("headerCheckForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await runTool("/api/email/header-check", { rawHeaders: form.get("rawHeaders") }, "headerCheckResult", renderHeaderResult);
  });

  document.getElementById("iocCheckForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const indicators = splitLines(form.get("indicators"));
    const payload = indicators.length === 1 ? { indicator: indicators[0] } : { indicators };
    await runTool("/api/iocs/check", payload, "iocCheckResult", renderIocCheckResult);
  });

  document.getElementById("iocImportForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const tags = String(form.get("tags") || "")
      .split(",")
      .map((tag) => tag.trim())
      .filter(Boolean);
    const payload = {
      records: [
        {
          indicator: form.get("indicator"),
          verdict: form.get("verdict"),
          severity: form.get("severity"),
          confidence: Number(form.get("confidence") || 0),
          source: form.get("source") || "Manual import",
          tags
        }
      ]
    };

    await api("/api/iocs/import", { method: "POST", body: payload });
    showToast("IOC imported");
    event.currentTarget.reset();
    await refreshIocs();
  });

  document.getElementById("dnsCheckForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await runTool("/api/dns/check-domain", { domain: form.get("domain") }, "dnsCheckResult", renderDnsResult);
  });

  document.getElementById("monitoredDomainForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await api("/api/dns/monitored-domains", {
      method: "POST",
      body: {
        domain: form.get("domain"),
        owner: form.get("owner") || null
      }
    });
    showToast("Monitored domain added");
    event.currentTarget.reset();
    await refreshMonitoredDomains();
  });

  document.getElementById("darkWebForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await runTool(
      "/api/darkweb/check",
      { query: form.get("query"), queryType: form.get("queryType") },
      "darkWebResult",
      renderDarkWebResult
    );
  });

  document.getElementById("networkScanForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const ports = String(form.get("ports") || "")
      .split(",")
      .map((port) => Number(port.trim()))
      .filter((port) => Number.isInteger(port));
    await runTool(
      "/api/network/scans",
      {
        target: form.get("target"),
        scanType: form.get("scanType"),
        ports: ports.length ? ports : null
      },
      "networkScanResult",
      renderNetworkCreateResult
    );
    await refreshJobs();
  });

  document.getElementById("networkLookupForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await runReadTool(`/api/network/scans/${encodeURIComponent(form.get("scanId"))}`, "networkLookupResult", renderNetworkScan);
  });

  document.getElementById("siemEventForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const rawText = String(form.get("raw") || "").trim();
    let raw = {};
    try {
      if (rawText.length > 0) {
        raw = JSON.parse(rawText);
      }
    } catch (error) {
      document.getElementById("siemIngestResult").innerHTML = renderError(new Error("Raw JSON is not valid."));
      return;
    }

    await runTool(
      "/api/siem/events",
      {
        source: form.get("source"),
        host: form.get("host"),
        eventType: form.get("eventType"),
        severity: form.get("severity"),
        message: form.get("message"),
        raw
      },
      "siemIngestResult",
      renderSiemIngestResult
    );
    await refreshEvents();
  });
}

async function refreshAll() {
  await refreshHealth();
  await Promise.allSettled([
    refreshFindings(),
    refreshJobs(),
    refreshIocs(),
    refreshEvents(),
    refreshMonitoredDomains()
  ]);
  renderDashboard();
}

async function refreshHealth() {
  const statusText = document.getElementById("statusText");
  try {
    const response = await fetch("/health");
    statusText.textContent = response.ok ? "API online" : "API health check returned a warning";
  } catch {
    statusText.textContent = "API offline";
  }
}

async function refreshFindings() {
  try {
    state.findings = await api("/api/findings");
  } catch (error) {
    state.findings = [];
    showToast(`Findings unavailable: ${error.message}`);
  }

  renderFindings();
  renderDashboard();
}

async function refreshJobs() {
  try {
    state.jobs = await api("/api/jobs");
  } catch (error) {
    state.jobs = [];
    showToast(`Jobs unavailable: ${error.message}`);
  }

  renderJobs();
  renderDashboard();
}

async function refreshIocs() {
  try {
    state.iocs = await api("/api/iocs");
  } catch (error) {
    state.iocs = [];
    showToast(`IOCs unavailable: ${error.message}`);
  }

  renderIocs();
  renderDashboard();
}

async function refreshEvents() {
  try {
    state.events = await api("/api/siem/events");
  } catch (error) {
    state.events = [];
    showToast(`Events unavailable: ${error.message}`);
  }

  renderEvents();
  renderDashboard();
}

async function refreshMonitoredDomains() {
  try {
    const domains = await api("/api/dns/monitored-domains");
    renderRows("monitoredDomainRows", domains, (domain) => `
      <tr>
        <td>${escapeHtml(domain.domain)}</td>
        <td>${escapeHtml(domain.owner || "")}</td>
        <td>${domain.isActive ? "Yes" : "No"}</td>
      </tr>
    `);
  } catch (error) {
    renderRows("monitoredDomainRows", [], null);
    showToast(`Monitored domains unavailable: ${error.message}`);
  }
}

async function runTool(path, payload, targetId, renderer) {
  const target = document.getElementById(targetId);
  target.innerHTML = `<div class="result-title">Running...</div>`;
  try {
    const data = await api(path, { method: "POST", body: payload });
    target.innerHTML = renderer(data);
    await Promise.allSettled([refreshFindings(), refreshJobs()]);
  } catch (error) {
    target.innerHTML = renderError(error);
  }
}

async function runReadTool(path, targetId, renderer) {
  const target = document.getElementById(targetId);
  target.innerHTML = `<div class="result-title">Loading...</div>`;
  try {
    const data = await api(path);
    target.innerHTML = renderer(data);
  } catch (error) {
    target.innerHTML = renderError(error);
  }
}

async function api(path, options = {}) {
  const request = {
    method: options.method || "GET",
    headers: {
      Accept: "application/json"
    }
  };

  if (options.body !== undefined) {
    request.headers["Content-Type"] = "application/json";
    request.body = JSON.stringify(options.body);
  }

  const response = await fetch(path, request);
  const contentType = response.headers.get("content-type") || "";
  const payload = contentType.includes("application/json")
    ? await response.json()
    : await response.text();

  if (!response.ok) {
    const message = typeof payload === "string"
      ? payload
      : payload.error || payload.title || JSON.stringify(payload);
    throw new Error(message || `HTTP ${response.status}`);
  }

  return payload;
}

function renderDashboard() {
  const openFindings = state.findings.filter((finding) => finding.status !== "Resolved" && finding.status !== "Dismissed");
  const highFindings = state.findings.filter((finding) => finding.severity === "High" || finding.severity === "Critical");
  document.getElementById("metricOpenFindings").textContent = openFindings.length;
  document.getElementById("metricHighFindings").textContent = highFindings.length;
  document.getElementById("metricIocs").textContent = state.iocs.length;
  document.getElementById("metricEvents").textContent = state.events.length;

  renderRows("dashboardFindingsRows", state.findings.slice(0, 8), (finding) => `
    <tr>
      <td>${pill(finding.severity)}</td>
      <td>${escapeHtml(finding.module)}</td>
      <td>${escapeHtml(finding.title)}</td>
      <td>${pill(finding.status)}</td>
    </tr>
  `);

  renderRows("dashboardJobsRows", state.jobs.slice(0, 8), (job) => `
    <tr>
      <td>${escapeHtml(job.jobType)}</td>
      <td>${pill(job.status)}</td>
      <td>${formatDate(job.createdAtUtc)}</td>
    </tr>
  `);
}

function renderFindings() {
  renderRows("findingRows", state.findings, (finding) => `
    <tr>
      <td>${pill(finding.severity)}</td>
      <td>${escapeHtml(finding.module)}</td>
      <td>
        <strong>${escapeHtml(finding.title)}</strong>
        <div class="muted-text">${escapeHtml(finding.summary || "")}</div>
      </td>
      <td>${finding.riskScore}</td>
      <td>${pill(finding.status)}</td>
      <td>${formatDate(finding.createdAtUtc)}</td>
    </tr>
  `);
}

function renderJobs() {
  renderRows("jobRows", state.jobs, (job) => `
    <tr>
      <td>${escapeHtml(job.jobType)}</td>
      <td>${pill(job.status)}</td>
      <td>${escapeHtml(job.relatedEntityType || "")}<br>${escapeHtml(job.relatedEntityId || "")}</td>
      <td>${formatDate(job.createdAtUtc)}</td>
      <td>${escapeHtml(job.error || "")}</td>
    </tr>
  `);
}

function renderIocs() {
  renderRows("iocRows", state.iocs, (ioc) => `
    <tr>
      <td>${escapeHtml(ioc.indicator)}</td>
      <td>${escapeHtml(ioc.type)}</td>
      <td>${pill(ioc.verdict)}</td>
      <td>${ioc.confidence}</td>
      <td>${escapeHtml(ioc.source || "")}</td>
    </tr>
  `);
}

function renderEvents() {
  renderRows("eventRows", state.events, (event) => `
    <tr>
      <td>${pill(event.severity)}</td>
      <td>${escapeHtml(event.host)}</td>
      <td>${escapeHtml(event.eventType)}</td>
      <td>${escapeHtml(event.message)}</td>
    </tr>
  `);
}

function renderEmailResult(data) {
  return `
    <div class="result-title">
      ${pill(data.risk)}
      <span>${escapeHtml(data.email)}</span>
      <span>Trust score ${data.trustScore}</span>
    </div>
    <p>${escapeHtml(data.summary)}</p>
    ${renderFindingList(data.findings)}
    ${jsonBlock(data)}
  `;
}

function renderHeaderResult(data) {
  return `
    <div class="result-title">
      ${pill(data.risk)}
      <span>${escapeHtml(data.from || "Unknown sender")}</span>
    </div>
    <p>${escapeHtml(data.summary)}</p>
    <p>SPF: ${escapeHtml(data.spfResult || "not found")} | DKIM: ${escapeHtml(data.dkimResult || "not found")} | DMARC: ${escapeHtml(data.dmarcResult || "not found")}</p>
    ${renderFindingList(data.findings)}
    ${jsonBlock(data)}
  `;
}

function renderIocCheckResult(data) {
  const rows = (data.results || []).map((result) => `
    <tr>
      <td>${escapeHtml(result.indicator)}</td>
      <td>${escapeHtml(result.type)}</td>
      <td>${result.matched ? "Yes" : "No"}</td>
      <td>${pill(result.verdict)}</td>
      <td>${result.finding ? escapeHtml(result.finding.title) : ""}</td>
    </tr>
  `).join("");

  return `
    <div class="result-title">IOC Results</div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Indicator</th>
            <th>Type</th>
            <th>Matched</th>
            <th>Verdict</th>
            <th>Finding</th>
          </tr>
        </thead>
        <tbody>${rows || emptyRow(5)}</tbody>
      </table>
    </div>
    ${jsonBlock(data)}
  `;
}

function renderDnsResult(data) {
  return `
    <div class="result-title">
      ${pill(data.risk)}
      <span>${escapeHtml(data.domain)}</span>
    </div>
    <p>${escapeHtml(data.summary)}</p>
    <p>MX: ${data.mxRecords.length} | TXT: ${data.txtRecords.length} | SPF: ${yesNo(data.spfPresent)} | DMARC: ${yesNo(data.dmarcPresent)} | CAA: ${data.caaRecords.length}</p>
    ${renderFindingList(data.findings)}
    ${jsonBlock(data)}
  `;
}

function renderDarkWebResult(data) {
  return `
    <div class="result-title">
      ${pill(data.exposed ? "High" : "Informational")}
      <span>${escapeHtml(data.query)}</span>
    </div>
    <p>Exposed: ${yesNo(data.exposed)} | Breach count: ${data.breachCount}</p>
    ${renderFindingList(data.findings)}
    ${jsonBlock(data)}
  `;
}

function renderNetworkCreateResult(data) {
  return `
    <div class="result-title">
      ${pill(data.status)}
      <span>Scan queued</span>
    </div>
    <p>Scan ID: <strong>${escapeHtml(data.scanId)}</strong></p>
    <p>Job ID: <strong>${escapeHtml(data.jobId)}</strong></p>
    ${jsonBlock(data)}
  `;
}

function renderNetworkScan(data) {
  const results = data.results || [];
  const rows = results.map((result) => `
    <tr>
      <td>${escapeHtml(result.asset)}</td>
      <td>${result.port}</td>
      <td>${result.isOpen ? "Open" : "Closed"}</td>
      <td>${escapeHtml(result.service || "")}</td>
      <td>${pill(result.severity)}</td>
    </tr>
  `).join("");

  return `
    <div class="result-title">
      ${pill(data.status)}
      <span>${escapeHtml(data.target)}</span>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Asset</th>
            <th>Port</th>
            <th>State</th>
            <th>Service</th>
            <th>Severity</th>
          </tr>
        </thead>
        <tbody>${rows || emptyRow(5)}</tbody>
      </table>
    </div>
    ${jsonBlock(data)}
  `;
}

function renderSiemIngestResult(data) {
  return `
    <div class="result-title">
      ${pill(data.event.severity)}
      <span>${escapeHtml(data.event.eventType)}</span>
    </div>
    <p>${escapeHtml(data.event.message)}</p>
    ${renderFindingList(data.findings)}
    ${jsonBlock(data)}
  `;
}

function renderFindingList(findings = []) {
  if (!findings.length) {
    return `<p>No findings created.</p>`;
  }

  return `
    <div class="finding-list">
      ${findings.map((finding) => `
        <div class="result-title">
          ${pill(finding.severity)}
          <span>${escapeHtml(finding.title)}</span>
        </div>
        <p>${escapeHtml(finding.recommendation || "")}</p>
      `).join("")}
    </div>
  `;
}

function renderRows(targetId, items, renderer) {
  const target = document.getElementById(targetId);
  if (!items || items.length === 0 || !renderer) {
    target.innerHTML = emptyRow(target.closest("table")?.querySelectorAll("th").length || 1);
    return;
  }

  target.innerHTML = items.map(renderer).join("");
}

function emptyRow(columns) {
  return `<tr><td class="empty-row" colspan="${columns}">No records yet</td></tr>`;
}

function renderError(error) {
  return `
    <div class="result-title">${pill("High")} Request failed</div>
    <p>${escapeHtml(error.message)}</p>
  `;
}

function pill(value) {
  const label = escapeHtml(value || "Unknown");
  const className = String(value || "Unknown").replace(/[^a-zA-Z0-9_-]/g, "");
  return `<span class="pill ${className}">${label}</span>`;
}

function jsonBlock(data) {
  return `<pre class="json-output">${escapeHtml(JSON.stringify(data, null, 2))}</pre>`;
}

function splitLines(value) {
  return String(value || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}

function yesNo(value) {
  return value ? "Yes" : "No";
}

function formatDate(value) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "" : date.toLocaleString();
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

let toastTimer;
function showToast(message) {
  toast.textContent = message;
  toast.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove("show"), 4500);
}
