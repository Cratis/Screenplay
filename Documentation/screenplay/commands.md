# Commands

Commands are input definitions — imperative intents. A command declares its properties, its authorization, its validation rules, and what events it produces.

## Syntax

```screenplay
command <Name>
  [description "<text>"]

  <property> <Type>[?] [identifier]
  ...

  [reads <ReadModel> [by <property>]]   ← state the command decides against
  ...

  [authorize <PolicyName> [<PolicyName>]*]

  [validate
    <rule> message "<message>"
    require <condition>              ← a rule about the command as a whole
      [message "<message>"]
    ...]

  [validate csharp
    ```
    <C# yielding the message of every broken rule>
    ```]

  [produces ...]                  ← declarative — repeatable

  [handler                        ← imperative fallback — instead of produces
    file <Path>
    | csharp
        ```
        <C# returning the events to append>
        ```]

  [concurrency                    ← optional concurrency scope
    [eventSource]
    [sourceType <Name>]
    [streamType <Name>]
    [streamId <Name>]
    [events <EventType>[, <EventType>]*]]
```

## Description

An optional `description` is the first body line of a command — a human-readable summary consumers such as Prologue surface when presenting the model. At most one per command. Use the quoted form for a single line:

```screenplay
command RegisterInvoice
  description "Registers a new invoice with its lines and payment terms"
```

When one line is not enough, use a fenced block — the same ``` convention as inline code blocks. The fenced text is kept verbatim:

````screenplay
command RegisterInvoice
  description
    ```
    Registers a new invoice with its lines and payment terms.
    The invoice starts out as a draft.
    ```
````

Descriptions work the same on modules, features, slices, and personas — see [Descriptions](slices.md#descriptions).

## The identifier

Every command that changes state changes the state of *something*. `identifier` marks which property names it:

```screenplay
command RegisterInvoice
  invoiceId     InvoiceId identifier
  invoiceNumber InvoiceNumber
```

A runtime such as Stage honors that property as the event source id for everything the command appends. Leave it out and the runtime generates a fresh `Uuid` instead — which is right for a command that creates something whose identity the caller does not supply:

```screenplay
command ArchiveOldInvoices
  olderThan Date
```

**At most one property per command** may be the identifier; a second one is a compile error, because there is no sensible way to choose between them. The modifier belongs to commands only — an event never carries its own event source id (it is implicit in the event context), so `identifier` on an event property is an error too.

## What the command reads

A command that changes state usually has to consult state first — whether the month is already started, which phase an engagement is in, who the consultant on a scope is. `reads` declares that dependency:

```screenplay
command StartMonth
  engagementId EngagementId identifier
  year         TimesheetYear
  month        TimesheetMonth
  reads EngagementScope by engagementId

  produces TimesheetStarted
    engagementId = engagementId
    consultantId = EngagementScope.consultantId
```

This is the read-model-to-command arrow of Event Modeling — one of the four the method is built on, and the only one a document could not draw. Without it, a command that decides against state shows its inputs and its events but not what it consulted in between, and the mapping fed from that state has nowhere to come from.

- `<ReadModel>` names a read model some [projection](projections/index.md) produces. Reading something no projection produces is a warning — the document says it depends on state nothing in it explains.
- `by <property>` names the command property the read model is looked up by, and must be one of the command's own properties. Leave it out for a read model that is not looked up by a key — a single view the whole application shares rather than one instance per identifier.
- A command may read more than one read model, but only once each; reading the same one twice says nothing the first declaration did not.

With the read model in scope, its properties are addressable for the rest of the command body — in a produces mapping, as above, and in the validation rules below.

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
| `<= <value>` | `discountPct <= 100` |
| `== <value>` | `currency == "NOK"` |
| `!= <value>` | `status != "draft"` |
| `length == <n>` | `currency length == 3` |
| `matches <regex>` | `email matches email` |
| `matches "<pattern>"` | `invoiceNumber matches "^INV-[0-9]{6}$"` |
| `all > <value>` (on collection) | `lines.quantity all > 0` |
| `all >= <value>` (on collection) | `lines.unitPrice all >= 0` |
| `rule <Name>` | `orgNumber rule BeAValidOrganizationNumber` |

Every rule carries a `message` shown when it fails:

```screenplay
validate
  invoiceNumber not empty                  message "Invoice number is required"
  invoiceNumber matches "^INV-[0-9]{6}$"  message "Invoice number must match INV-000000"
  dueDate > today                          message "Due date must be in the future"
