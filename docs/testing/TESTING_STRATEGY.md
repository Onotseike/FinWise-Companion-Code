# Testing Strategy

## Test projects in solution

1. `FinWise.BudgetingAgent.Tests`
2. `FinWise.LoanAgent.Tests`
3. `FinWise.MauiApp.Tests`
4. `FinWise.Shared.Tests`
5. `FinWise.SharedTest.Fixtures`

## Test layers

1. Unit tests for domain/service logic.
2. Integration-style tests for agent workflows and shared telemetry behavior.
3. ViewModel-focused tests for MAUI behavior where applicable.

## Execution

```powershell
dotnet test FinWise.sln
```

## Manual app testing (Visual Studio)

1. Open `FinWise.sln`.
2. Set **Solution Properties -> Startup Project -> Multiple startup projects**.
3. Set these projects to **Start**:
   - `FinWise.SupervisorAgent` (profile: `FinWise.SupervisorAgent`, port `7072`)
   - `FinWise.BudgetingAgent` (profile: `FinWise.BudgetingAgent`, port `7240`)
   - `FinWise.LoanAgent` (profile: `FinWise.LoanAgent`, port `7095`)
   - `FinWise.MauiApp` (profile: `Windows Machine`)
4. Start debugging and confirm all four launch.
5. In MAUI:
   - Test **Financial Assistant** tab for supervisor-routed chat.
   - Test **Direct Routing** tab by selecting Budgeting and Loan explicitly.
   - Switch token mode (`hybrid`, `exact`, `estimated`) and verify telemetry reflects the selected mode.
6. Verify endpoints in a browser/Postman:
   - `GET http://localhost:7072/api/supervisor/health`
   - `GET http://localhost:7240/api/ai/analyze-spending?timeframe=monthly`
   - `GET http://localhost:7095/api/loan/health`

### Platform note

MAUI supervisor base URL defaults:

1. Windows: `http://localhost:7072`
2. Android emulator: `http://10.0.2.2:7072`
3. iOS simulator/device: use host IP via `SupervisorAgent:BaseUrl`

## Focus areas

1. Supervisor routing and response composition.
2. Token measurement mode behavior (`exact`, `estimated`, `hybrid`).
3. Agent hop sequencing and telemetry consistency.
4. Budgeting and loan endpoint response contracts.
