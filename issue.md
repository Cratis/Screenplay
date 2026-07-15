# Screenplay DSL — Full Documentation, EBNF Grammar, and Monaco Language Service

## Summary

Screenplay is the modeling language for the Cratis platform. It lets developers describe a complete bounded context — events, commands, queries, projections, screens, automations, authorization, validation, constraints, and concepts — in a single declarative file (`.play`). Stage interprets a Screenplay and runs it as a live application.

This issue covers:

1. Full language documentation in `Documentation/`
2. EBNF grammar definition
3. Monaco language service (syntax highlighting + IntelliSense) packaged as an NPM package
4. A standalone Vite + React + TypeScript host project for the Monaco editor

---

## Background and Language Overview

### Design principles

- **Indentation-based** — Python-style, no braces
- **Declarative first, imperative escape hatch** — every construct has a declarative form; any construct can drop into C#, TypeScript, React, or HTML via inline code blocks or `file` references
- **Slices are the atom** — everything lives inside a typed slice aligned with Event Modeling's vocabulary
- **Sub-language pluggability** — the Projection Declaration Language (PDL) and Change Data Capture Language (CDL) are embedded sub-grammars. The language is designed so additional sub-languages can be registered and parsed inside named constructs
- **Concepts carry compliance** — value types declare PII and sensitivity attributes once; all usages inherit them

### File extension

`.play`

### Top-level structure

```
<imports>
<concepts>
<policies>
<module>
  <layouts>
  <feature>+
    <feature>*          ← sub-features, arbitrarily deep
    <slice>+
      <construct>+      ← events, commands, queries, projections, captures, reactors, screens, constraints
```

---

## Language Reference

### 1. Imports

Cross-module references. Imported types are available by their short name within the module.

```
import Customers.CustomerRegistered
import Customers.CustomerDetailsReadModel
```

---

### 2. Concepts

Formalized value types that wrap a primitive. Attributes control compliance behavior. Chronicle applies `@pii` and `@sensitive` rules automatically wherever the concept is used.

**Syntax:**

```
concept <Name> : <PrimitiveType> [<attributes>]

concept <Name> : Enum
  <value>+
```

**Primitive types:** `Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`

**Attributes:** `@pii`, `@sensitive`

**Examples:**

```
concept InvoiceId        : Uuid
concept EmailAddress     : String   @pii
concept NationalIdNumber : String   @pii @sensitive
concept DateOfBirth      : Date     @pii

concept InvoiceStatus : Enum
  draft
  sent
  paid
  overdue
  cancelled

concept PaymentTerms : Enum
  net30
  net60
  immediate
```

When a concept is used as a property type on a command or event, its attributes are inherited — you never annotate at the property level.

---

### 3. Policies

Named authorization rules. Commands and queries reference them. Multiple policies on a single construct must all pass (AND semantics). Policies support role-based, claim-based, and fully custom logic.

**Syntax:**

```
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

**Examples:**

```
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

---

### 4. Module

Top-level namespace. Maps to a bounded context. One module per file is the convention but not enforced.

```
module <Name>

  [<layouts>]
  [<features>]
```

---

### 5. Layout Templates

Reusable screen templates with named slots. Declared at module level.

```
layout <Name>
  template
    <slot-name>+
```

**Example:**

```
layout MasterDetail
  template
    sidebar
    main

layout DashboardLayout
  template
    header
    left
    right
    footer
```

---

### 6. Features

Vertical slice groupings. Arbitrarily nestable for sub-features.

```
feature <Name>
  [feature <Name>]*   ← sub-features
  [slice <type> <Name>]+
```

---

### 7. Slices

The atomic unit of behavior, aligned with Event Modeling. A slice has a type and a name.

**Slice types:**

| Type | Description |
|---|---|
| `StateChange` | A command → events flow; something that changes the system |
| `StateView` | A query + projection + screen; something that reads the system |
| `Automation` | A reactor or reducer; something that reacts to events |
| `Translate` | A capture; converts external data into events |

**Syntax:**

```
slice <SliceType> <Name>
  <constructs>
```

---

### 8. Events

Event type definitions. Properties use concepts or primitives.

```
event <Name>
  <property> <Type>
  ...
```

**Example:**

```
event InvoiceRegistered
  invoiceId     InvoiceId
  customerId    CustomerId
  invoiceNumber InvoiceNumber
  lines         InvoiceLine[]
  currency      CurrencyCode
  registeredAt  DateTime
  status        InvoiceStatus
```

**Collection syntax:** `<Type>[]`
**Optional properties:** `<Type>?`

---

### 9. Commands

Input definitions. Commands declare authorization, validation rules, and what events they produce.

```
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

#### 9.1 Validation rules

Declarative validation covers the common cases without code:

| Rule | Example |
|---|---|
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

Cross-field or complex rules drop into C#:

```
validate csharp
  ```
  var total = Lines.Sum(l => l.Quantity * l.UnitPrice);
  if (total > 1_000_000 && PaymentTerms == PaymentTerms.Immediate)
      yield ValidationError("Invoices over 1,000,000 cannot require immediate payment");
  ```
```

Both `validate` and `validate csharp` can coexist on the same command.

#### 9.2 Authorization on commands

```
authorize <PolicyName> [<PolicyName>]*
```

Multiple policies: all must pass. `or` between policies on the same line:

```
authorize IsAccountant
          or IsCustomerSelf
