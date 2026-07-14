# Setup, Build, and Run

## Prerequisites

1. .NET SDK 10
2. Azure Functions Core Tools v4
3. Azure resources/config for OpenAI, Service Bus, and Cosmos DB

## Build

```powershell
dotnet restore FinWise.sln
dotnet build FinWise.sln
```

## Run function apps locally

Open separate terminals:

1. `Agents\SupervisorAgent` -> `func host start`
2. `Agents\BudgetingAgent` -> `func host start`
3. `Agents\LoanAgent` -> `func host start`

## Run MAUI app

```powershell
dotnet build Apps\FinWise.MauiApp\FinWise.MauiApp.csproj
```

Use Visual Studio or `dotnet` target-specific launch workflow for the chosen MAUI platform.

## Test

```powershell
dotnet test FinWise.sln
```
