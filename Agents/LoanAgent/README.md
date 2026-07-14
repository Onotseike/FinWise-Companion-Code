# FinWise LoanAgent

LoanAgent provides mortgage analysis/scenario evaluation and property data endpoints.

## Endpoints

1. `GET /api/loan/health`
2. `POST /api/loan/analyze-mortgage`
3. `POST /api/loan/mortgage-scenario`
4. `GET /api/loan/properties`
5. `GET /api/loan/properties/location`
6. `GET /api/loan/properties/price-range`
7. `POST /api/loan/properties/filter`

## Core files

1. `LoanAgentFunctions.cs` - HTTP entry points.
2. `Services/LoanAgentOrchestrator.cs` - domain orchestration.
3. `Services/LoanAgentStateHandler.cs` - context persistence.
4. `Models/` - request/response and domain models.
5. `Modules/PropertyModule.cs` - property data filtering/query logic.
6. `Plugins/MortgagePlugin.cs` - AI tool/plugin surface.

## Output contracts

1. `LoanAnalysisResponse`
   - `ConversationId`
   - `Analysis`
   - `TraceId`
   - `SpanId`
2. `LoanScenarioResponse`
   - `ConversationId`
   - `Scenario`
   - `TraceId`