```

#### 9.3 The `produces` block

Declares what events a command emits. Supports single, multiple, conditional, and fully imperative forms.

**Single event with property mapping:**

```
produces InvoiceRegistered
  invoiceId     = invoiceId              // from command property
  registeredAt  = $context.occurred      // from event context
  registeredBy  = $context.identity.id  // caller identity
  source        = $env.SERVICE_NAME      // environment variable
  status        = "draft"                // string constant
  lineCount     = 0                      // numeric constant
```

**Mapping sources:**

| Source | Syntax | Description |
|---|---|---|
| Command property | `= <propertyName>` | Direct copy from command |
| Event context | `= $context.occurred` | Timestamp of the event |
| Caller identity | `= $context.identity.id` | Subject from auth token |
| Environment | `= $env.<VAR_NAME>` | Environment variable |
| String constant | `= "value"` | Literal string |
| Numeric constant | `= 0` | Literal number |
| Expression | `= lines.sum(l => l.quantity * l.unitPrice)` | Computed value |

**Multiple unconditional events:**

```
produces InvoiceLineItemAdded
  invoiceId  = invoiceId
  lineNumber = lineNumber
  addedAt    = $context.occurred

produces InvoiceRunningTotalUpdated
  invoiceId  = invoiceId
  adjustment = lines.sum(l => l.quantity * l.unitPrice * (1 - l.discountPct / 100))
  updatedAt  = $context.occurred
```

**Conditional produces:**

```
produces when isProForma == true
  ProFormaInvoiceIssued
    invoiceId  = invoiceId
    issuedAt   = $context.occurred

produces when currency != "NOK"
  ForeignCurrencyInvoiceRegistered
    invoiceId = invoiceId
    currency  = currency
```

**Mutually exclusive branches:**

```
produces when paymentTerms == "immediate"
  ImmediatePaymentInvoiceRegistered
    invoiceId = invoiceId
    dueDate   = dueDate

produces when paymentTerms == "net30"
  DeferredPaymentInvoiceRegistered
    invoiceId    = invoiceId
    paymentTerms = paymentTerms
    dueDate      = dueDate
```

**Conditional on environment:**

```
produces when $env.WELCOME_EMAILS_ENABLED == "true"
  CustomerWelcomeEmailRequested
    customerId  = customerId
    email       = email
    requestedAt = $context.occurred
```

**Full imperative fallback:**

```
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
```

---

### 10. Queries

Read-side entry points. Map to a return type.

```
query <Name> => <ReturnType>[[]?]
  [by <paramName> <Type>]
  [filter <paramName> <Type>?]
  [authorize <PolicyName> [or <PolicyName>]*]
```

**Examples:**

```
query GetInvoice => InvoiceDetailsReadModel
  by invoiceId InvoiceId
  authorize IsAuthenticated

query ListInvoices => InvoiceListReadModel[]
  filter status     InvoiceStatus?
  filter customerId CustomerId?
  authorize IsAuthenticated
```

---

### 11. Projections (PDL sub-language)

The Projection Declaration Language (PDL) is embedded verbatim inside a `projection` block. The Screenplay parser delegates to the PDL parser for the body.

```
projection <Name> => <ReadModel>
  <PDL grammar>
```

**Example:**

```
projection InvoiceDetails => InvoiceDetailsReadModel
  key invoiceId
  from InvoiceRegistered key invoiceId
    customerId    = customerId
    invoiceNumber = invoiceNumber
    currency      = currency
    status        = "draft"
    registeredAt  = $eventContext.occurred
  from InvoiceSent
    status = "sent"
    sentAt = sentAt
  from InvoicePaid
    status = "paid"
    paidAt = paidAt
  join customer on customerId
    with CustomerRegistered
      customerName = name
  children lineItems identified by lineNumber
    from InvoiceLineItemAdded key lineNumber
      parent invoiceId
      quantity  = quantity
      unitPrice = unitPrice
    remove with InvoiceLineItemRemoved key lineNumber
      parent invoiceId
```

Full PDL grammar: https://www.cratis.io/chronicle/projections/projection-declaration-language/grammar/

---

### 12. Captures (CDL sub-language)

The Change Data Capture Language (CDL) is embedded verbatim inside a `capture` block. Used inside `Translate` slices.

```
capture <Name>
  <CDL grammar>
```

**Example:**

```
capture LegacyInvoiceCapture
  source api
    api LegacyInvoicingApi
    route /invoices
    poll 5m
  key id
  map
    status = status translate
      "utkast"     => draft
      "sendt"      => sent
      "betalt"     => paid
      "kansellert" => cancelled
  append InvoiceStatusChanged
    when status
      invoiceId = $.id
      status    = $.status
      changedAt = $context.occurred
  children lineItems identified by lineNumber
    append InvoiceLineItemAdded
      when added
        invoiceId  = $.id
        lineNumber = $.lineNumber
        quantity   = $.quantity
        unitPrice  = $.unitPrice
    append InvoiceLineItemRemoved
      when removed
        invoiceId  = $.id
        lineNumber = $.lineNumber
```

Full CDL grammar: (link to CDL documentation)

---

### 13. Sub-language pluggability

The Screenplay parser is designed as a registry of named sub-language parsers. When the parser encounters a construct keyword (`projection`, `capture`, or any registered extension), it delegates to the registered sub-parser for that construct's indented body.

This means additional domain-specific languages can be plugged in by:
1. Registering a construct keyword
2. Providing a parser that consumes the indented block
3. Optionally providing Monaco language service extensions for that sub-language (completions, diagnostics)

The Monaco language service implementation must support the same registration pattern so that sub-language syntax highlighting and IntelliSense compose cleanly.

---

### 14. Constraints

Server-side rules enforced in the Chronicle kernel before events are committed. Two built-in types; complex rules delegate to a file.

```
constraint <Name>
  unique <property> on <EventType>         ← unique property constraint

