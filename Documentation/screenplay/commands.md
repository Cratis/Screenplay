# Commands

Commands are input definitions — imperative intents. A command declares its properties, its authorization, its validation rules, and what events it produces.

## Syntax

```screenplay
command <Name>
  <property> <Type>[?]
  ...

  [authorize <PolicyName> [<PolicyName>]*]

  [validate
    <rule> message "<message>"
    ...]

  [validate csharp
    ```
    <C# yielding ValidationError>
    ```]

  [produces ...]
```

## Validation rules

Declarative validation covers the common cases without code:

| Rule | Example |
| --- | --- |
| `not empty` | `name not empty` |
| `max <n>` | `reason max 500` |
| `min <n>` | `quantity min 1` |
| `> <value>` | `quantity > 0` |
| `>= <value>` | `discountPct >= 0` |
| `< <value>` | `dateOfBirth < today` |
| `length == <n>` | `currency length == 3` |
| `matches <regex>` | `email matches email` |
| `matches "<pattern>"` | `invoiceNumber matches "^INV-[0-9]{6}$"` |
| `all > <value>` (on collection) | `lines.quantity all > 0` |

Every rule carries a `message` shown when it fails:

```screenplay
validate
  invoiceNumber not empty                  message "Invoice number is required"
  invoiceNumber matches "^INV-[0-9]{6}$"  message "Invoice number must match INV-000000"
  dueDate > today                          message "Due date must be in the future"
```

Cross-field or complex rules drop into C#:

````screenplay
validate csharp
  ```
  var total = Lines.Sum(l => l.Quantity * l.UnitPrice);
  if (total > 1_000_000 && PaymentTerms == PaymentTerms.Immediate)
      yield ValidationError("Invoices over 1,000,000 cannot require immediate payment");
  ```
````

Both `validate` and `validate csharp` can coexist on the same command.

## Authorization

```screenplay
authorize <PolicyName> [<PolicyName>]*
```

Multiple policies must all pass. Continuation lines list additional policies; `or` makes policies alternatives:

```screenplay
authorize CanManageInvoice
          IsAdultCustomer

authorize IsAccountant
          or IsCustomerSelf
```

Policies are declared at the top of the file — see [Policies](policies.md).

## The `produces` block

Declares what events a command emits. Supports single, multiple, conditional, and fully imperative forms.

### Single event with property mapping

```screenplay
produces InvoiceRegistered
  invoiceId     = invoiceId              // from command property
  registeredAt  = $context.occurred      // from event context
  registeredBy  = $context.identity.id  // caller identity
  source        = $env.SERVICE_NAME      // environment variable
  status        = "draft"                // string constant
  lineCount     = 0                      // numeric constant
```

### Mapping sources

| Source | Syntax | Description |
| --- | --- | --- |
| Command property | `= <propertyName>` | Direct copy from command |
| Event context | `= $context.occurred` | Timestamp of the event |
| Caller identity | `= $context.identity.id` | Subject from auth token |
| Environment | `= $env.<VAR_NAME>` | Environment variable |
| String constant | `= "value"` | Literal string |
| Numeric constant | `= 0` | Literal number |
| Expression | `= lines.sum(l => l.quantity * l.unitPrice)` | Computed value |

### Multiple unconditional events

Repeat `produces` for each event; all are emitted:

```screenplay
produces InvoiceLineItemAdded
  invoiceId  = invoiceId
  addedAt    = $context.occurred

produces InvoiceRunningTotalUpdated
  invoiceId  = invoiceId
  adjustment = lines.sum(l => l.quantity * l.unitPrice * (1 - l.discountPct / 100))
```

### Conditional produces

`produces when <condition>` emits the indented event only when the condition holds. Conditions compare command properties, constants, and environment variables with `==`, `!=`, `>`, `>=`, `<`, `<=`, combined with `and`/`or`:

```screenplay
produces when isProForma == true
  ProFormaInvoiceIssued
    invoiceId  = invoiceId
    issuedAt   = $context.occurred

produces when paymentTerms == "net30" or paymentTerms == "net60"
  DeferredPaymentInvoiceRegistered
    invoiceId    = invoiceId
    paymentTerms = paymentTerms

produces when $env.WELCOME_EMAILS_ENABLED == "true"
  CustomerWelcomeEmailRequested
    customerId  = customerId
```

Multiple `produces when` blocks form mutually exclusive or overlapping branches — each condition is evaluated independently.

### Full imperative fallback

````screenplay
produces csharp
  ```
  var events = new List<object>();
  events.Add(new InvoiceBatchProcessingStarted(
      BatchId: BatchId,
      StartedAt: DateTimeOffset.UtcNow
  ));
  foreach (var invoiceId in InvoiceIds)
      events.Add(new InvoiceSent(invoiceId, DateTimeOffset.UtcNow, context.Identity.Id, null));
  return events;
  ```
````
