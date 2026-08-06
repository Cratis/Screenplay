# Contexts

Inline and file-referenced code is a **sandbox**. You do not write a namespace, a class, or usually even a signature — just the body — because Screenplay puts a predictable `context` in scope for you.

That only works if `context` means something exact. A command handler, a query performer, a validation rule and a policy are four different jobs, so they are given four different contexts, each shaped for what its decision actually needs. A rule cannot read the caller's roles because deciding on roles is authorization, not validation; a policy cannot read the whole event context because it decides who may act, not what happened. The shape *is* the contract.

## The four contexts

A command handler is given a `CommandContext`:

```csharp
public record CommandContext(
    dynamic Command,        // the command, conforming to the shape the 'command' declaration gives it
    TenantId Tenant,        // the tenant the command executes for
    Identity Identity,      // the caller that sent it
    CausedBy CausedBy,      // the identity recorded as having caused it
    Causation Causation,    // what caused it
    DateTimeOffset Occurred);
```

A query performer is given a `QueryContext` — the same, with the command replaced by the query's arguments:

```csharp
public record QueryContext(
    dynamic Arguments,      // the 'by' and 'filter' parameters the query declares
    TenantId Tenant,
    Identity Identity,
    CausedBy CausedBy,
    Causation Causation,
    DateTimeOffset Occurred);
```

A validation rule is given a `RuleContext` — what is being validated, and nothing about what the caller is allowed to do:

```csharp
public record RuleContext(
    dynamic Artifact,       // the whole thing under validation — the command, or the concept's own value
    dynamic Value,          // the value the rule is declared on
    string Property,        // where that value sits in the artifact; empty for a whole-artifact block
    TenantId Tenant,
    CausedBy CausedBy,      // who is calling — so "you may not approve your own request" is expressible
    DateTimeOffset Occurred);
```

A policy is given a `PolicyContext` — the caller, and what the decision is about:

```csharp
public record PolicyContext(
    dynamic Artifact,       // the command being authorized, or the query's arguments
    string Subject,         // the identifier of the thing being acted on — what 'matches subject' compares to
    Identity Identity,      // the caller: authenticated, roles, claims
    TenantId Tenant,
    DateTimeOffset Occurred);
```

These live in `Cratis.Screenplay.Contexts`. A runtime such as Stage supplies the instance; inline `csharp` blocks and imported files compile against it, in scope as `context`.

## The values they carry

| Type | Carries |
| --- | --- |
| `TenantId` | The tenant identifier. `TenantId.Default` for a single-tenant application. |
| `Identity` | `Id`, `Name`, `UserName`, `IsAuthenticated`, `Roles`, `Claims` — who the caller is and what they can prove. |
| `Claim` | `Name` and `Value`. A caller may carry the same claim name more than once, so claims are a sequence rather than a dictionary. |
| `CausedBy` | `Subject`, `Name`, `UserName` — the same three values a projection reads through `$causedBy`. |
| `Causation` | `Type` (`Command`, `Reactor`, `Schedule`, …), `Occurred`, and free-form `Properties`. |

`Identity` and `CausedBy` describe the same caller from two sides. `Identity` is the **decision** view — what a policy is allowed to inspect. `CausedBy` is the **audit** view — the three values that travel with an appended event. `Identity.Id` and `CausedBy.Subject` are the same value.

`Identity` answers the questions the declarative conditions ask, so a policy written in code reads like the one written in conditions:

```csharp
context.Identity.IsAuthenticated              // require authenticated
context.Identity.HasRole("Accountant")        // require role "Accountant"
context.Identity.ClaimValue("department")     // require claim "department" matches …
context.Identity.ClaimValues("scope")         // every value, when the caller carries the claim more than once
```

Roles and claim names match exactly — ordinal and case sensitive. The values come from a token and mean what they say.

## What each context deliberately leaves out

The differences are the point, not an oversight:

| Context | Sees | Does not see | Why |
| --- | --- | --- | --- |
| `RuleContext` | `CausedBy` | `Identity` | Rejecting an input because of *who sent it* — "you may not approve your own request" — is validation and needs the caller's identifier. Inspecting *roles or claims* is authorization and belongs in a `policy`. Leaving roles and claims out is what keeps the two apart. |
| `PolicyContext` | `Identity` | `CausedBy`, `Causation` | A policy decides what a caller may do, and needs what they can prove. It does not record anything, so the audit triple would be dead weight — and leaving it out means the one `Subject` in scope is unambiguously the thing being acted on, not the caller. |
| `CommandContext` / `QueryContext` | both | — | A handler both decides and records. |

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
| `$context.identity.id` | The caller's identifier from the auth token. |
| `$context.identity.name` | The caller's display name. |
| `$context.identity.userName` | The caller's user name. |
| `$context.identity.isAuthenticated` | Whether the caller is authenticated. |
| `$context.identity.roles` | The roles the caller holds. |
| `$context.identity.claims.<name>` | The value of a claim the caller carries. |

```screenplay
produces InvoiceRegistered
  invoiceId     = invoiceId
  registeredAt  = $context.occurred
  registeredFor = $context.tenant
  registeredBy  = $context.causedBy.subject
  department    = $context.identity.claims.department
```

A path outside this set is a **warning** — it can never resolve against the context the language defines, though a runtime is free to expose more than the language names. Everything after `$context.identity.claims.` is a claim name, so it is never checked; every other segment is.

`$context.` reaches the command and query contexts only. A rule and a policy have no declarative half — a `rule` names a predicate and a `policy` states conditions, and those *are* the declarative form.

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

Inside a named `rule` body, `context` is the `RuleContext` and the block answers with a `bool`:

````screenplay
validate
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
    csharp
      ```
      string orgNumber = context.Value;
      return orgNumber.Length == 9 && orgNumber.All(char.IsDigit);
      ```
````

`Artifact`, `Value` and the command itself are `dynamic`, so they carry whatever shape the document declares rather than a type the language would have to invent. Extension methods do not bind on a `dynamic` value — assign it to a typed local first, as above, and LINQ works normally from there.

Inside a `policy` block, `context` is the `PolicyContext` and the block answers with a `bool` — the same answer a `require` condition gives, so the two forms compose identically. See [Policies](policies.md#custom-logic).

A `file` reference compiles against the same type as the inline block it replaces, so moving a block out to a file changes nothing about what it can see.