constraint <Name>
  unique event <EventType>                 ← unique event type constraint

constraint <Name>
  file <Path>                              ← custom C# implementation
```

**Examples:**

```
constraint UniqueInvoiceNumber
  unique invoiceNumber on InvoiceRegistered

constraint OneRegistrationPerInvoice
  unique event InvoiceRegistered

constraint InvoiceStatusTransition
  file Constraints/InvoiceStatusTransitionConstraint.cs
```

---

### 15. Reactors

Event reaction rules. Live inside `Automation` slices.

```
reactor <Name>
  on <EventType>
    [file <Path>]
    [csharp
      ```
      <C# returning event side effects>
      ```]
```

**Examples:**

```
reactor NotifyCustomer
  on InvoiceRegistered
    file Reactors/NotifyCustomerReactor.cs

reactor OverdueInvoiceDetector
  on InvoiceStatusChanged
    csharp
      ```
      if (@event.Status != InvoiceStatus.Paid &&
          @event.ChangedAt < DateTimeOffset.UtcNow.AddDays(-30))
      {
          return [new MarkInvoiceOverdue(
              InvoiceId: @event.InvoiceId,
              OverdueAt: DateTimeOffset.UtcNow
          )];
      }
      ```
```

---

### 16. Screens

UI declarations. Live inside `StateView` slices. Support three levels of abstraction.

#### Level 1 — Intent

Declares data and available actions. Studio generates the component.

```
screen <Name>
  data <ReadModel>[[] ] via query <QueryName> [by <param>]
  action <CommandName>
    [navigate to <ScreenName> [by <param>]]
    [label "<text>"]
```

**Example:**

```
screen InvoiceList
  data InvoiceListReadModel[] via query ListInvoices
  action RegisterInvoice
    navigate to RegisterInvoiceScreen
  action CancelInvoice
```

#### Level 2 — Structure

Adds named sections, tables, forms, and summary widgets.

```
screen <Name>
  layout <LayoutName>
    <slot-name>
      section <name>
        data ...
        action ...
        table <ReadModel>
          column <property> [label "<text>"]
          on row-click navigate to <Screen> [by <param>]
        summary <ReadModel>
          field <property> label "<text>"
        title "<text>"
```

**Example:**

```
screen InvoiceDetails
  layout MasterDetail
    sidebar
      data InvoiceDetailsReadModel via query GetInvoice by invoiceId
      section summary
        action CancelInvoice
        action ChangeInvoiceStatus
    main
      section lineItems
        table lineItems
          column lineNumber  label "#"
          column description label "Description"
          column quantity    label "Qty"
          column unitPrice   label "Unit Price"
          on row-click navigate to InvoiceLineDetail by lineNumber
```

#### Level 3 — Layout with inline code

Combines layout templates, structural sections, and inline React/HTML/TypeScript blocks. The surrounding Screenplay context provides the typed data contract; the inline block receives it as `Props`.

```
screen InvoiceDashboard
  layout DashboardLayout
    header
      section title
        data InvoiceSummaryReadModel via query GetInvoiceSummary
        react
          ```
          export default ({ data }: Props) => (
            <header className="dashboard-header">
              <h1>Invoice Dashboard</h1>
              <span className="badge">{data.totalCount} invoices</span>
            </header>
          );
          ```
    left
      section overdue
        data OverdueInvoicesReadModel[] via query GetOverdueInvoices
        table OverdueInvoicesReadModel
          column invoiceNumber label "Invoice #"
          column dueDate       label "Due Date"
          on row-click navigate to InvoiceDetails by invoiceId
```

#### File reference

Full external implementation — Stage uses the file, the Screenplay contract remains visible to Studio.

```
screen RegisterInvoiceScreen
  file Screens/RegisterInvoiceScreen.tsx
```

#### Inline code languages

| Tag | Used for |
|---|---|
| `react` | React/TSX components |
| `typescript` | Plain TypeScript |
| `html` | Static HTML |
| `csharp` | Server-side logic (validation, reactor bodies, produces blocks) |

---

## Full Sample

```
// ============================================================
// Invoicing — complete .play file
// ============================================================

import Customers.CustomerRegistered

concept InvoiceId        : Uuid
concept CustomerId       : Uuid
concept InvoiceNumber    : String
concept CurrencyCode     : String
concept Money            : Decimal
concept Quantity         : Int
concept EmailAddress     : String   @pii
concept PersonName       : String   @pii

concept InvoiceStatus : Enum
  draft
  sent
  paid
  overdue
  cancelled

concept PaymentTerms : Enum
  net30
  net60
  immediate

policy IsAuthenticated
  require authenticated

policy IsAccountant
  require role "Accountant"

policy CanManageInvoice
  require role "InvoiceManager"
    or role "Accountant"

policy IsAdultCustomer
  csharp
    ```
    var dob = context.User.FindFirst("dateOfBirth")?.Value;
    if (dob is null) return PolicyResult.Fail("Date of birth claim missing");
    return DateTime.Parse(dob) <= DateTime.UtcNow.AddYears(-18)
        ? PolicyResult.Success()
        : PolicyResult.Fail("Customer must be 18 or older");
    ```

