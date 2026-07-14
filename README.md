# FinWise

FinWise is a .NET 10 multi-agent financial assistant. It has a .NET MAUI client, a Supervisor Azure Function that orchestrates requests, and two specialist Azure Functions (Budgeting and Loan) connected through Azure Service Bus and persisted with Cosmos DB.

## Architecture

```text
MAUI App (Apps/FinWise.MauiApp)
          |
          | HTTP
          v
SupervisorAgent (Agents/SupervisorAgent)
          |
          | A2A via Service Bus
   +------+------+
   |             |
   v             v
BudgetingAgent   LoanAgent
```

## Current solution projects

- `Agents\BudgetingAgent\FinWise.BudgetingAgent.csproj`
- `Agents\LoanAgent\FinWise.LoanAgent.csproj`
- `Agents\SupervisorAgent\FinWise.SupervisorAgent.csproj`
- `Apps\FinWise.MauiApp\FinWise.MauiApp.csproj`
- `Shared\FinWise.Shared.Core\FinWise.Shared.Core.csproj`
- `Shared\FinWise.Shared.Data\FinWise.Shared.Data.csproj`
- `Tests\FinWise.BudgetingAgent.Tests\FinWise.BudgetingAgent.Tests.csproj`
- `Tests\FinWise.LoanAgent.Tests\FinWise.LoanAgent.Tests.csproj`
- `Tests\FinWise.MauiApp.Tests\FinWise.MauiApp.Tests.csproj`
- `Tests\FinWise.Shared.Tests\FinWise.Shared.Tests.csproj`
- `Tests\FinWise.SharedTest.Fixtures\FinWise.SharedTest.Fixtures.csproj`

## Primary HTTP endpoints

### Supervisor

- `GET /api/supervisor/health`
- `POST /api/supervisor/chat`
- `POST /api/supervisor/route/{agentName}`

### Budgeting

- `POST /api/ai/chat`
- `GET /api/ai/budget-advice`
- `GET /api/ai/analyze-spending?timeframe=...`
- `POST /api/ai/create-budget`

### Loan

- `GET /api/loan/health`
- `POST /api/loan/analyze-mortgage`
- `POST /api/loan/mortgage-scenario`
- `GET /api/loan/properties`
- `GET /api/loan/properties/location`
- `GET /api/loan/properties/price-range`
- `POST /api/loan/properties/filter`

## Local run (minimal)

0. Fill in the necessary api keys, uris and secrets in each agent's local.settings.json files
1. `dotnet restore FinWise.sln`
2. Start each function app from its project folder with `func host start`
3. Build/run MAUI app from `Apps\FinWise.MauiApp`

## Documentation map

See [`docs/README.md`](docs/README.md) for the maintained documentation index.
