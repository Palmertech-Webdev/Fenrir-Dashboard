# Fenrir SOC Core

Fenrir SOC Core is a .NET 10 modular-monolith backend for the MVP security modules:

- Email verification
- Email header checking
- IOC checking
- DNS monitoring
- Dark-web provider checks
- Network scanning jobs
- SIEM data collection
- Shared findings and jobs

## Run Locally

Use a local PostgreSQL instance, then run the API with the .NET SDK:

```powershell
dotnet restore
dotnet test Fenrir.SecurityPlatform.slnx
dotnet run --project src/Fenrir.Api/Fenrir.Api.csproj
```

The default development connection string is:

```text
Host=localhost;Port=5432;Database=fenrir_soc_core;Username=fenrir;Password=fenrir_dev_password
```

Override it with `ConnectionStrings__FenrirDb` or edit `src/Fenrir.Api/appsettings.json`.

Once running, open:

- Dashboard: `http://localhost:5248/`
- API health: `http://localhost:5248/health`
- Swagger UI: `http://localhost:5248/swagger`

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
- `POST /api/network/scans`
- `GET /api/network/scans/{id}`
- `GET /api/network/scans/{id}/results`
- `POST /api/siem/events`
- `GET /api/siem/events`
- `GET /api/findings`
- `GET /api/findings/{id}`
- `PATCH /api/findings/{id}/status`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
