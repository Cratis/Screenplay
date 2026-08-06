# Policies

Policies are named authorization rules. Commands and queries reference them by name with `authorize`. Multiple policies on a single construct must all pass (AND semantics). Policies support role-based, claim-based, and fully custom logic.

## Syntax

````screenplay
policy <Name>
  require authenticated
  require role "<role>"
  require claim "<claim>" matches <subject|"value"|expression>
  require role "<role>" or role "<role>"
  require role "<role>" or (role "<role>" and claim "<claim>" matches "<value>")
  csharp
    ```
    <C# returning bool>
    ```
````

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

`or` and `and` do not take precedence over each other. Conditions combine strictly left to right, so `a or b and c` means `(a or b) and c` - not `a or (b and c)`. Parentheses are the only way to group differently, and printing a policy writes them wherever the grouping is not the one reading left to right produces.

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

## Custom logic

When the declarative conditions cannot express the rule, drop into C#. The block answers with a `bool` — the same answer `require authenticated` gives, so a policy written in code and a policy written in conditions mean the same kind of thing and compose the same way:

````screenplay
policy IsAdultCustomer
  csharp
    ```
    var dateOfBirth = context.Identity.ClaimValue("dateOfBirth");
    return dateOfBirth is not null && DateTime.Parse(dateOfBirth) <= DateTime.UtcNow.AddYears(-18);
    ```
````

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
