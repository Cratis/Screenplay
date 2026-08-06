# Policies

Policies are named authorization rules. Commands and queries reference them by name with `authorize`. Multiple policies on a single construct must all pass (AND semantics). Policies support role-based, claim-based, and fully custom logic.

## Syntax

```screenplay
policy <Name>
  require authenticated
  require role "<role>"
  require claim "<claim>" matches <subject|"value"|expression>
  require role "<role>" or role "<role>"
  require role "<role>" or (role "<role>" and claim "<claim>" matches "<value>")
  csharp
    ```
    <C# returning PolicyResult>
    ```
```

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

When the declarative conditions cannot express the rule, drop into C#. The block must return a `PolicyResult`:

```screenplay
policy IsAdultCustomer
  csharp
    ```
    var dob = context.User.FindFirst("dateOfBirth")?.Value;
    if (dob is null) return PolicyResult.Fail("Date of birth claim missing");
    return DateTime.Parse(dob) <= DateTime.UtcNow.AddYears(-18)
        ? PolicyResult.Success()
        : PolicyResult.Fail("Customer must be 18 or older");
    ```
```

See [Commands](commands.md#authorization) and [Queries](queries.md) for how policies are applied.