module Invoicing

  layout MasterDetail
    template
      sidebar
      main

  layout DashboardLayout
    template
      header
      left
      right
      footer

  feature InvoiceManagement

    slice StateChange RegisterInvoice

      command RegisterInvoice
        invoiceId     InvoiceId
        customerId    CustomerId
        invoiceNumber InvoiceNumber
        lines         InvoiceLine[]
        currency      CurrencyCode
        paymentTerms  PaymentTerms
        dueDate       Date
        isProForma    Bool

        authorize CanManageInvoice
                  IsAdultCustomer

        validate
          invoiceNumber not empty                  message "Invoice number is required"
          invoiceNumber matches "^INV-[0-9]{6}$"  message "Invoice number must match INV-000000"
          lines not empty                          message "An invoice must have at least one line"
          lines.quantity all > 0                   message "All line quantities must be positive"
          currency length == 3                     message "Currency must be a 3-letter ISO code"
          dueDate > today                          message "Due date must be in the future"

        validate csharp
          ```
          var total = Lines.Sum(l => l.Quantity * l.UnitPrice);
          if (total > 1_000_000 && PaymentTerms == PaymentTerms.Immediate)
              yield ValidationError("Invoices over 1,000,000 cannot require immediate payment");
          ```

        produces InvoiceRegistered
          invoiceId     = invoiceId
          customerId    = customerId
          invoiceNumber = invoiceNumber
          lines         = lines
          currency      = currency
          paymentTerms  = paymentTerms
          dueDate       = dueDate
          registeredAt  = $context.occurred
          registeredBy  = $context.identity.id
          source        = $env.SERVICE_NAME
          status        = "draft"

        produces when isProForma == true
          ProFormaInvoiceIssued
            invoiceId  = invoiceId
            issuedAt   = $context.occurred

        produces when currency != "NOK"
          ForeignCurrencyInvoiceRegistered
            invoiceId = invoiceId
            currency  = currency

        produces when paymentTerms == "immediate"
          ImmediatePaymentInvoiceRegistered
            invoiceId = invoiceId
            dueDate   = dueDate

        produces when paymentTerms == "net30" or paymentTerms == "net60"
          DeferredPaymentInvoiceRegistered
            invoiceId    = invoiceId
            paymentTerms = paymentTerms
            dueDate      = dueDate

      event InvoiceRegistered
        invoiceId     InvoiceId
        customerId    CustomerId
        invoiceNumber InvoiceNumber
        lines         InvoiceLine[]
        currency      CurrencyCode
        paymentTerms  PaymentTerms
        dueDate       Date
        registeredAt  DateTime
        registeredBy  String
        source        String
        status        InvoiceStatus

      event ProFormaInvoiceIssued
        invoiceId InvoiceId
        issuedAt  DateTime

      event ForeignCurrencyInvoiceRegistered
        invoiceId InvoiceId
        currency  CurrencyCode

      event ImmediatePaymentInvoiceRegistered
        invoiceId InvoiceId
        dueDate   Date

      event DeferredPaymentInvoiceRegistered
        invoiceId    InvoiceId
        paymentTerms PaymentTerms
        dueDate      Date

      constraint UniqueInvoiceNumber
        unique invoiceNumber on InvoiceRegistered

      constraint OneRegistrationPerInvoice
        unique event InvoiceRegistered

    slice StateChange CancelInvoice

      command CancelInvoice
        invoiceId InvoiceId
        reason    String
        refund    Bool

        authorize CanManageInvoice

        validate
          reason not empty  message "A cancellation reason is required"
          reason max 500    message "Reason must be 500 characters or fewer"

        produces InvoiceCancelled
          invoiceId   = invoiceId
          reason      = reason
          cancelledAt = $context.occurred
          cancelledBy = $context.identity.id

        produces when refund == true
          InvoiceRefundRequested
            invoiceId   = invoiceId
            requestedAt = $context.occurred

      event InvoiceCancelled
        invoiceId   InvoiceId
        reason      String
        cancelledAt DateTime
        cancelledBy String

      event InvoiceRefundRequested
        invoiceId   InvoiceId
        requestedAt DateTime

    slice StateChange ChangeInvoiceStatus

      command ChangeInvoiceStatus
        invoiceId InvoiceId
        status    InvoiceStatus
        note      String?

        authorize IsAccountant

        validate
          status not empty  message "Status is required"

        produces when status == "sent"
          InvoiceSent
            invoiceId = invoiceId
            sentAt    = $context.occurred
            note      = note

        produces when status == "paid"
          InvoicePaid
            invoiceId = invoiceId
            paidAt    = $context.occurred
            note      = note

        produces when status == "overdue"
          InvoiceMarkedOverdue
            invoiceId = invoiceId
            overdueAt = $context.occurred

        produces when status != "sent" and status != "paid" and status != "overdue"
          InvoiceStatusChanged
            invoiceId = invoiceId
            status    = status
            changedAt = $context.occurred

      event InvoiceSent
        invoiceId InvoiceId
        sentAt    DateTime
        note      String?

      event InvoicePaid
        invoiceId InvoiceId
        paidAt    DateTime
        note      String?

      event InvoiceMarkedOverdue
        invoiceId InvoiceId
        overdueAt DateTime

      event InvoiceStatusChanged
        invoiceId InvoiceId
        status    InvoiceStatus
        changedAt DateTime

      constraint InvoiceStatusTransition
        file Constraints/InvoiceStatusTransitionConstraint.cs

    slice StateChange ProcessInvoiceBatch

      command ProcessInvoiceBatch
        batchId    Uuid
        invoiceIds InvoiceId[]
        action     String

        authorize IsAccountant

        validate
          invoiceIds not empty  message "Batch must contain at least one invoice"
          action not empty      message "Action is required"

        produces csharp
          ```
          var events = new List<object>();
          events.Add(new InvoiceBatchProcessingStarted(BatchId, InvoiceIds.Count, DateTimeOffset.UtcNow));
          foreach (var id in InvoiceIds)
              events.Add(Action switch {
                  "send"   => new InvoiceSent(id, DateTimeOffset.UtcNow, context.Identity.Id, null),
                  "cancel" => new InvoiceCancelled(id, "Batch", DateTimeOffset.UtcNow, context.Identity.Id),
                  _        => throw new InvalidOperationException($"Unknown action: {Action}")
              });
          events.Add(new InvoiceBatchProcessingCompleted(BatchId, InvoiceIds.Count, DateTimeOffset.UtcNow));
          return events;
          ```

      event InvoiceBatchProcessingStarted
        batchId   Uuid
        count     Int
        startedAt DateTime

      event InvoiceBatchProcessingCompleted
        batchId        Uuid
        processedCount Int
        completedAt    DateTime

    slice Translate LegacyInvoiceSync

      capture LegacyInvoiceCapture
        source api
          api LegacyInvoicingApi
          route /invoices
          poll 5m
        key id
        map
          status = status translate
            "utkast"     => draft
            "sendt"      => sent
            "betalt"     => paid
            "kansellert" => cancelled
        append InvoiceStatusChanged
          when status
            invoiceId = $.id
            status    = $.status
            changedAt = $context.occurred
        children lineItems identified by lineNumber
          append InvoiceLineItemAdded
            when added
              invoiceId  = $.id
              lineNumber = $.lineNumber
              quantity   = $.quantity
              unitPrice  = $.unitPrice
          append InvoiceLineItemRemoved
            when removed
              invoiceId  = $.id
              lineNumber = $.lineNumber

    slice StateView InvoiceList

      query ListInvoices => InvoiceListReadModel[]
        filter status     InvoiceStatus?
        filter customerId CustomerId?
        authorize IsAuthenticated

      projection InvoiceList => InvoiceListReadModel
        from InvoiceRegistered key invoiceId
          customerId    = customerId
          invoiceNumber = invoiceNumber
          currency      = currency
          status        = "draft"
          lineCount     = 0
          dueDate       = dueDate
        from InvoiceSent
          status = "sent"
        from InvoicePaid
          status = "paid"
        from InvoiceMarkedOverdue
          status = "overdue"
        from InvoiceStatusChanged
          status = status
        remove with InvoiceCancelled

      screen InvoiceList
        data InvoiceListReadModel[] via query ListInvoices
        action RegisterInvoice
          navigate to RegisterInvoiceScreen
        action CancelInvoice

    slice StateView InvoiceDetails

      query GetInvoice => InvoiceDetailsReadModel
        by invoiceId InvoiceId
        authorize IsAuthenticated

      projection InvoiceDetails => InvoiceDetailsReadModel
        key invoiceId
        from InvoiceRegistered key invoiceId
          customerId    = customerId
          invoiceNumber = invoiceNumber
          currency      = currency
          paymentTerms  = paymentTerms
          status        = "draft"
          dueDate       = dueDate
          registeredAt  = $eventContext.occurred
        from InvoiceSent
          status = "sent"
          sentAt = sentAt
        from InvoicePaid
          status = "paid"
          paidAt = paidAt
        from InvoiceMarkedOverdue
          status    = "overdue"
          overdueAt = overdueAt
        join customer on customerId
          with CustomerRegistered
            customerName = name
        children lineItems identified by lineNumber
          from InvoiceLineItemAdded key lineNumber
            parent invoiceId
            quantity  = quantity
            unitPrice = unitPrice
          remove with InvoiceLineItemRemoved key lineNumber
            parent invoiceId

      screen InvoiceDetails
        layout MasterDetail
          sidebar
            data InvoiceDetailsReadModel via query GetInvoice by invoiceId
            section summary
              action CancelInvoice
              action ChangeInvoiceStatus
          main
            section lineItems
              table lineItems
                column lineNumber  label "#"
                column quantity    label "Qty"
                column unitPrice   label "Unit Price"
                on row-click navigate to InvoiceLineDetail by lineNumber

    slice StateView InvoiceDashboard

      query GetInvoiceSummary  => InvoiceSummaryReadModel
        authorize IsAccountant

      query GetOverdueInvoices => OverdueInvoicesReadModel[]
        authorize IsAccountant

      projection InvoiceSummary => InvoiceSummaryReadModel
        from InvoiceRegistered
          key "global"
          increment totalCount
          increment draftCount
        from InvoiceSent
          decrement draftCount
          increment sentCount
        from InvoicePaid
          decrement sentCount
          increment paidCount
        from InvoiceMarkedOverdue
          increment overdueCount
        from InvoiceCancelled
          decrement totalCount

      screen InvoiceDashboard
        layout DashboardLayout
          header
            section title
              data InvoiceSummaryReadModel via query GetInvoiceSummary
              react
                ```
                export default ({ data }: Props) => (
                  <header className="dashboard-header">
                    <h1>Invoice Dashboard</h1>
                    <span className="badge">{data.totalCount} invoices</span>
                    {data.overdueCount > 0 && (
                      <span className="badge badge--warning">{data.overdueCount} overdue</span>
                    )}
                  </header>
                );
                ```
          left
            section overdue
              data OverdueInvoicesReadModel[] via query GetOverdueInvoices
              table OverdueInvoicesReadModel
                column invoiceNumber label "Invoice #"
                column dueDate       label "Due Date"
                on row-click navigate to InvoiceDetails by invoiceId
          right
            section summary
              data InvoiceSummaryReadModel via query GetInvoiceSummary
              react
                ```
                import { PieChart, Pie, Cell, Tooltip } from 'recharts';
                const COLORS = ['#94a3b8', '#60a5fa', '#34d399', '#f87171'];
                export default ({ data }: Props) => (
                  <PieChart width={300} height={300}>
                    <Pie data={[
                      { name: 'Draft', value: data.draftCount },
                      { name: 'Sent',  value: data.sentCount },
                      { name: 'Paid',  value: data.paidCount },
                    ]} dataKey="value">
                      {COLORS.map((c, i) => <Cell key={i} fill={c} />)}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                );
                ```
          footer
            section actions
              action RegisterInvoice
                label "New Invoice"
                navigate to RegisterInvoiceScreen

    slice Automation NotifyCustomerOnInvoiceRegistered

      reactor NotifyCustomer
        on InvoiceRegistered
          file Reactors/NotifyCustomerReactor.cs

    slice Automation DetectOverdueInvoices

      reactor OverdueInvoiceDetector
        on InvoiceRegistered
          csharp
            ```
            if (DueDate < DateTimeOffset.UtcNow)
                return [new MarkInvoiceOverdue(InvoiceId, DateTimeOffset.UtcNow)];
            ```
