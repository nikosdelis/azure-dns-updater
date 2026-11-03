# Azure DNS Updater

Keeps an A-record in [Azure DNS](https://azure.microsoft.com/en-gb/products/dns) pointed at your current **dynamic public IP address**. Runs as a Windows Service, a Docker container, or a plain console app.

Inspired by [Jussi Roine's PowerShell tutorial](https://jussiroine.com/2019/06/building-a-simple-and-secure-dns-updater-for-azure-dns-using-raspberry-pi-and-azure-functions/), rewritten in C#.

## How it works

Every `intervalInMinutes` (default 5) the updater:

1. Fetches the public IP from `https://checkip.amazonaws.com/` (configurable).
2. Reads the A-record `recordsetName` in zone `zoneName`.
3. If the record is missing or points elsewhere, creates/updates it with the new IP.

Errors (network down, Azure throttling, bad credentials) are logged and retried on the next tick. The process never exits on its own.

## Requirements

- An Azure subscription with a DNS zone.
- A [Service Principal](https://learn.microsoft.com/en-us/entra/identity-platform/app-objects-and-service-principals?tabs=browser) with the **DNS Zone Contributor** role on that zone (or its resource group).
- To build from source: the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Not needed for the Docker image or the self-contained Windows build.

## Configuration

All settings can be given as environment variables, in `appsettings.json` next to the executable, or as `--key=value` command-line arguments. Environment variables and arguments override the file.

| Key | Required | Description |
|---|---|---|
| `tenantId` | yes | Azure tenant ID |
| `subscriptionId` | yes | Azure subscription ID |
| `rgName` | yes | Resource group containing the DNS zone |
| `zoneName` | yes | DNS zone, e.g. `example.com` |
| `recordsetName` | yes | A-record name, e.g. `www` or `home`. Created if missing. |
| `clientId` | yes | Service principal application (client) ID |
| `clientSecret` | yes | Service principal secret |
| `intervalInMinutes` | no | Check interval. Default `5`. |
| `ttlInSeconds` | no | TTL written to the record. Default `3600`. |
| `publicIpEndpoint` | no | URL returning your public IPv4 as plain text. Default `https://checkip.amazonaws.com/`. |

## Run on Windows

### Option A: Windows Service (recommended)

1. Download `azure-dns-updater-win-x64.zip` from the latest [workflow run](../../actions) or build it yourself:
   ```
   dotnet publish src/azure-dns-updater.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
   ```
   The result is a single `azure-dns-updater.exe` that does not need .NET installed.
2. Copy the folder somewhere permanent, e.g. `C:\Services\azure-dns-updater\`.
3. Fill in `appsettings.json` in that folder (or set system-wide environment variables).
4. Open an **elevated** PowerShell and register the service:
   ```powershell
   sc.exe create "AzureDnsUpdater" binPath= "C:\Services\azure-dns-updater\azure-dns-updater.exe" start= auto DisplayName= "Azure DNS Updater"
   sc.exe failure "AzureDnsUpdater" reset= 86400 actions= restart/60000/restart/60000/restart/60000
   sc.exe start "AzureDnsUpdater"
   ```
5. Check logs in **Event Viewer → Windows Logs → Application**, source `azure-dns-updater`.

To remove: `sc.exe stop AzureDnsUpdater` then `sc.exe delete AzureDnsUpdater`.

### Option B: Console

Run `azure-dns-updater.exe` directly from a terminal. Logs go to the console. Stop with Ctrl+C.

### Option C: Docker Desktop

Same as the Linux instructions below.

## Run with Docker

Published image (linux/amd64 and linux/arm64):

```
docker run -d --restart unless-stopped --name azure-dns-updater \
  -e tenantId=XXXXX -e subscriptionId=XXXXX -e rgName=XXXXX -e zoneName=XXXXX \
  -e recordsetName=XXXXX -e clientId=XXXXX -e clientSecret=XXXXX -e intervalInMinutes=5 \
  ghcr.io/nikosdelis/azure-dns-updater:latest
```

Build locally:

```
docker build -t azure-dns-updater .
```

## Development

```
dotnet run --project src -- --tenantId=... --subscriptionId=... --rgName=... --zoneName=... --recordsetName=... --clientId=... --clientSecret=...
```

Never commit secrets. `appsettings.Production.json` and `appsettings.Development.json` are git-ignored for local use.
