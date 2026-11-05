---
title: "Screenplay authorization policies"
description: "Define who may run Screenplay commands and queries with named policies, combine scope gates, and distinguish portable conditions from custom logic."
---

# Policies

Screenplay policies define who may run a command or query by checking the caller's authentication, roles, claims or custom logic. Define a policy once and attach it with `authorize` where access must be checked. Signing in identifies the caller; it does not by itself satisfy a role or claim requirement.

Start with the operation's access rule, then put shared gates on its module or feature. Every enclosing gate must also pass, so a local policy cannot widen access granted by an ancestor. The reference evaluator runs portable conditions; an application host must enforce authorization in the running application and supply the caller's identity.

## Syntax

```screenplay
policy <Name>
  require <condition>
```

Write one `require` line. Its `<condition>` can be `authenticated`, `anonymous`, `role "<role>"`, `claim "<claim>" matches <subject|"value"|expression>`, `not <condition>`, or a combination such as `role "<role>" or (role "<role>" and claim "<claim>" matches "<value>")`. Continue a condition at deeper indentation rather than starting another `require` line; a second line reports `PLAY0441`.

Alternatively, implement the policy in an inline block:

````screenplay
policy <Name>
  ```csharp
    <C# returning bool>
    ```
````

Instead of the inline `csharp` block, a policy can name its implementation with `file <path>`.

A policy may have a `require` condition or one implementation (`file` or inline code), never both. Mixing `require` with an implementation reports `PLAY0440`.

A policy also accepts one quoted or fenced-text `description` body line, printed before its implementation. This is report-only authoring metadata (`PLAY0270`): a description-only policy still needs `require`, a file or code. Markdown `documentation` is not supported here.

## Condition limits

Conditions support at most 128 nested parentheses or unary `not` operators and a
constructed expression depth of 128 (a leaf has depth 1). Grouping counts toward
nesting, not tree depth. Flat `and`/`or` chains are left-associative and count
toward tree depth too. Each condition or `authorize` requirement gets its own
budget, including policy `require` lines and ordinary conditions. The same tree
depth limit applies to the combined gate when several `authorize` lines appear
on one construct or gates are merged across a folder's files. Each added gate
joins the existing tree with `and` and counts toward its depth.

Exceeding either limit reports `PLAY0861` at the owning condition or requirement
and refuses that condition. Simplify it or split the requirement into named
policies. Parentheses inside quoted values do not count toward nesting.

## Authorization scopes

Declare `authorize <policy expression>` directly in a module or feature body, or on a command or query. Each gate can use policy names joined by `and` and `or`, with parentheses for grouping. Names written next to one another mean `and`. Every gate along the path to a command or query must pass: its own gate **and** every enclosing feature's gate (including nested features) **and** the module's gate. This is the same AND rule used for several policies on one construct. An `or` inside a gate does not bypass any enclosing gate.

```screenplay
policy HasPortalAccess
  require authenticated

policy CanManageOrders
  require role "OrderManager"

policy IsRegionalManager
  require role "RegionalManager"

module Portal
  authorize HasPortalAccess
  feature Orders
    authorize CanManageOrders or IsRegionalManager
    feature Returns
      slice StateChange ReturnOrder
        command RequestReturn
          authorize CanManageOrders
```

`RequestReturn` requires `HasPortalAccess` **and** (`CanManageOrders` **or** `IsRegionalManager`) **and** `CanManageOrders`. A feature without its own gate still inherits its ancestors' gates. When a folder declares the same module or feature gate in different files, distinct gates accumulate with AND; identical repeated gates are reported and kept once. Repeated `authorize` lines on a command or query combine with AND in authored order; printing writes them as one `authorize A and B` line. The printer keeps a gate where it was declared rather than copying inherited gates onto each command or query.

The source compiler resolves policy names at all four positions and warns about unknown names. Portable conditions bind to the executable semantic model (ESM) and run in the reference evaluator. A custom implementation binds as an opaque ESM v3 policy predicate: the model references the body, but the reference evaluator does not interpret its code. A target provider must execute it.

## Conditions

