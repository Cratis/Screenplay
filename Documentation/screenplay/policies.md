# Policies

Policies are named authorization rules. Commands and queries reference them by name with `authorize`. Multiple policies on a single construct must all pass (AND semantics). Policies support role-based, claim-based, and fully custom logic.

## Syntax

```screenplay
policy <Name>
  require authenticated
  require role "<role>"
  require claim "<claim>" matches <subject|value>
  require role "<role>" or role "<role>"
  require (role "<role>" and claim "<claim>" matches <value>)
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
| `claim "<claim>" matches <value>` | The claim must match a specific value or expression. |

Conditions combine with `or` and `and`, and parentheses group them. A condition may continue on the next line at deeper indentation.

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

policy CanManageInvoice
  require role "InvoiceManager"
    or (role "Accountant" and claim "department" matches invoice.department)
```

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
