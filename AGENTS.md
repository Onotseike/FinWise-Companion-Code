# FinWise Agent Implementation Notes

This file documents the current agent model implemented in this repository.

## Active agents

1. `SupervisorAgent`: HTTP entry point and orchestrator.
2. `BudgetingAgent`: budgeting and spending analysis.
3. `LoanAgent`: mortgage/loan analysis and property queries.

## Architecture rules

1. Inter-agent communication is Service Bus A2A only.
2. Supervisor orchestrates; specialist logic stays in specialist agents.
3. Conversation/context persistence is handled through Cosmos DB state handlers.
4. Shared message contracts are defined in `Shared\FinWise.Shared.Core\A2A`.

## Endpoint surface

### Supervisor

- `GET /api/supervisor/health`
- `POST /api/supervisor/chat`
- `POST /api/supervisor/route/{agentName}`

### Budgeting

- `POST /api/ai/chat`
- `GET /api/ai/budget-advice`
- `GET /api/ai/analyze-spending`
- `POST /api/ai/create-budget`

### Loan

- `GET /api/loan/health`
- `POST /api/loan/analyze-mortgage`
- `POST /api/loan/mortgage-scenario`
- `GET /api/loan/properties`
- `GET /api/loan/properties/location`
- `GET /api/loan/properties/price-range`
- `POST /api/loan/properties/filter`

## Telemetry conventions

1. Supervisor response payload includes routing, functions/tools called, `agentHops`, and token usage blocks (`supervisor`, `downstream`, `total`).
2. Token measurement mode is request-driven (`exact`, `estimated`, `hybrid`) and must be respected throughout aggregation.
3. Budgeting direct HTTP responses expose measured, exact, and estimated token fields in `AgentHttpResponse`.

## Project status

Active shared libraries:

- `FinWise.Shared.Core`
- `FinWise.Shared.Data`

Removed shared library:

- `FinWise.Shared.Contracts` (no longer part of the solution)
