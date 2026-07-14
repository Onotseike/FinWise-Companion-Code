# Message and Telemetry Contracts

## Canonical contract location

Primary A2A contracts are defined in:

- `Shared\FinWise.Shared.Core\A2A\AgentMessages.cs`

Telemetry/token primitives are defined in:

- `Shared\FinWise.Shared.Core\Telemetry\TokenMeasurement.cs`
- Supervisor context/models in `Agents\SupervisorAgent\Models`

## A2A envelope pattern

Agents exchange strongly typed payloads wrapped in an envelope carrying correlation and routing metadata. Supervisor uses these envelopes to invoke downstream agents and to correlate replies.

## HTTP response contracts

### Supervisor (`/api/supervisor/chat`)

Response includes:

- `response` text
- routing details
- functions/tools called
- `agentHops` with `hopSequence`
- token usage (`supervisor`, `downstream`, `total`)

### Budgeting (`/api/ai/*`)

`AgentHttpResponse` includes:

- response text and metadata
- `TokenMeasurementMode`
- measured token totals
- exact token totals (nullable)
- estimated token totals

### Loan

- `LoanAnalysisResponse` for mortgage analysis
- `LoanScenarioResponse` for mortgage scenario
- property routes return filtered property data payloads

## Token measurement behavior

Supported modes:

1. `exact`: prefer provider-reported tokens.
2. `estimated`: use local estimator only.
3. `hybrid`: use exact when present, fallback to estimated.

Supervisor aggregation computes totals from supervisor + downstream measurements, then respects selected mode for surfaced telemetry.
