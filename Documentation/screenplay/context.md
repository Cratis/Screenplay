# Command and query context

Every command handler and every query performer runs *somewhere*: for a tenant, on behalf of an identity, caused by something, at a point in time. That surrounding information is the **context**, and Screenplay gives it a single shape so it means the same thing in both halves of the language — the declarative `$context.` expressions a mapping uses, and the type an inline or imported code block compiles against.

## The shapes

A command handler is given a `CommandContext`:

```csharp
public record CommandContext(
    dynamic Command,        // the command, conforming to the shape the 'command' declaration gives it
    TenantId Tenant,        // the tenant the command executes for
    CausedBy CausedBy,      // the identity that caused it
    Causation Causation,    // what caused it
    DateTimeOffset Occurred);
```

A query performer is given a `QueryContext` — the same, with the command replaced by the query's arguments:

```csharp
public record QueryContext(
    dynamic Arguments,      // the 'by' and 'filter' parameters the query declares
    TenantId Tenant,
    CausedBy CausedBy,
    Causation Causation,
    DateTimeOffset Occurred);
```

`Command` and `Arguments` are `dynamic` — typically an `ExpandoObject` populated from the incoming request and shaped by the `command` or `query` declaration. Everything else is strongly typed:

| Type | Carries |
| --- | --- |
| `TenantId` | The tenant identifier. `TenantId.Default` for a single-tenant application. |
| `CausedBy` | `Subject`, `Name`, `UserName` — the same three values a projection reads through `$causedBy`. |
| `Causation` | `Type` (`Command`, `Reactor`, `Schedule`, …), `Occurred`, and free-form `Properties`. |

These live in `Cratis.Screenplay.Contexts`. A runtime such as Stage supplies the instance; inline `csharp` blocks and imported handler or performer files compile against it, in scope as `context`.

## Reaching the context declaratively

The same values are reachable from a `produces` mapping, a `capture` mapping, a `tag`, or a query parameter — without any code:

| Path | Value |
| --- | --- |
| `$context.occurred` | When the command or query was received. |
| `$context.tenant` | The tenant identifier. |
| `$context.command.<property>` | A property of the command being handled. |
| `$context.arguments.<name>` | An argument of the query being performed. |
| `$context.causedBy.subject` | The subject of the calling identity. |
| `$context.causedBy.name` | The display name of the calling identity. |
| `$context.causedBy.userName` | The user name of the calling identity. |
| `$context.causation.type` | What caused this — a command, a reactor, a schedule. |
| `$context.identity.id` | The caller's identity id from the auth token. |

```screenplay
produces InvoiceRegistered
  invoiceId     = invoiceId
  registeredAt  = $context.occurred
  registeredFor = $context.tenant
  registeredBy  = $context.causedBy.subject
```

A path outside this set is a **warning** — it can never resolve against the context the language defines, though a runtime is free to expose more than the language names.

## Filling a query parameter from the context

A query parameter declared with `from` is filled from the context instead of the caller, so a value the UI must never choose — the tenant, the caller's own subject — is stated once in the document rather than trusted from the request:

```screenplay
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see"
  filter status   InvoiceStatus?
  filter tenantId TenantId from $context.tenant
  authorize IsAuthenticated
```

See [Queries](queries.md#parameters) for the full parameter syntax.

## In code

Inside a `handler` or `performer` block, `context` is the corresponding record:

````screenplay
query GetOverdueInvoices => OverdueInvoicesReadModel[]
  performer
    csharp
      ```
      return readModels
          .Where(invoice => invoice.Status == InvoiceStatus.Overdue)
          .Where(invoice => invoice.TenantId == context.Tenant)
          .OrderBy(invoice => invoice.DueDate);
      ```
````

A `file` reference compiles against the same type, so moving a block out to a file changes nothing about what it can see.
