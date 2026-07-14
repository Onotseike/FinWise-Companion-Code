# Project Breakdown

## Solution projects (`FinWise.sln`)

### Agent projects

1. `FinWise.SupervisorAgent`
2. `FinWise.BudgetingAgent`
3. `FinWise.LoanAgent`

### App project

1. `FinWise.MauiApp`

### Shared projects

1. `FinWise.Shared.Core`
2. `FinWise.Shared.Data`

### Test projects

1. `FinWise.BudgetingAgent.Tests`
2. `FinWise.LoanAgent.Tests`
3. `FinWise.MauiApp.Tests`
4. `FinWise.Shared.Tests`
5. `FinWise.SharedTest.Fixtures`

## What each top-level folder does

### `Agents\`

Azure Function apps and their domain logic.

### `Apps\`

Client UI app (`FinWise.MauiApp`) for chatting with supervisor/direct routes and viewing telemetry.

### `Shared\`

Reusable framework/models:

- base agent plumbing
- A2A message models
- telemetry models (token/hop)
- state and repository support

### `Tests\`

Unit and integration coverage for app/shared/agents.

### `docs\`

Repository documentation aligned to the current code.
