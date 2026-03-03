# Sports Store

ASP.NET Core Sports Store application (based on Pro ASP.NET Core 6). Upgraded to **.NET 9**.

---

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server LocalDB (Windows) or SQL Server / SQLite (see note below)

---

## How to Run Locally

1. **Restore and build**
   ```bash
   dotnet restore SportsSln.sln
   dotnet build SportsSln.sln
   ```

2. **Apply database migrations** (if using EF migrations)
   ```bash
   cd SportsStore
   dotnet ef database update
   cd ..
   ```

3. **Run the application (uses .NET 9 via `global.json`)**
   ```bash
   dotnet run --project SportsStore
   ```
   If port 5000 is in use, run on another port:
   ```bash
   dotnet run --project SportsStore --urls "http://localhost:5050"
   ```
   Or from Visual Studio: set **SportsStore** as startup project and run (F5).

4. Open in browser: **https://localhost:5001** or **http://localhost:5000** (or the port you set). Check the console for the actual URL.

---

## Project Structure

- **SportsStore** – Main web app (MVC + Razor Pages + Blazor)
- **SportsStore.Tests** – Unit tests (xUnit)

---

## Upgrade to .NET 9 (Part A)

- Target framework: `net6.0` → `net9.0` in both projects.
- NuGet packages updated to 9.x (EF Core, Identity, Test SDK, xUnit, etc.).
- Nullable reference warnings fixed for a clean build (0 warnings).

---

## Serilog structured logging (Part B)

- **Packages:** `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Enrichers.Environment`
- **Configuration:** `appsettings.json` – Serilog section with `MinimumLevel`, `WriteTo` (Console + rolling File), `Enrich` (FromLogContext, WithMachineName, WithEnvironmentName). Development uses `Debug` level in `appsettings.Development.json`.
- **Where we log:**
  - **Application startup** – `Program.cs`: `Log.Information("Application starting up. Environment: {EnvironmentName}", ...)`
  - **Checkout flow** – `OrderController`: checkout page requested (CartItemCount), checkout submitted (CustomerName, ItemCount, TotalAmount), order created (OrderId, CustomerName), empty-cart warning
  - **Order creation** – `EFOrderRepository.SaveOrder`: OrderId, LineCount
  - **Exceptions** – global middleware logs unhandled exceptions; `OrderController` logs save failures; Error page logs when user hits `/error`
- **Sinks:** Console and rolling file (`Logs/log-YYYYMMDD.txt`).

---










