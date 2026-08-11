# NutriIndex

Scan a food product barcode, enter what you paid, and see nutrition cost indices in EUR:

- cost per 100 kcal
- cost per 10g protein

## Stack

- `NutriIndex.Core` — shared models and index calculations
- `NutriIndex.Api` — ASP.NET Core Minimal API (Open Food Facts proxy + calculate endpoint)
- `NutriIndex.Web` — Blazor WebAssembly PWA with camera barcode scanning

## Prerequisites

- .NET 7 SDK

## Run locally

Start the API:

```bash
dotnet run --project src/NutriIndex.Api
```

In another terminal, start the web app:

```bash
dotnet run --project src/NutriIndex.Web
```

Open `http://localhost:5210`.

The web app calls the API at `http://localhost:5234` (see `src/NutriIndex.Web/wwwroot/appsettings.Development.json`).

## Test

```bash
dotnet test
```

## Try it

1. Tap **Scan barcode** (or enter a barcode manually).
2. Example barcode: `3017620422003` (Nutella).
3. Enter price and quantity — indices update live.

## Deploy to Fly.io

Single app: API + Blazor PWA on the same domain (HTTPS included).

### Prerequisites

- [Fly.io account](https://fly.io/app/sign-up)
- [flyctl installed](https://fly.io/docs/hands-on/install-flyctl/)

### First deploy

```bash
fly auth login
fly launch --no-deploy
```

When prompted, keep the generated app name or pick your own, then update `app` in `fly.toml` to match.

```bash
fly deploy
```

Your app will be live at `https://<app-name>.fly.dev`.

### Verify

```bash
curl https://<app-name>.fly.dev/health
```

Open the URL on your phone to test barcode scanning (requires HTTPS — Fly provides this automatically).

### Local Docker test

```bash
docker build -t nutriindex .
docker run -p 8080:8080 nutriindex
```

Open `http://localhost:8080`.

### Notes

- Production serves API and PWA from the same origin — no CORS config needed.
- Local dev still uses separate API (port 5234) and Web (port 5210) with CORS enabled.
- Fly free tier may sleep idle machines; first request after idle can take a few seconds.

## Next steps

- Save scanned products per user
- Rankings and history
- SQLite persistence
