document.addEventListener("DOMContentLoaded", () => {
  bindDnsFormNormalisation();
});

function bindDnsFormNormalisation() {
  const dnsCheckForm = document.getElementById("dnsCheckForm");
  if (dnsCheckForm) {
    dnsCheckForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      event.stopImmediatePropagation();

      await withFormBusy(event.currentTarget, async () => {
        const form = new FormData(event.currentTarget);
        const domain = normaliseDomainInput(form.get("domain"));
        await runTool("/api/dns/check-domain", { domain }, "dnsCheckResult", renderDnsResult);
      }, "Checking...");
    }, true);
  }

  const monitoredDomainForm = document.getElementById("monitoredDomainForm");
  if (monitoredDomainForm) {
    monitoredDomainForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      event.stopImmediatePropagation();

      await withFormBusy(event.currentTarget, async () => {
        const form = new FormData(event.currentTarget);
        await api("/api/dns/monitored-domains", {
          method: "POST",
          body: {
            domain: normaliseDomainInput(form.get("domain")),
            owner: String(form.get("owner") || "").trim() || null
          }
        });
        showToast("Monitored domain added");
        event.currentTarget.reset();
        await refreshMonitoredDomains();
      }, "Saving...");
    }, true);
  }
}

function normaliseDomainInput(value) {
  let text = String(value || "").trim();

  if (!text) {
    return "";
  }

  if (!/^https?:\/\//i.test(text) && text.includes("/") && text.includes(".")) {
    text = `https://${text}`;
  }

  try {
    if (/^https?:\/\//i.test(text)) {
      return new URL(text).hostname.trim().replace(/\.$/, "").toLowerCase();
    }
  } catch {
    return String(value || "").trim().replace(/^https?:\/\//i, "").split("/")[0].replace(/\.$/, "").toLowerCase();
  }

  return text
    .replace(/^https?:\/\//i, "")
    .split("/")[0]
    .replace(/\.$/, "")
    .toLowerCase();
}
