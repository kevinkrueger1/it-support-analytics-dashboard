# IT Operations Analytics Dashboard

A small full-stack C#/.NET project for exploring IT support ticket statistics. It uses **synthetic demonstration data only**—no company or employee data.

## Stack

- C# and ASP.NET Core 10 Minimal API
- SQLite via `Microsoft.Data.Sqlite`
- HTML/CSS/JavaScript and Chart.js
- Git/GitHub

## Features

- Initializes a local SQLite database and creates 150 synthetic support tickets on first run.
- `GET /api/health` - API health response.
- `GET /api/summary` - totals, category counts, and status counts.
- `GET /api/tickets` - latest 100 tickets, filterable using `?category=O365&status=Open`.
- Responsive dashboard with charts, summary cards, and category/status filters.
- Parameterized SQLite queries to avoid SQL injection.

## Run on macOS

1. Install the **.NET 10 SDK**, not just the runtime: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
   - Apple Silicon M1/M2/M3/M4 etc.: macOS Arm64
   - Intel Mac: macOS x64
2. Open Terminal in the project folder.
3. Check installation: `dotnet --version`
4. Restore and run:

   ```bash
   dotnet restore
   dotnet run --urls http://localhost:5000
   ```

5. Open http://localhost:5000/ in your browser.

### Test the API

Open http://localhost:5000/api/summary in your browser, or run:

```bash
curl http://localhost:5000/api/health
curl 'http://localhost:5000/api/tickets?category=O365&status=Open'
```

Stop the server with **Control+C** in Terminal.

## Reproducibility and privacy

The app creates a `tickets.db` file locally if it doesn't exist. This file is intentionally ignored by Git. Delete it while the app is stopped if you want to regenerate demonstration data. Samples are generated with a fixed random seed, though the dates are relative to the day of the first run. Chart.js is loaded from a public CDN, so the charts need internet access.

## Limitations and next improvements

- This version does **not** use real support tickets or make ML predictions.
- No authentication, editing workflows, automated tests, or cloud deployment yet.
- Add unit/integration tests and a clear data model.
- Optionally integrate predictions from the separate Python ML ticket classifier.
- Explore Azure deployment after checking service pricing.
