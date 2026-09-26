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

These live in `Cratis.Screenplay.Contexts`. A runtime such as Stage supplies the instance; inline `csharp` blocks and imported files compile against it, in scope as `context`. Reducer rules receive a `ReducerContext` with `State` (null before the first event) and `Event`; they have `StateAs<T>()` and `EventAs<T>()` too.

## Typed context sidecars

Semantic compilation publishes `TypedContextDescriptors` beside `ImplementationRequirements` (pair by `ContextsFor(requirementId)`). The descriptor contract is revision 1, independently of ESM and the role's result contract. Each ordered member names a portable model type or runtime token, its nullability and source identity/path. Shaped payloads include ordered properties with their resolved portable types and stable IDs; an optional payload property does not make its containing payload nullable. `IsFirst` and `IsWholeArtifact` are marked derived. `*As<T>()` accessors are methods, not stored data members. A wrapper provider must reject unknown descriptor contract/context versions, unresolved shapes and unsupported roles rather than generating `dynamic`. Successful descriptors carry the exact `ModelRevision` of the same compilation and `IsWrapperReady = true`; failed compilations publish only resolvable command-handler descriptors, with `IsWrapperReady = false` and no model revision. Each descriptor includes a transitive `Types` table for referenced concepts (including their primitive) and composite types (including their property references). A failed handler with missing type definitions is not renderable. Never attach a failed descriptor to an ESM from elsewhere.

Reducer descriptors are per `on Event`: `State` is nullable read-model shape, `Event` is the non-null *current* event generation, and `Key` is the event source identity. Rule descriptors distinguish the whole command or concept value (`Artifact`) from the validated property (`Value`); concept value rules use the concept type for both. For a rule on an optional command property, `Value` is nullable: an absent property value is null (the reference evaluator represents it as `SemanticValue.Null`), even though the `RuleContext` dynamic parameter has no nullable annotation. A provider must permit null in its generated value type and preserve the rule's documented absence semantics. Policy descriptors are per authorized command or query use site; an unused policy has no wrapper-ready context. `Property` uses `validated-property` with the property ID and a `ConstantValue` containing its path, or `validated-artifact` with an empty constant for a whole artifact; `Subject` uses `unavailable` where a command has no identifier. An event source includes its current contract revision. Policy use sites are necessarily joined by name because ESM policy references carry names rather than IDs. A command with a resolvable shape exposes a `CommandContext` v1 descriptor even if its handler fails ESM binding. No handler result or envelope is implied. Query performers, reactions and constraints have no descriptors yet.

The public `SemanticContextTypeKinds` vocabulary is `runtime`, `shape`, `model`; `SemanticContextRuntimeTokens` are `Text`, `WholeNumber`, `Boolean`, `DateTime`, `TenantId`, `Identity`, `CausedBy`, `Causation`. The first four match portable model primitives (with `DateTime` representing the runtime occurrence time); the latter four are named host contracts. `SemanticContextSourceKinds` are `context-contract`, `derived`, `command`, `read-model`, `current-event`, `event-source-id`, `model-property`, `concept-value`, `validated-property`, `validated-artifact`, `authorized-operation`, `command-identifier`, `query-key`, `unavailable`. The public `SemanticTypedContextSerializer` produces the checked-in JSON vectors `typed-contexts-v1.json` and `unbound-handler-context-v1.json`, independently of `ModelRevision`.

These are sidecars of the **same compilation** as the ESM: canonical v1–v4 bytes and revisions do not include them. An ESM-only loader cannot reconstruct a typed context. Providers must carry the requirement ID, descriptor version and the matching model provenance together with the attachments, not combine arbitrary compilations.

## The values they carry

| Type | Carries |
| --- | --- |
| `TenantId` | The tenant identifier. `TenantId.Default` for a single-tenant application. |
| `Identity` | `Id`, `Name`, `UserName`, `IsAuthenticated`, `Roles`, `Claims` — who the caller is and what they can prove. |
| `Claim` | `Name` and `Value`. A caller may carry the same claim name more than once, so claims are a sequence rather than a dictionary. |
| `CausedBy` | `Subject`, `Name`, `UserName` — the same three values a projection reads through `$eventContext.causedBy`. |
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

In ESM v2 `produces` mappings, the portable subset is `$context.occurred`, `$context.identity.id` / `.name` / `.userName`, and the equivalent `$context.causedBy.subject` / `.name` / `.userName`. The execution request supplies the occurrence time and audit identity; `occurred` is the event occurrence time, not a guaranteed append timestamp. `identity.id` maps to the event context's `causedBy.subject`. This is separate from `$eventSourceId`, which is supplied by the fact's typed event-source context. The target property must have the matching scalar type (`DateTime` for `occurred`, `String` or `Uuid` for audit identity). Without request occurrence data, reference execution rejects a command that reads these paths.

Other paths may still be available to code and to other language surfaces, but cannot bind as a portable `produces` mapping. `identity.roles` and `identity.claims.<name>` are collections or unbounded; `causation.<anything>` is not addressable on Chronicle's collection-valued causation. `$context.tenant` is a tenant ID, not Chronicle's event namespace, and the language does not name a `$context.correlation` value. These cases report `PLAY0268` rather than guessing a mapping. A path outside the documented language catalog remains a syntax-level **warning**; admission to ESM is a separate decision.

`$context.` reaches the command and query contexts only. It is a separate namespace from [`$eventContext.`](projections/event-context.md), which reads the metadata of the event a projection is processing: neither falls back to the other, and each is checked against its own catalog. A rule and a policy have no declarative half — a `rule` names a predicate and a `policy` states conditions, and those *are* the declarative form.

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
    ```csharp
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
    ```csharp
      string orgNumber = context.ValueAs<string>();
      return orgNumber.Length == 9 && orgNumber.All(char.IsDigit);
      ```
````

Use the typed accessor for code that calls extension methods such as LINQ. For example, a rule whose value is a collection of integers can use `context.ValueAs<IEnumerable<int>>().Sum()`. The accessor returns the existing payload as `T`, so extension methods bind statically; it does not deserialize an `ExpandoObject` into a new type. The realization must supply the payload as a compatible `T`. If it does not, the accessor throws `ContextPayloadTypeMismatch` with the member name and both types (or `null`).

| Context | Typed accessor beside the dynamic member |
| --- | --- |
| `CommandContext` | `CommandAs<T>()` |
| `QueryContext` | `ArgumentsAs<T>()` |
| `RuleContext` | `ArtifactAs<T>()`, `ValueAs<T>()` |
| `PolicyContext` | `ArtifactAs<T>()` |
| `ReducerContext` | `StateAs<T>()`, `EventAs<T>()` |

`StateAs<T>()` returns null for the first event if `T` permits null (a reference or nullable value type); it throws `ContextPayloadTypeMismatch` for a non-nullable value type. Check `context.IsFirst` before reading a non-nullable state. The original `dynamic` members remain available, including for existing code that assigns one to a static local before using LINQ. These accessors require the caller to name `T`; they do not infer the shape from the Screenplay declaration.

Inside a `policy` block, `context` is the `PolicyContext` and the block answers with a `bool` — the same answer a `require` condition gives, so the two forms compose identically. See [Policies](policies.md#custom-logic).

A `file` reference compiles against the same type as the inline block it replaces, so moving a block out to a file changes nothing about what it can see.
