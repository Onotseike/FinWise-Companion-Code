# Developer Quick Reference

## Main places to look

1. App UI: `Apps\FinWise.MauiApp`
2. Orchestration: `Agents\SupervisorAgent`
3. Budgeting domain: `Agents\BudgetingAgent`
4. Loan domain: `Agents\LoanAgent`
5. Shared A2A + telemetry contracts: `Shared\FinWise.Shared.Core`

## Commands

```powershell
dotnet restore FinWise.sln
dotnet build FinWise.sln
dotnet test FinWise.sln
```

## Function endpoints

- Supervisor: `/api/supervisor/*`
- Budgeting: `/api/ai/*`
- Loan: `/api/loan/*`

## Token measurement modes

- `exact`
- `estimated`
- `hybrid`

Mode is selected by the request and carried through supervisor telemetry.