| Condition | Meaning |
| --- | --- |
| `authenticated` | The caller must be authenticated. |
| `anonymous` | Every caller passes, with or without an identity. Stands alone; see [Public commands and queries](#public-commands-and-queries). |
| `not <condition>` | The operand must be false; parentheses can group its operand. |
| `role "<role>"` | The caller must have the role. |
| `claim "<claim>" matches subject` | The claim must match the command's identifier or a keyed query's argument. |
| `claim "<claim>" matches "<value>"` | The claim must equal that literal value. |
| `claim "<claim>" matches <expression>` | The claim must equal the value the expression resolves to. |

Quote a literal target such as `"Finance"`. Leave a path such as `invoice.department`, or a `$`-rooted expression such as `$context.tenant`, unquoted so it names where to read the comparison value. These forms remain distinct in the syntax tree. `$`-rooted expressions are authoring syntax but do not bind to the portable ESM v1 profile described below.

Conditions combine with `or` and `and`, and parentheses group them. A condition may continue on the next line at deeper indentation.

`not` binds tighter than `and`, which binds tighter than `or`; `and` and `or` are left associative - the rule a general purpose language follows, so `a or b and c` means `a or (b and c)` and `a or b or c` means `(a or b) or c`. Parentheses override that. This is the one condition grammar the language has: a `produces when` condition combines by exactly the same rules, over comparisons instead of roles and claims.

Printing writes the parentheses back wherever the grouping is not the one those rules produce, so a printed policy always compiles to the condition it came from. It also writes them where mixing `or` and `and` would otherwise leave a reader to work the precedence out - the text says which grouping it means rather than assuming you know.

To allow signed-in people but exclude service identities, negate their role and claim match:

```screenplay
policy PersonOnly
  require authenticated and not role "Service" and not claim "actorKind" matches "service"
```

A caller with either the `Service` role or an `actorKind` value of `service` is denied. A missing claim has no match, so its negation is true. `not role "Service"` alone does **not** require authentication; keep `authenticated` when you need a signed-in caller. `not (role "Service" or claim "actorKind" matches "service")` negates the whole group, while `not not authenticated` restores the original condition. `not` belongs to policy conditions, not named-policy expressions in `authorize`, and cannot negate an inline or file implementation.

## Public commands and queries

Some things must work for someone who has not signed in: browsing a catalog, signing up, confirming an email address. Say so with a policy that requires `anonymous`, and authorize them with it:

```screenplay
policy Anyone
  description "Open to everyone, signed in or not"
  require anonymous

module Catalog
  feature Browsing
    slice StateChange RegisterInterest
      command RegisterInterest
        authorize Anyone
        productId String identifier
        email String
        produces InterestRegistered
          for productId
          productId = productId
          email = email
      event InterestRegistered
        productId String
        email String
      specification AVisitorRegistersInterest
        when RegisterInterest
          productId = "p1"
          email = "ada@example.com"
        then InterestRegistered
          productId = "p1"
          email = "ada@example.com"
```

`anonymous` always holds. A caller without an identity passes, as do signed-in callers, system identities and reactions that run as an identity. It does not mean *only* callers without an identity. Leaving `authorize` off makes no access requirement in the model; `authorize Anyone` explicitly declares public access.

Stage renders this declaration as `[AllowAnonymous]`, but a host-wide sign-in requirement can still restrict access before the operation is reached. In Stage's generated host, configuring Microsoft Entra ID sign-in enables such a perimeter. Check the host's authentication settings as well as the policy when an intended public operation requires sign-in.

Because it lets everyone in, an anonymous policy is the whole authorization of what it gates:

- `anonymous` is the only condition on its `require` line. Combining it with another condition reports `PLAY0804`, and `not anonymous` reports `PLAY0805` - write `require authenticated` instead.
- An `authorize` line that names an anonymous policy names nothing else (`PLAY0806`).
- A command or query gated by an anonymous policy has no other gate on its module, its feature or itself (`PLAY0807`). Gates combine with `and`, while `[AllowAnonymous]` in a generated application overrides `[Authorize]`; refusing the mix keeps the model and the application in agreement. Put the anonymous gate on the public commands and queries, and keep the other gates on the rest.

A specification for a command or query gated only by an anonymous policy may leave out `given caller`, as `AVisitorRegistersInterest` does; one that gives a caller is checked the same way. A model with an anonymous policy uses ESM v31 ([decision 0103](https://github.com/Cratis/Screenplay/blob/main/decisions/0103-admit-the-anonymous-policy-condition-in-esm-v31.md)).

## Examples

```screenplay
policy IsAuthenticated
  require authenticated

policy IsAccountant
  require role "Accountant"

policy CanViewSensitiveFinancials
  require role "FinanceDirector"
    or role "Auditor"

policy IsCustomerSelf
  require claim "customerId" matches subject

policy IsFinanceDepartment
  require claim "department" matches "Finance"

policy CanManageInvoice
  require role "InvoiceManager"
    or (role "Accountant" and claim "department" matches invoice.department)
```

`IsFinanceDepartment` compares against the literal text `Finance`. `CanManageInvoice` allows an `InvoiceManager`, or an `Accountant` whose department matches `invoice.department`. Its parentheses make the grouping explicit but do not change it: `and` already binds tighter than `or`.

## Portable evaluation

Positive declarative policies execute in the portable ESM v1 reference evaluator. Policy-condition negation selects ESM v7 as a byte-preserving extension ([decision 0027](https://github.com/Cratis/Screenplay/blob/main/decisions/0027-policy-negation-joins-esm-v7.md)); models without it keep their existing versions, bytes and revisions. Pre-extension v7 strict readers reject the new `not` variant. A consumer must explicitly support negation before reading, executing or rendering a model that uses it.

Inline `csharp` and `file` policy predicates bind to ESM v3 with a stable requirement id, context/result contract version 1, and the required capability `pure`. The compilation result's implementation requirement carries the inline body's exact source span and dedented line map, or a resolved file's whole-file span; canonical ESM JSON still carries only the requirement id. Without a host-supplied provider, an authorization that depends on one returns `SemanticUnsupported` with the Authorization capability and the policy name; it never guesses allow or deny.

An opt-in `ISemanticBodyExecutionProvider.AuthorizeAsync` receives verified source and a `SemanticPolicyContext`: an artifact with semantic property ids, the explicit command identifier or single query key as invariant scalar subject text, caller authentication, ordered roles and repeated claims through `SemanticBodyIdentity`, and supplied tenant and occurrence time. Unavailable subjects, tenants and times remain null; generated identities and audit names are not invented. Role claims are normalized into roles, preserving order and duplicates. Claim names compare ordinal-ignore-case, role names and claim values ordinal. The provider returns an explicit allow, deny or unsupported answer.

Invalid answers, source/hash failures, exceptions and cancellation remain unsupported, never denial. A specification asserting `then denied` does not pass on this unsupported outcome. The host owns language admission, compilation, isolation and time limits. The reference evaluator does not compile or interpret bodies.

For a mixed gate, evaluation follows authored order: a portable denial on the left of `and` denies without evaluating the right, and a portable allowance on the left of `or` allows without evaluating the right. If an opaque operand is reached first without an executing provider, the reference evaluator returns unsupported; a later portable operand cannot decide the result.

With a provider, its allow or deny participates at that same position; an unsupported answer still cannot be bypassed. Enclosing gates combine in module → feature → construct order. This matches Stage's generated C# `&&`/`||` expression (`SemanticPolicyArtifactRenderer.Authorization`) and Arc's ordered, short-circuiting policy loops (`ArcAuthorizationPolicyRuntime.IsAuthorized` and `AspNetAuthorizationPolicyRuntime.IsAuthorized`), following the runtime-authority boundary in [decision 0001](https://github.com/Cratis/Screenplay/blob/main/decisions/0001-chronicle-runtime-semantic-authority.md).

For a command or query with an authorization gate, the execution request must supply a caller explicitly unless the gate is an [anonymous policy](#public-commands-and-queries). An absent caller otherwise denies.

An authenticated condition checks the caller's authentication flag; a role compares the caller's roles by ordinal, case-sensitive text. Claim **types** compare ordinal-ignore-case, while claim **values** compare ordinal, case-sensitively. If a caller carries several values for the same claim type, **any** matching value satisfies that condition. A missing caller claim makes a comparison against a known text target false, so its negation is true.

A comparison target that is missing, null, an absent subject, or not text instead makes the condition **unknown**. `not unknown` remains unknown, including under double negation; a policy whose final condition is unknown denies. Conditions use three-valued Kleene logic: `true or unknown` is true, `false or unknown` is unknown, `false and unknown` is false, and `true and unknown` is unknown (in either operand order). Thus a satisfied role alternative can still allow access when another target is undecidable. Positive policies keep their existing allow/deny outcomes. Opaque predicates cannot be nested under negation: unsupported execution is never converted to false or permission. `and` and `or` retain authored-order short-circuiting when the left operand decides the result.

In the portable ESM v1 profile, an artifact path must resolve from a command property (including declared composite members) or the keyed query argument. An absent or null nested value makes its claim comparison unknown. `$`-rooted expressions remain valid authoring syntax but do not bind to this portable profile. A `subject` comes from a command's identifier property or a keyed query's argument; if no identifier is available, it cannot match.

When binding an authorized command or query, the compiler warns with `PLAY0546` if a claim under `not` (including inside a group) targets an optional path, a command's `subject` without an identifier, or a non-string type. The warning does not prevent binding; check that the target is available and text when the policy runs. The TypeScript syntax compiler does not perform this semantic binding check.

A failed effective authorization yields `Unauthorized` before command validation or query lookup and never changes the world. For the caller fixture and denial assertion, see [Specifications](specifications.md#rejections).

## Custom logic

Use a custom predicate when declarative conditions cannot express the access rule. The C# body below returns a `bool`, so its allow or deny answer composes with other policies just like a `require` condition. Running the body needs a host or target provider that admits and executes it:

````screenplay
policy IsDuringServiceHours
  ```csharp
    DateTimeOffset occurred = context.Occurred;
    return occurred.Hour >= 9 && occurred.Hour < 17;
    ```
````

Stage checks policy bodies against its pure-body allowlist. This example uses the supplied receipt time, `context.Occurred`, rather than reading the wall clock. Use `DateTimeOffset`; `System.DateTime`, parsing with `TryParse`, `UtcDateTime`, and ambient `UtcNow` reads are outside that subset.

The same custom logic can live in a repository-relative file instead of an inline block:

```screenplay
policy IsDuringServiceHours
  file Policies/IsDuringServiceHours.cs
```

`file` is the *implemented by* alternative: the file provides the same `PolicyContext` and `bool` answer as the inline block, not a location for generating a declaration. The compiler does not resolve the path; an absolute path warns with `PLAY0264`.

A policy returns whether the caller may act, not a denial message. This is the same contract for declarative conditions and custom code. The host chooses the message a user sees; use `validate` for input-validation rejections that carry messages.

## What the code can see

`context` is the `PolicyContext` — the caller, and what the decision is about:

| Member | Value |
| --- | --- |
| `context.Identity` | The caller: `Id`, `Name`, `UserName`, `IsAuthenticated`, `Roles`, `Claims`. |
| `context.Artifact` | The command being authorized, or the arguments of the query. What a `matches <path>` condition resolves against. |
| `context.Subject` | The identifier of the thing being acted on. What `matches subject` compares the claim to. |
| `context.Tenant` | The tenant the authorized command or query is executing for. |
| `context.Occurred` | When the authorized command or query was received. |

`Identity` answers the same questions the conditions ask, so the two forms read alike:

```csharp
context.Identity.IsAuthenticated              // require authenticated
context.Identity.HasRole("Accountant")        // require role "Accountant"
context.Identity.ClaimValue("department")     // require claim "department" matches …
```

A policy is given an `Identity` and not a `CausedBy`: it decides what a caller may do and needs what they can prove, and it records nothing. See [Contexts](context.md) for the full set and for how a rule's context deliberately differs.

See [Commands](commands.md#authorization) and [Queries](queries.md) for how policies are applied.
