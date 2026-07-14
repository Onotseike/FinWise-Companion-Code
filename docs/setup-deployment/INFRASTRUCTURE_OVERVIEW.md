# Infrastructure Overview

The repo currently has no top-level `Infrastructure/` IaC folder. Runtime infrastructure expectations are defined by application configuration.

## Required external services

1. Azure OpenAI
2. Azure Service Bus
3. Azure Cosmos DB

## Where configuration is consumed

1. Agent `Program.cs` files
2. Agent `local.settings.json` (local only, not committed with secrets)
3. Shared setup utilities inside `Shared\FinWise.Shared.Core`

## CI note

Repository CI workflow exists at `.github\workflows\ci_cd.yml`.
