# FinWise System Overview

FinWise is a multi-agent financial assistant built on Azure Functions and .NET MAUI.

## Components

1. **MAUI client (`Apps/FinWise.MauiApp`)**
   - Shell tabs: Main, AI Assistant, Direct Routing.
   - Sends chat/routing requests and displays telemetry (including token usage modes).
2. **SupervisorAgent (`Agents/SupervisorAgent`)**
   - Main orchestration endpoint.
   - Chooses route, calls downstream agents, and composes telemetry output.
3. **BudgetingAgent (`Agents/BudgetingAgent`)**
   - Budget advice, spending analysis, and financial-health style responses.
4. **LoanAgent (`Agents/LoanAgent`)**
   - Mortgage analysis/scenarios and property query endpoints.
5. **Shared libraries**
   - `FinWise.Shared.Core`: agent framework, A2A contracts, telemetry models.
   - `FinWise.Shared.Data`: persistence/state support.

## High-level flow

```text
MAUI App -> Supervisor HTTP API -> Service Bus A2A -> Specialist Agent(s)
                                             \-> aggregate response + telemetry
```

## Telemetry output highlights

Supervisor responses include:

- routing details
- function/tool call lists
- `agentHops` and `hopSequence`
- token usage split (`supervisor`, `downstream`, `total`) with mode handling (`exact`, `estimated`, `hybrid`)

Budgeting direct responses also include measured + exact + estimated token fields.
