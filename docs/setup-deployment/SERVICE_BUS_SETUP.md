# Service Bus Setup Notes

FinWise uses Azure Service Bus for agent-to-agent communication.

## Minimum requirements

1. A namespace
2. Topics/subscriptions expected by each agent's configuration
3. Managed identity or connection-string access configured in local/app settings

## Current usage pattern

1. Supervisor publishes downstream requests.
2. Specialist agent processes and publishes response.
3. Supervisor receives and correlates response by correlation/session identifiers.

## Contract guidance

Use the shared A2A envelope and payload models from `Shared\FinWise.Shared.Core\A2A`.

## Operational guidance

1. Enable dead-lettering and monitor poison messages.
2. Keep message payloads schema-stable and version deliberately.
3. Propagate trace/correlation metadata in each envelope hop.