```

---

## EBNF Grammar

```ebnf
(* ============================================================ *)
(* Screenplay DSL — Full EBNF                                    *)
(* ============================================================ *)

Document       = { Import }, { ConceptDecl }, { PolicyDecl }, { Module } ;

(* -------------------------------------------------------------- *)
(* Imports                                                         *)
(* -------------------------------------------------------------- *)

Import         = "import", QualifiedName, NL ;
QualifiedName  = Ident, { ".", Ident } ;

(* -------------------------------------------------------------- *)
(* Concepts                                                        *)
(* -------------------------------------------------------------- *)

ConceptDecl    = "concept", Ident, ":", PrimitiveType, { Attribute }, NL
               | "concept", Ident, ":", "Enum", NL,
                   INDENT, { Ident, NL }, DEDENT ;

PrimitiveType  = "Uuid" | "String" | "Int" | "Decimal" | "Bool"
               | "Date" | "DateTime" ;

Attribute      = "@pii" | "@sensitive" ;

(* -------------------------------------------------------------- *)
(* Policies                                                        *)
(* -------------------------------------------------------------- *)

PolicyDecl     = "policy", Ident, NL,
                 INDENT, PolicyBody, DEDENT ;

PolicyBody     = PolicyExpr
               | InlineBlock ;

