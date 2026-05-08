# Fenrir SOC Core

Fenrir SOC Core is a .NET 10 modular-monolith backend for the MVP security modules:

- Email verification
- Email header checking
- IOC checking
- DNS monitoring
- Dark-web provider checks
- Network scanning jobs
- SIEM collector, ingestion jobs and log search
- Shared findings and jobs

## Run Locally

Run the API with the .NET SDK:

```powershell
dotnet restore
dotnet test Fenrir.SecurityPlatform.slnx
dotnet run --project src/Fenrir.Api/Fenrir.Api.csproj
```

Development uses a local SQLite database file by default so the dashboard tools work immediately with no Docker or PostgreSQL setup. The file is created automatically as `src/Fenrir.Api/fenrir-dev.db`.

The default production/PostgreSQL connection string remains:

```text
Host=localhost;Port=5432;Database=fenrir_soc_core;Username=fenrir;Password=fenrir_dev_password
```

To use PostgreSQL locally, set `Database__Provider=Postgres` and override `ConnectionStrings__FenrirDb`, or edit the appsettings files.

## Run Perpetually With Docker

Copy `.env.example` to `.env`, change the passwords/signing key, then start the stack:

```powershell
copy .env.example .env
docker compose up -d --build
```

The compose stack runs:

- `fenrir-api` on `http://localhost:5248/`
- `fenrir-db` as PostgreSQL with a persistent Docker volume
- automatic EF migrations on API startup
- container health checks
- `restart: unless-stopped` so both services come back after reboot or Docker restart

Useful commands:

```powershell
docker compose ps
docker compose logs -f fenrir-api
docker compose restart fenrir-api
docker compose down
```

Application data such as local exposure summaries is bind-mounted from `./data` into the API container at `/app/data`.

Once running, open:

- Dashboard: `http://localhost:5248/`
- API health: `http://localhost:5248/health`
- Swagger UI: `http://localhost:5248/swagger`

## Exposure Checks

The dark-web/exposure module is provider based and safe by default:

- Local open CSV summaries are read from `data/darkweb/exposures.csv`.
- XposedOrNot's free public API is used for email checks and public breach/domain metadata.
- LeakCheck's public API is used for email and username breach checks.
- Username checks use the local CSV dataset and LeakCheck public API.
- Raw breached passwords and raw breach dumps are not stored.

The local CSV stores summary rows only:

```csv
query,queryType,sourceName,breachDate,exposureCount,description
demo@example.org,Email,Synthetic Training Exposure,2024-01-01,1,Safe sample row.
```

Tune providers in `src/Fenrir.Api/appsettings.json` under `DarkWeb`.

If a trusted breach list shows an exposure that public APIs miss, import a summary row through the dashboard or `POST /api/darkweb/import`. Do not import raw passwords or full dump contents.

## API Routes

- `POST /api/email/verify`
- `POST /api/email/header-check`
- `POST /api/iocs/check`
- `POST /api/iocs/import`
- `GET /api/iocs`
- `POST /api/dns/check-domain`
- `GET /api/dns/monitored-domains`
- `POST /api/dns/monitored-domains`
- `POST /api/darkweb/check`
- `POST /api/darkweb/import`
- `POST /api/network/scans`
- `GET /api/network/scans/{id}`
- `GET /api/network/scans/{id}/results`
- `POST /api/siem/events`
- `GET /api/siem/events`
- `POST /api/siem/events/search`
- `POST /api/siem/ingest/batch`
- `POST /api/siem/import/logs`
- `POST /api/siem/sources`
- `GET /api/siem/sources`
- `GET /api/siem/ingestion-jobs`
- `GET /api/siem/ingestion-jobs/{id}`
- `GET /api/findings`
- `GET /api/findings/{id}`
- `PATCH /api/findings/{id}/status`
- `GET /api/jobs`
- `GET /api/jobs/{id}`

## SIEM Collector Scope

The SIEM collector now supports:

- Source registration for manual uploads, API pulls, syslog-style feeds, agents and SIEM exports.
- Batch event ingestion with ingestion job status tracking.
- Raw log, JSON and NDJSON import through `POST /api/siem/import/logs`.
- Single-event ingestion compatibility for existing clients.
- Search by source, host, severity, event type, user, IP, indicator and time range.
- Automatic IOC extraction from message/raw payloads and finding creation where local IOCs match.
- High/critical event finding creation for analyst review.

The current implementation is intentionally collector-first and parser-ready. It preserves raw JSON payloads and creates normalised `SecurityEvent` records that can later be extended with dedicated parser packs for Windows, M365, Entra ID, DNS, firewall, VPN, Wazuh, Elastic and Splunk exports.

## Recommended Threat-Intel Connectors

Good next integrations for IOC enrichment:

- URLhaus by abuse.ch for malicious URL/domain/hash feeds.
- AbuseIPDB for IP reputation and abuse confidence.
- VirusTotal API v3 for hash, URL, domain and IP enrichment where your usage fits their terms.
- MISP as an open-source sharing/import hub for local and community threat feeds.

Keep these as provider interfaces behind the IOC module so Fenrir can continue to work with local storage when external APIs are unavailable or rate-limited.
