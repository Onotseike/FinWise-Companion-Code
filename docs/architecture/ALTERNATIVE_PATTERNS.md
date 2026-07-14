# Alternative Communication Patterns

This note records what is currently used versus what was considered.

## Pattern in use: Supervisor orchestration

```text
Client -> Supervisor -> (A2A) Budgeting/Loan -> Supervisor -> Client
```

### Why it is the default

1. Single orchestration boundary.
2. Cleaner distributed tracing and telemetry aggregation.
3. Specialist agents stay domain-focused and less coupled to each other.

## Pattern considered: lateral specialist calls

Specialist-to-specialist direct calls were considered/prototyped but are not the active default in this repository. The main drawbacks were stronger coupling and harder operational tracing.

## Decision summary

Use supervisor-led orchestration unless a future workload explicitly requires peer-to-peer specialist communication and the operational tradeoffs are accepted.