PolicyExpr     = "require", PolicyCondition, { ( "or" | "and" ), PolicyCondition } ;

PolicyCondition = "authenticated"
               | "role", StringLiteral
               | "claim", StringLiteral, "matches", ( "subject" | StringLiteral )
               | "(", PolicyCondition, { ( "or" | "and" ), PolicyCondition }, ")" ;

(* -------------------------------------------------------------- *)
(* Module                                                          *)
(* -------------------------------------------------------------- *)

Module         = "module", Ident, NL,
                 INDENT,
                   { LayoutDecl },
                   { Feature },
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Layouts                                                         *)
(* -------------------------------------------------------------- *)

LayoutDecl     = "layout", Ident, NL,
                 INDENT,
                   "template", NL,
                   INDENT, { Ident, NL }, DEDENT,
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Features                                                        *)
(* -------------------------------------------------------------- *)

Feature        = "feature", Ident, NL,
                 INDENT,
                   { Feature },
                   { SliceDecl },
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Slices                                                          *)
(* -------------------------------------------------------------- *)

SliceDecl      = "slice", SliceType, Ident, NL,
                 INDENT, { SliceBody }, DEDENT ;

SliceType      = "StateChange" | "StateView" | "Automation" | "Translate" ;

SliceBody      = EventDecl
               | CommandDecl
               | QueryDecl
               | ProjectionDecl
               | CaptureDecl
               | ReactorDecl
               | ScreenDecl
               | ConstraintDecl ;

(* -------------------------------------------------------------- *)
(* Events                                                          *)
(* -------------------------------------------------------------- *)

EventDecl      = "event", Ident, NL,
                 INDENT, { PropertyLine }, DEDENT ;

PropertyLine   = Ident, TypeRef, NL ;

TypeRef        = Ident, [ "[]" ], [ "?" ] ;

(* -------------------------------------------------------------- *)
(* Commands                                                        *)
(* -------------------------------------------------------------- *)

CommandDecl    = "command", Ident, NL,
                 INDENT,
                   { PropertyLine },
                   [ AuthorizeDecl ],
                   { ValidateDecl },
                   { ProducesDecl },
                 DEDENT ;

AuthorizeDecl  = "authorize", PolicyRef, { ( NL, PolicyRef ) | ( "or", PolicyRef ) }, NL ;

PolicyRef      = Ident ;

ValidateDecl   = "validate", NL,
                   INDENT, { ValidationRule }, DEDENT
               | "validate", "csharp", NL, InlineBlock ;

ValidationRule = Ident, RuleOp, [ "message", StringLiteral ], NL ;

RuleOp         = "not empty"
               | "max", Number
               | "min", Number
               | ">", Value
               | ">=", Value
               | "<", Value
               | "<=", Value
               | "==", Value
               | "length", "==", Number
               | "matches", ( "email" | StringLiteral )
               | "all", ">", Value
               | "all", ">=", Value ;

Value          = Number | StringLiteral | "today" | "true" | "false" ;

(* -------------------------------------------------------------- *)
(* Produces                                                        *)
(* -------------------------------------------------------------- *)

