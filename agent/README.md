# Fenrir Agent

Fenrir Agent collects lightweight endpoint telemetry and sends it to Fenrir SOC Core SIEM ingestion.

## Run

Start the backend first, then run:

```powershell
dotnet run --project agent/Fenrir.Agent.csproj -- --once
```

For a continuous local agent loop:

```powershell
dotnet run --project agent/Fenrir.Agent.csproj -- --api http://localhost:5248 --interval 30
```

Useful options:

- `--api <url>`: Fenrir API base URL.
- `--source <name>`: SIEM source name. Defaults to `FenrirAgent-{machine}`.
- `--token <jwt>`: optional bearer token for future authenticated deployments.
- `--interval <seconds>`: collection interval for continuous mode.
- `--batch-size <count>`: maximum events sent per batch.
- `--once`: collect once, send once, then exit.
- `--no-process`: disable process telemetry.
- `--no-network`: disable network telemetry.
- `--no-register`: skip SIEM source registration.

Environment variables are also supported:

- `FENRIR_API_URL`
- `FENRIR_AGENT_NAME`
- `FENRIR_AGENT_TOKEN`
- `FENRIR_AGENT_INTERVAL_SECONDS`
- `FENRIR_AGENT_ONCE`
