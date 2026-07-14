# Agent Overview

## Active agents

### SupervisorAgent

- **Role:** request orchestration and response composition.
- **HTTP routes:** `/api/supervisor/health`, `/api/supervisor/chat`, `/api/supervisor/route/{agentName}`.
- **Owns:** routing decisions, downstream delegation, hop/token telemetry aggregation.

### BudgetingAgent

- **Role:** budgeting and spending analysis.
- **HTTP routes:** `/api/ai/chat`, `/api/ai/budget-advice`, `/api/ai/analyze-spending`, `/api/ai/create-budget`.
- **Output:** `AgentHttpResponse` with text, metadata, and token fields (measured/exact/estimated).

### LoanAgent

- **Role:** mortgage analysis and property data responses.
- **HTTP routes:** `/api/loan/health`, `/api/loan/analyze-mortgage`, `/api/loan/mortgage-scenario`, property routes under `/api/loan/properties...`.
- **Output:** DTO responses (`LoanAnalysisResponse`, `LoanScenarioResponse`) and property payloads.

## Collaboration model

1. Supervisor receives user request.
2. Supervisor routes by intent.
3. Supervisor invokes one or more downstream agents via Service Bus.
4. Supervisor emits final response payload with consolidated telemetry.

Direct lateral agent-to-agent calls are not the default path in this repository.