ProducesDecl   = "produces", Ident, NL,
                   [ INDENT, { PropertyMapping }, DEDENT ]
               | "produces", "when", Condition, NL,
                   INDENT, Ident, NL,
                   [ INDENT, { PropertyMapping }, DEDENT ],
                   DEDENT
               | "produces", "csharp", NL, InlineBlock ;

Condition      = ConditionExpr, { ( "and" | "or" ), ConditionExpr } ;

ConditionExpr  = Ident, CompOp, Value
               | Ident, CompOp, Ident
               | "(" Condition ")" ;

CompOp         = "==" | "!=" | ">" | ">=" | "<" | "<=" ;

PropertyMapping = Ident, "=", MappingSource, NL ;

MappingSource  = Ident                         (* command property   *)
               | "$context.occurred"
               | "$context.identity.id"
               | "$env.", Ident
               | StringLiteral
               | Number
               | "true" | "false"
               | Expression ;

Expression     = (* arithmetic / method-call expression — freeform *) ;

(* -------------------------------------------------------------- *)
(* Queries                                                         *)
(* -------------------------------------------------------------- *)

QueryDecl      = "query", Ident, "=>", TypeRef, NL,
                 [ INDENT,
                     [ ByClause ],
                     { FilterClause },
                     [ AuthorizeDecl ],
                   DEDENT ] ;

ByClause       = "by", Ident, TypeRef, NL ;
FilterClause   = "filter", Ident, TypeRef, NL ;

(* -------------------------------------------------------------- *)
(* Projections — PDL sub-language                                  *)
(* -------------------------------------------------------------- *)

ProjectionDecl = "projection", Ident, "=>", Ident, NL,
                 INDENT, PDLBody, DEDENT ;

PDLBody        = (* Projection Declaration Language grammar —
                    see https://cratis.io/chronicle/projections/
                    projection-declaration-language/grammar/ *) ;

(* -------------------------------------------------------------- *)
(* Captures — CDL sub-language                                     *)
(* -------------------------------------------------------------- *)

CaptureDecl    = "capture", Ident, NL,
                 INDENT, CDLBody, DEDENT ;

CDLBody        = (* Change Data Capture Language grammar *) ;

(* -------------------------------------------------------------- *)
(* Sub-language extension point                                    *)
(* -------------------------------------------------------------- *)

(* Any registered keyword not listed above may appear as a
   SliceBody construct. The parser delegates to the registered
   sub-parser for the indented body.                               *)

ExtensionConstruct = Ident, Ident, NL,
                     [ INDENT, { AnyLine }, DEDENT ] ;

(* -------------------------------------------------------------- *)
(* Constraints                                                     *)
(* -------------------------------------------------------------- *)

ConstraintDecl = "constraint", Ident, NL,
                 INDENT, ConstraintBody, DEDENT ;

