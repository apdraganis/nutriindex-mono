# NutriIndex

Point the camera at a barcode (or type it in), enter what you paid, and get two indices in EUR:

- cost per 100 kcal
- cost per 10g protein

Product nutrition comes from [Open Food Facts](https://world.openfoodfacts.org/). When data is missing, you can fill in kcal and protein manually.

## Architecture

Single-repo .NET 10 monolith with three projects:

| Project | Role |
|---------|------|
| `NutriIndex.Core` | Shared models and index math |
| `NutriIndex.Api` | Minimal API — Open Food Facts proxy, calculate endpoint, hosts the WASM app in production |
| `NutriIndex.Web` | Blazor WebAssembly PWA with camera barcode scanning |

In production, the API serves both `/api/*` and the static Blazor app from one origin (no CORS). Local development runs API and web on separate ports with CORS enabled.

```
Browser (PWA)
    │  barcode scan + price input
    ▼
NutriIndex.Api
    │  product lookup
    ▼
Open Food Facts API
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Run locally

Start the API:

```bash
dotnet run --project src/NutriIndex.Api
```

In another terminal, start the web app:

```bash
dotnet run --project src/NutriIndex.Web
```

Open `http://localhost:5210`. The web app calls the API at `http://localhost:5234` (see `src/NutriIndex.Web/wwwroot/appsettings.Development.json`).



## Tests

```bash
dotnet test
```

## Docker

Build and run the combined API + PWA image:

```bash
docker build -t nutriindex .
docker run -p 8080:8080 nutriindex
```

Open `http://localhost:8080`.

## Camera & PWA

Barcode scanning uses the device camera via `getUserMedia`. Browsers only allow that on **secure contexts** — `https://` or `http://localhost`. If you deploy without HTTPS, the scanner will fail and manual entry still works.

The app is installable as a PWA (`manifest.json`, service worker). Icons and offline shell are included; product lookup still needs network access.
