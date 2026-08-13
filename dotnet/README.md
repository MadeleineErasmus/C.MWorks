# JobCard & Invoicing — C# Starter

A base .NET solution you can open in Visual Studio 2022 (17.12+) or `dotnet` CLI and expand.

```
dotnet/
  JobCardApp.sln
  src/
    JobCardApp.Shared/   # Models + DTOs shared by API and app
    JobCardApp.Api/      # ASP.NET Core Web API + EF Core (the "central database")
    JobCardApp.Mobile/   # .NET MAUI app (iOS + Android)
```

## 1. Prerequisites

```bash
dotnet workload install maui
```
- .NET 9 SDK
- Android: Visual Studio Android SDK, or `dotnet workload install maui-android`
- iOS: a Mac with Xcode (required by Apple for building/signing iOS)

## 2. Run the API (hosted on your machine)

```bash
cd dotnet/src/JobCardApp.Api
dotnet ef database update      # dotnet tool install -g dotnet-ef  (first time)
dotnet run --urls "http://0.0.0.0:5080"
```

Swagger UI: http://localhost:5080/swagger

**Database**: defaults to SQLite (`jobcards.db`) so it just works. To use SQL Server,
set `Database:Provider` to `SqlServer` in `appsettings.json` and fill in the connection string.

**Reaching it from a phone**: the phone must be on the same network. Use your machine's LAN IP
(e.g. `http://192.168.1.20:5080`). For real remote access, host it behind a tunnel
(ngrok/Cloudflare Tunnel) or on a VPS, and put HTTPS in front of it.

## 3. Run the mobile app

Set the API base URL in `src/JobCardApp.Mobile/Services/ApiConfig.cs`.
Note: `10.0.2.2` is how the Android emulator reaches your machine's localhost.

```bash
cd dotnet/src/JobCardApp.Mobile
dotnet build -t:Run -f net9.0-android
# iOS (on a Mac):
dotnet build -t:Run -f net9.0-ios
```

## 4. What's included

- Customers, JobCards (with line items), Invoices (generated from a jobcard)
- REST endpoints: `/api/customers`, `/api/jobcards`, `/api/invoices`,
  `POST /api/invoices/from-jobcard/{id}`
- MAUI screens: jobcard list, jobcard editor with lines, invoice list
- Totals + VAT calculated server-side in `InvoiceFactory`

## 5. Obvious next steps

- Authentication (JWT) — the API is currently open
- Offline cache + sync in the app (SQLite via `sqlite-net-pcl`)
- PDF invoice generation (QuestPDF) and email sending
- Payments / partial payments, invoice numbering per financial year