ConstraintBody = "unique", Ident, "on", Ident, NL   (* unique property  *)
               | "unique", "event", Ident, NL         (* unique event     *)
               | FileDirective ;                       (* custom C#        *)

(* -------------------------------------------------------------- *)
(* Reactors                                                        *)
(* -------------------------------------------------------------- *)

ReactorDecl    = "reactor", Ident, NL,
                 INDENT,
                   "on", Ident, NL,
                   ( FileDirective | InlineBlock ),
                 DEDENT ;

(* -------------------------------------------------------------- *)
(* Screens                                                         *)
(* -------------------------------------------------------------- *)

ScreenDecl     = "screen", Ident, NL,
                 INDENT, ScreenBody, DEDENT ;

ScreenBody     = FileDirective                          (* full external file  *)
               | { ScreenDirective } ;                  (* declarative levels  *)

ScreenDirective = DataDecl
               | ActionDecl
               | SectionDecl
               | LayoutRef
               | InlineBlock ;

DataDecl       = "data", TypeRef, "via", "query", Ident,
                 [ "by", Ident ], NL ;

ActionDecl     = "action", Ident, NL,
                 [ INDENT, { ActionOption }, DEDENT ] ;

ActionOption   = NavigateDecl
               | "label", StringLiteral, NL ;

NavigateDecl   = "navigate", "to", Ident, [ "by", Ident ], NL ;

LayoutRef      = "layout", Ident, NL,
                 INDENT, { SlotDecl }, DEDENT ;

SlotDecl       = Ident, NL,
                 [ INDENT, { ScreenDirective }, DEDENT ] ;

SectionDecl    = "section", Ident, NL,
                 INDENT, { ScreenDirective | WidgetDecl }, DEDENT
               | "title", StringLiteral, NL ;

WidgetDecl     = ( "table" | "summary" ) , ( TypeRef | Ident ), NL,
                 [ INDENT, { WidgetOption }, DEDENT ] ;

WidgetOption   = "column", Ident, [ "label", StringLiteral ], NL
               | "field",  Ident, "label", StringLiteral, NL
               | "on", "row-click", NavigateDecl ;

(* -------------------------------------------------------------- *)
(* Shared                                                          *)
(* -------------------------------------------------------------- *)

FileDirective  = "file", FilePath, NL ;
FilePath       = (* relative path string *) ;

InlineBlock    = LanguageTag, NL, "```", NL, { AnyLine }, "```", NL ;
LanguageTag    = "csharp" | "typescript" | "react" | "html" ;

StringLiteral  = '"', { ? any char except '"' ? }, '"' ;
Number         = [ "-" ], { "0".."9" }, [ ".", { "0".."9" } ] ;
Ident          = Letter, { Letter | Digit | "_" } ;
Letter         = "A".."Z" | "a".."z" ;
Digit          = "0".."9" ;

NL             = ? newline ? ;
INDENT         = ? increase in indentation level ? ;
DEDENT         = ? decrease in indentation level ? ;
AnyLine        = ? any text until newline ? ;
```

---

## Deliverables

### Folder structure

```
Documentation/
  screenplay/
    index.md                  ← Language overview and design principles
    concepts.md               ← Concepts reference
    policies.md               ← Policies reference
    slices.md                 ← Slice types reference
    events.md                 ← Event declarations
    commands.md               ← Commands, validation, produces
    queries.md                ← Queries and authorization
    projections.md            ← PDL embedding
    captures.md               ← CDL embedding
    constraints.md            ← Constraint types
    reactors.md               ← Automation / reactor declarations
    screens.md                ← Screens — all three levels
    sub-languages.md          ← Sub-language pluggability design
    grammar.md                ← Full EBNF

Source/
  Screenplay/
    Monaco/
      screenplay-language/            ← NPM package: Monaco language service
        package.json                  ← name: @cratis/screenplay-language
        tsconfig.json
        src/
          index.ts                    ← Public API: register(monaco)
          language.ts                 ← Language ID, aliases, file extensions (.play)
          tokens.ts                   ← Monarch tokenizer definition
          completions.ts              ← Completion item provider
          hover.ts                    ← Hover provider
          diagnostics.ts              ← Markers / validation provider
          sub-language-registry.ts    ← Registration API for PDL, CDL, extensions
          sub-languages/
            pdl.ts                    ← PDL token rules + completions
            cdl.ts                    ← CDL token rules + completions
          themes/
            screenplay-dark.ts
            screenplay-light.ts

      screenplay-editor/              ← Standalone editor host app
        package.json                  ← Vite + React + TypeScript
        vite.config.ts
        tsconfig.json
        index.html
        src/
          main.tsx
          App.tsx
          components/
            ScreenplayEditor.tsx      ← Monaco editor wrapper
            Toolbar.tsx
          samples/
            invoicing.play            ← Sample loaded on startup
```

---

## Implementation notes

### NPM package (`@cratis/screenplay-language`)

- Use Monaco's `languages.register`, `languages.setMonarchTokensProvider`, `languages.registerCompletionItemProvider`, and `languages.registerHoverProvider`
- The public API is a single `register(monaco)` function — callers pass the Monaco instance
- Sub-language registry: `registerSubLanguage(keyword, { tokens, completions })` — allows PDL and CDL to be registered separately and composed at runtime. Screenplay token rules switch into the sub-language's token rules when inside a `projection` or `capture` block (using Monarch's `@push`/`@pop` state stack)
- Keywords to highlight: `module`, `feature`, `slice`, `concept`, `policy`, `event`, `command`, `query`, `projection`, `capture`, `reactor`, `screen`, `constraint`, `layout`, `produces`, `validate`, `authorize`, `import`
- Slice types: `StateChange`, `StateView`, `Automation`, `Translate`
- Concept attributes: `@pii`, `@sensitive`
- Context vars: `$context.occurred`, `$context.identity.id`, `$env.*`
- Inline code blocks (between triple backticks) should use Monaco's embedded language feature to provide C#/TypeScript/TSX highlighting inside the block

### Editor host (`screenplay-editor`)

- Check `/Volumes/Code/Cratis/Studio` for the Yarn workspace configuration, `.yarnrc.yml`, and how Web projects are structured under `Core/` — mirror that setup
- Yarn (not npm); use workspace protocol for `@cratis/screenplay-language` dependency
- Vite for bundling; `@monaco-editor/react` as the React wrapper
- Load Monaco workers via `vite-plugin-monaco-editor` or equivalent
- On startup: load the `invoicing.play` sample file into the editor
- Toolbar: New, Open (file picker), Save (download), theme toggle (dark/light)
- Editor should fill the viewport with a clean, minimal chrome

### Completions to implement (minimum viable)

| Context | Completions |
|---|---|
| Top level | `import`, `concept`, `policy`, `module` |
| Inside `module` | `layout`, `feature` |
| Inside `feature` | `feature`, `slice StateChange`, `slice StateView`, `slice Automation`, `slice Translate` |
| Inside `slice` | `event`, `command`, `query`, `projection`, `capture`, `reactor`, `screen`, `constraint` |
| Inside `command` | `authorize`, `validate`, `produces` |
| Inside `produces` | `when`, `csharp`, known event names declared in scope |
| Inside `screen` | `data`, `action`, `layout`, `section`, `file`, `react`, `typescript`, `html` |
| Inside `constraint` | `unique`, `file` |
| After `authorize` | All policy names declared in file |
| After `on` (reactor) | All event names declared in file |
| Type positions | All concept names + primitives (`Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`) |

### Hover documentation

Hovering a keyword should show a one-line description of what it does. Hovering a concept name should show its primitive type and attributes. Hovering a policy name should show its require expression.

---

## Acceptance criteria

- [ ] All language reference sections documented in `Documentation/screenplay/`
- [ ] Full EBNF committed to `Documentation/screenplay/grammar.md`
- [ ] `@cratis/screenplay-language` NPM package builds cleanly with `yarn build`
- [ ] Syntax highlighting covers all keywords, slice types, concept attributes, context variables, inline code blocks, and string literals
- [ ] Sub-language registry API documented and PDL/CDL registered as reference implementations
- [ ] Completion provider covers the minimum viable set listed above
- [ ] Hover provider covers keywords and in-scope symbol names
- [ ] `screenplay-editor` app starts with `yarn dev`, loads the sample `.play` file, and renders the editor with correct highlighting
- [ ] Dark and light themes both implemented and toggleable
- [ ] The `invoicing-complete.play` sample included in the editor's `samples/` folder and loads without errors
