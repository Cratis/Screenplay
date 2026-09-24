# Policies

Policies are named authorization rules. Modules, features, commands, and queries reference them by name with `authorize`. Policies support role-based, claim-based, and fully custom logic. Authorization gates on enclosing scopes narrow access; they never replace a gate closer to a command or query.

## Syntax

````screenplay
policy <Name>
  require authenticated
  require role "<role>"
  require claim "<claim>" matches <subject|"value"|expression>
  require role "<role>" or role "<role>"
  require role "<role>" or (role "<role>" and claim "<claim>" matches "<value>")
  ```csharp
    <C# returning bool>
    ```
````

Instead of the inline `csharp` block, a policy can name its implementation with `file <path>`. A policy may have a `require` condition or one implementation (`file` or inline code), never both. Mixing `require` with an implementation reports `PLAY0440`.

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

The source compiler resolves policy names at all four positions and warns about unknown names. Portable conditions bind to the ESM and run in the reference evaluator. A custom implementation binds as an opaque ESM v3 policy predicate; it requires a target provider to evaluate it.

## Conditions

| Condition | Meaning |
| --- | --- |
| `authenticated` | The caller must be authenticated. |
| `role "<role>"` | The caller must have the role. |
| `claim "<claim>" matches subject` | The claim must match the subject of the current event source. |
| `claim "<claim>" matches "<value>"` | The claim must equal that literal value. |
| `claim "<claim>" matches <expression>` | The claim must equal the value the expression resolves to. |

A quoted target is the value itself; anything unquoted - a path such as `invoice.department`, or a `$` rooted expression such as `$context.tenant` - names where the value is read from. Quote a literal, leave an expression unquoted; the two are distinct in the syntax tree, so a tool consuming a policy always knows which one it is looking at.

Conditions combine with `or` and `and`, and parentheses group them. A condition may continue on the next line at deeper indentation.

`and` binds tighter than `or`, and both are left associative - the rule a general purpose language follows, so `a or b and c` means `a or (b and c)` and `a or b or c` means `(a or b) or c`. Parentheses override that. This is the one condition grammar the language has: a `produces when` condition combines by exactly the same rules, over comparisons instead of roles and claims.

Printing writes the parentheses back wherever the grouping is not the one those rules produce, so a printed policy always compiles to the condition it came from. It also writes them where mixing `or` and `and` would otherwise leave a reader to work the precedence out - the text says which grouping it means rather than assuming you know.

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

`IsFinanceDepartment` compares against the literal text `Finance`. `CanManageInvoice` compares against whatever `invoice.department` resolves to, and its parentheses are load bearing - without them the condition would mean `(role "InvoiceManager" or role "Accountant") and claim "department" matches invoice.department`, which lets an `InvoiceManager` through only when their department also matches.

## Portable evaluation

Declarative policies execute in the portable ESM v1 reference evaluator. Inline `csharp` and `file` policy predicates bind to ESM v3 with a stable requirement id, context/result contract version 1, and the required capability `pure`. The reference evaluator cannot execute their bodies. An authorization that depends on one returns `SemanticUnsupported` with the Authorization capability and the policy name; it never guesses allow or deny. A specification asserting `then denied` does not pass on this unsupported outcome. A provider must admit and evaluate the implementation before it can decide authorization.

For a mixed gate, evaluation follows authored order: a portable denial on the left of `and` denies without evaluating the right, and a portable allowance on the left of `or` allows without evaluating the right. If an opaque operand is reached first, the reference evaluator returns unsupported; a later portable operand cannot decide the result. Enclosing gates combine in module → feature → construct order. This matches Stage's generated C# `&&`/`||` expression (`SemanticPolicyArtifactRenderer.Authorization`) and Arc's ordered, short-circuiting policy loops (`ArcAuthorizationPolicyRuntime.IsAuthorized` and `AspNetAuthorizationPolicyRuntime.IsAuthorized`), following the runtime-authority boundary in [decision 0001](../../decisions/0001-chronicle-runtime-semantic-authority.md). An absent caller still denies authorization. The execution request must supply a caller explicitly when an authorized command or query runs. A missing caller cannot satisfy authorization. An authenticated condition checks the caller's authentication flag; a role compares the caller's roles by ordinal, case-sensitive text. Claim **types** compare ordinal-ignore-case, while claim **values** compare ordinal, case-sensitively. If a caller carries several values for the same claim type, **any** matching value satisfies that condition. Missing claims, a null artifact value, and an unresolved subject deny; `and` and `or` short-circuit according to the parsed grouping.

In the portable ESM v1 profile, an artifact path must resolve from a command property (including declared composite members) or the keyed query argument. An absent or null nested value denies. `$`-rooted expressions remain valid authoring syntax but do not bind to this portable profile. A `subject` comes from a command's identifier property or a keyed query's argument; if no identifier is available, it cannot match. A failed effective authorization yields `Unauthorized` before command validation or query lookup and never changes the world. For the caller fixture and denial assertion, see [Specifications](specifications.md#rejections).

## Custom logic

When the declarative conditions cannot express the rule, drop into C#. The block answers with a `bool` — the same answer `require authenticated` gives, so a policy written in code and a policy written in conditions mean the same kind of thing and compose the same way:

````screenplay
policy IsAdultCustomer
  ```csharp
    var dateOfBirth = context.Identity.ClaimValue("dateOfBirth");
    return dateOfBirth is not null && DateTime.TryParse(dateOfBirth, System.Globalization.CultureInfo.InvariantCulture,
      System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
      out var born) && born <= context.Occurred.UtcDateTime.AddYears(-18);
    ```
````

The same custom logic can live in a repository-relative file instead of an inline block:

```screenplay
policy IsAdultCustomer
  file Policies/IsAdultCustomer.cs
```

`file` is the *implemented by* alternative: the file provides the same `PolicyContext` and `bool` answer as the inline block, not a location for generating a declaration. The compiler does not resolve the path; an absolute path warns with `PLAY0264`.

There is deliberately no result type carrying a denial reason. The declarative half has no way to say *why* a `require` failed either, and adding one only to the code half would split the language into two kinds of policy — one that can explain itself and one that cannot. A policy answers whether the caller may act; the message a user sees is a runtime's concern, and `validate` is where the language attaches messages to rejections.

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