```

### Rules about the command as a whole

Every rule above says something about one property. The rules that actually guard a domain usually do not — "the month is already started", "the engagement must be in its contract phase" — they are about the command as a whole, and most often about state it [reads](#what-the-command-reads). `require` states one:

```screenplay
command StartMonth
  engagementId EngagementId identifier
  reads EngagementScope by engagementId

  validate
    require EngagementScope.isStarted == false
      message "The month is already started"
    require EngagementScope.phase == "Contract"
      message "The engagement must be in its contract phase"
```

The condition is the language's [one condition grammar](grammar.md) — the same one a [policy](policies.md) `require` carries, so `and` and `or` mean the same thing here, `and` binds tighter than `or`, and parentheses group:

```screenplay
validate
  require EngagementScope.isStarted == false and EngagementScope.phase == "Contract"
    message "The month cannot be started yet"
```

The message goes in the body rather than on the end of the line. A condition is as long as the rule it states, and a message pushed out past it is the part nobody reads.

An operand is either a property of the command or a path into state the command declares it reads. Anything else is a warning — a requirement a reader cannot resolve says less than it appears to. A rule whose logic is not a comparison at all still belongs in a named `rule` or an inline block, as below; `require` is for the rules that *can* be stated.

#### A rule that only applies sometimes

A rule that holds only under a condition — "an extension has to move the end date out, but a renewal need not" — is a requirement like any other. State it as an implication rather than reaching for a second construct:

```screenplay
command ExtendEngagement
  engagementId EngagementId identifier
  isExtension  Bool
  endDate      Date
  newEndDate   Date

  validate
    require isExtension == false or newEndDate > endDate
      message "An extension must move the end date out"
```

`or` is what makes the rule conditional: when `isExtension` is false the requirement is already satisfied and the comparison never decides anything. This is why the language has no separate `when` clause on a rule — the condition grammar already says it, and a second way to write the same rule would be a second thing to keep consistent.

### Rules whose logic is not expressible

Not every rule is a comparison. A predicate — "is this a valid organization number", "is this still available" — has logic that lives in code, and the declarative shapes above cannot express it.

Leaving it out is the worst option, because it makes the document lie: a property with two declarative rules and three predicates reads as a property with two rules, and a reader cannot tell "nothing further constrains this" from "the rest could not be written down". Name the rule instead:

```screenplay
validate
  orgNumber not empty                       message "Organization number is required"
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
  orgNumber rule BeUnique
```

The name is a reference into the implementation, not a declared construct — nothing resolves it, and the compiler does not check that anything called `BeAValidOrganizationNumber` exists. It is there so the document is honest about how constrained a value is, and so a reader has something more useful than "a rule was omitted".

### Giving a named rule a body

A bare `rule <Name>` states that a constraint exists without saying what it computes — sometimes that is genuinely all a document can say, because the logic lives somewhere the compiler cannot see. When the logic *can* live in the document, give the rule a body the same way every other construct that needs exact details does: a `file` reference or an inline code block, indented under the rule:

```screenplay
validate
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
    file Validations/BeAValidOrganizationNumber.cs
```

````screenplay
validate
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
    csharp
      ```
      string orgNumber = context.Value;
      return orgNumber.Length == 9 && orgNumber.All(char.IsDigit);
      ```
````

Both forms are optional and mutually exclusive with each other — a rule with neither stays the bare, undetermined-location form from above. The `file`/`csharp` shapes and their compiled representation (`FileReferenceSyntax` / `CodeBlockSyntax`) are exactly the ones used by [`handler`](#the-handler-block) and [reactors](reactors.md), so a reader who knows one already knows the other. The same body is available on a concept's own `rule <Name>` (see [Concepts](concepts.md#validation)) — the implementation travels with the value everywhere it appears.

Cross-field or complex rules drop into C#. The block yields the message of every rule the command breaks, and yields nothing when the command is valid:

````screenplay
validate csharp
  ```
  if (context.Artifact.paymentTerms == "immediate" && context.Artifact.total > 1_000_000)
  {
      yield return "Invoices over 1,000,000 cannot require immediate payment";
  }
  ```
````

Both `validate` and `validate csharp` can coexist on the same command.

### What a rule can see

Inside a `rule` body and inside a `validate csharp` block, `context` is the `RuleContext`:

| Member | Value |
| --- | --- |
| `context.Artifact` | The whole thing under validation — the command here, the concept's own value on a concept rule. |
| `context.Value` | The value the rule is declared on. Equal to `Artifact` for a `validate csharp` block. |
| `context.Property` | Where that value sits in the artifact — `orgNumber` above, empty for a whole-command block. |
| `context.Tenant` | The tenant the command is executing for. |
| `context.CausedBy` | The identity that caused the command, so a rule such as "you may not approve your own request" is expressible. |
| `context.Occurred` | When the command was received. |

A rule can see **who** is calling but not **what they are allowed to do** — there are no roles and no claims in a `RuleContext`. A rule that inspects those is an authorization decision wearing a validation hat, and belongs in a [policy](policies.md). See [Contexts](context.md) for all four shapes.

## Authorization

```screenplay
authorize <requirement>
```

Two policies written next to each other mean both must pass, and `and` says the same thing out loud. `or` makes them alternatives. A requirement may continue on the next line at deeper indentation:

```screenplay
authorize CanManageInvoice IsAdultCustomer

