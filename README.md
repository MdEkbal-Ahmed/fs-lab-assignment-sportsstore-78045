# Sports Store

ASP.NET Core Sports Store application (based on Pro ASP.NET Core 6). Upgraded to **.NET 9**.

---

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server LocalDB (Windows) or SQL Server / SQLite (see note below)
- SEQ

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

- **Packages:** `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Sinks.Seq`, `Serilog.Enrichers.Environment`
- **Configuration:** `appsettings.json` – Serilog section with `MinimumLevel`, `WriteTo` (Console + rolling File), `Enrich` (FromLogContext, WithMachineName, WithEnvironmentName). Development uses `Debug` level in `appsettings.Development.json`.
- **Where we log:**
  - **Application startup** – `Program.cs`: `Log.Information("Application starting up. Environment: {EnvironmentName}", ...)`
  - **Checkout flow** – `OrderController`: checkout page requested (CartItemCount), checkout submitted (CustomerName, ItemCount, TotalAmount), order created (OrderId, CustomerName), empty-cart warning
  - **Order creation** – `EFOrderRepository.SaveOrder`: OrderId, LineCount
  - **Exceptions** – global middleware logs unhandled exceptions; `OrderController` logs save failures; Error page logs when user hits `/error`
- **Sinks:** Console, rolling file (`Logs/log-YYYYMMDD.txt`), and **Seq** (see below).

### Seq – structured logging in the UI

Logs are also sent to **Seq** so you can search and filter structured events in a browser.

- **How it works:** The app uses `Serilog.Sinks.Seq`. At startup, Serilog reads the `Serilog.WriteTo` config from `appsettings.json` and sends each log event to the Seq server URL (default `http://localhost:5341`). You see the same events as in the file/console, but in Seq you can filter by properties (e.g. `CartId`, `OrderId`, `Username`).
- **What appears in Seq:** All Serilog events, including:
  - **Application:** startup (`EnvironmentName`).
  - **Cart:** item added/removed (`CartId`, `ProductId`, `ProductName`, `Username`).
  - **Checkout / Stripe:** checkout page requested (`CartId`, `CartItemCount`, `Username`); checkout submitted (`CartId`, `CustomerName`, `ItemCount`, `TotalAmount`, `Username`); **Stripe Checkout Session created** (`SessionId`, `AmountCents`); **Stripe session retrieved** (`SessionId`, `PaymentStatus`, `IsPaid`); order created after payment (`OrderId`, `CustomerName`, `StripeSessionId`, `Username`); payment cancelled (`CartId`, `Username`); Stripe not configured or session failed (`CustomerName`, `CartId`); payment error cases (`SessionId`, `CustomerName`, etc.).
  - **Orders:** order saved (`OrderId`, `LineCount`).
  - **Auth:** login succeeded/failed and logout (`Username`).
  - **Errors:** unhandled exceptions (`RequestPath`), order save failures, Error page hits.
  - **HTTP:** request logging (URL, method, status, duration).

Use Seq’s search/filter (e.g. by `StripeSessionId`, `CartId`, or `Username`) to trace a full Stripe payment or checkout flow.

---

## Stripe payment (Part C)

Checkout uses **Stripe Checkout**. Payment is taken **before** the order is confirmed and saved.

### Configuration (required for checkout)



1. From the `SportsStore` project folder, run:
   ```bash
   cd SportsStore
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key_here"
   ```

2. (Optional) Publishable key: `dotnet user-secrets set "Stripe:PublishableKey" "pk_test_your_key_here"`

### Flow

1. User fills shipping details and clicks **Proceed to payment**.
2. They are redirected to Stripe Checkout to pay.
3. **Success:** order is saved with `StripeSessionId` and `StripePaymentIntentId`, cart cleared, redirect to Completed.
4. **Cancel:** user returns to site.
5. **Failure:** payment error page is shown.

### Database

Run migrations so orders can store payment confirmation:

```bash
cd SportsStore
dotnet ef database update
```


---