authorize IsAccountant
          or IsCustomerSelf
```

This is the language's [one condition grammar](grammar.md) again, over policies instead of comparisons — so `and` binds tighter than `or`, and parentheses group:

```screenplay
authorize IsAccountant or IsFinance and OwnsInvoice     ← IsAccountant, or both of the others
authorize (IsAccountant or IsFinance) and OwnsInvoice   ← one of the first two, and OwnsInvoice
```

Those two admit different callers, and the parentheses are the only thing that distinguishes them. Printing writes them back wherever the grouping is not the one precedence gives, so a document always says which one it means.

Policies are declared at the top of the file — see [Policies](policies.md).

## The `produces` block

Declares what events a command emits. Supports single, multiple, and conditional forms. For a fully imperative implementation, use a [handler](#the-handler-block) instead.

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

### Tags

`tag` lines before the mappings attach [tags](events.md#tags) to the event appended by this specific production:

```screenplay
produces InvoiceRegistered
  tag audit
  invoiceId = invoiceId
```

### Mapping sources

| Source | Syntax | Description |
| --- | --- | --- |
| Command property | `= <propertyName>` | Direct copy from command |
| Command context | `= $context.occurred` | Timestamp of the command |
| Tenant | `= $context.tenant` | The tenant the command executes for |
| Calling identity | `= $context.causedBy.subject` | Subject of the identity that caused the command |
| Causation | `= $context.causation.type` | What caused the command — a command, reactor, schedule |
| Caller identity | `= $context.identity.id` | The caller's identifier from the auth token |
| Caller claim | `= $context.identity.claims.<name>` | The value of a claim the caller carries |
| Environment | `= $env.<VAR_NAME>` | Environment variable |
| String constant | `= "value"` | Literal string |
| Numeric constant | `= 0` | Literal number |
| Expression | `= lines.sum(l => l.quantity * l.unitPrice)` | Computed value |

Every `$context.` path names a member of the `CommandContext` an inline handler compiles against — see [Contexts](context.md).

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

## The `handler` block

Declares a fully imperative implementation of the command, in C#, as either an inline block or a reference to an external file. Use it when the declarative `produces` forms cannot express the logic — batch processing, imperative branching, or anything that needs more than property mappings and conditions.

Delegating to a file:

```screenplay
handler
  file Commands/ProcessInvoiceBatchHandler.cs
```

Inline C#:

````screenplay
handler
  csharp
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

A command uses either `produces` blocks or a `handler` — not both. Keep handler logic small; anything substantial belongs in a `file` reference where it can be tested on its own.

Inside either form, `context` is the [`CommandContext`](context.md) — the command itself, the tenant, the caller, the identity recorded as having caused it, the causation, and when the command was received. An inline block and a `file` reference compile against exactly the same type.

## Concurrency

An optional `concurrency` block declares the concurrency scope enforced when the command's events are appended — mirroring Chronicle's `ConcurrencyScope`. When two commands race, the append fails for the loser instead of silently letting both win.

```screenplay
command RegisterInvoice
  ...
  concurrency
    eventSource
    sourceType Account
    streamType Onboarding
    streamId Monthly
    events InvoiceRegistered, InvoiceCancelled
```

| Dimension | Meaning |
| --- | --- |
| `eventSource` | Scope the check to the command's event source id |
| `sourceType <Name>` | Scope the check to an event source type |
| `streamType <Name>` | Scope the check to an event stream type |
| `streamId <Name>` | Scope the check to an event stream id |
| `events <EventType>[, ...]` | Scope the check to the listed event types |

All dimensions are optional and each appears at most once, but the block must declare at least one — an empty `concurrency` block is a compile error. A command without a `concurrency` block appends with no concurrency check.
