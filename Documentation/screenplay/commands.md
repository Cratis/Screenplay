# Commands

Commands are input definitions — imperative intents. A command declares its properties, its authorization, its validation rules, and what events it produces.

## Syntax

````screenplay
command <Name>
  [description "<text>"]

  <property> <Type> [optional] [generated] [identifier]
  ...

  [reads <ReadModel> [as <alias>] [by <property>]]   ← state the command decides against
  ...

  [authorize <PolicyName> [<PolicyName>]*]

  [validate
    <rule> [severity information|warning|error] [message "<message>"]
    require <condition>              ← a rule about the command as a whole
      [severity information|warning|error]
      [message "<message>"|$strings.<key>]
    ...]

  [validate <inline csharp block yielding messages for broken rules>]

  [stream <EventSource>.<Stream>   ← syntax-only authored classification
    [streamId = <value>]]

  [produces ...]                  ← declarative — repeatable

  [handler                        ← imperative fallback — instead of produces
    file <Path>
    | <inline csharp block returning events to append>]

  [concurrency                    ← optional concurrency scope
    [eventSource]
    [sourceType <Name>]
    [streamType <Name>]
    [streamId <Name>]
    [events <EventType>[, <EventType>]*]]
````

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
    ```text
    Registers a new invoice with its lines and payment terms.
    The invoice starts out as a draft.
    ```
````

Commands also accept one nonempty fenced `markdown` `documentation` block for reasoning and assumptions. It is report-only (`PLAY0270`), with no effect on executable bytes. See [Descriptions and documentation](slices.md#descriptions-and-documentation).

Command descriptions use `text` fences, not `markdown`; Markdown description fences are available only on events. Descriptions work the same on modules, features, slices, and personas — see [Descriptions](slices.md#descriptions).

## Declare an event inline

Use `produces event <Name>` to declare a new event where the command produces it. Each payload property declares its type and mapping together:

```screenplay
command Rename
  projectId Uuid identifier
  name String
  produces event Renamed
    description "The project acquired another name"
    tag audit
    name String = name
```

Here the omitted `for` means `for projectId`; the identifier is **not** copied into the payload. The declaration belongs to the slice, so extracting it into a standalone `event Renamed` plus `produces Renamed` with explicit `for projectId` preserves its identity and canonical ESM bytes. Declarations remain order-independent. A plain `produces Renamed` still references a declared event rather than creating one.

Each inline payload property is declared once; repeating its name reports `PLAY0168`. Inline events are generation 1 only, local to the command, and cannot declare `generation` or `origin`. Reactions cannot declare inline events. Names must not collide with another standalone, imported, or inline event. `tag` inside this block is an **event-type** tag, unlike a tag on plain `produces`. [Event metadata](events.md#authoring-metadata) covers descriptions, documentation, and rename-only identity pins.

If any production targets a source other than the command identifier, **every production must state `for` explicitly** (`PLAY0470`). The same rule applies when an inline production and a plain production both omit `for`: their defaults differ. The diagnostic names the production that needs a destination; adding an inline event never silently retargets a plain sibling. The compiler compares authored paths, not runtime values. Commands without a generated identifier still cannot bind a destination other than their identifier. In ESM v7 a command with a generated identifier may explicitly route a production to another required scalar command property; its response may still return the generated identifier. This does not admit named sources or streams. An inline omission without a required scalar identifier cannot bind. `PLAY0470` currently has no verified workspace/MCP repair: these invalid mixed-destination models do not bind, so the before/after ESM routing and version checks cannot prove an edit safe. State the destinations through coordinated typed edits; do not assume an omitted plain production meant the identifier.

Plain `produces <Name>` retains its legacy omission behavior; it does not acquire the inline default. Copying an identifier into a same-source payload reports `PLAY0469`: warning for inline declarations, information for plain productions with explicit `for`. Review the persisted contract before removing a payload field. The inline-only typed repair removes the property and mapping together, retires the property's semantic address, and is labeled “changes the event contract”. It is excluded from fix-all. The narrow repair refuses other consumers of that event, opaque implementation impact, routing changes and comment loss; standalone/plain contracts receive guidance only, not an automatic shape change.

The unescaped directives `namespace`, `sequence`, `correlation`, `causation`, `causedBy`, and `occurred` are reserved system-assigned metadata in production bodies. Escape a genuine payload field, for example `@sequence String = name`; `occurred at` is not supported yet.

## Command stream routing (syntax-only)

Use `stream Source.Stream` to select a declared [event source and stream](event-sources.md). Map a scalar keyed stream with nested `streamId = <value>`. For a composite stream, nest a bare `streamId` header and one `<part> = <source>` mapping for every declared part, exactly once. Mapping order is free; the printer retains it. Each source is a nominally compatible nonoptional command path or scalar literal. Literals are checked like any stream id: text must be non-empty, well-formed and NFC, and an integer must lie within ±(2^53−1) in the default Double numeric mode. The command's identifier and each event's destination must have the source's identifier type, otherwise `PLAY0504` is an error. Scalar and composite mapping forms cannot substitute for one another. This route never supplies the event's `for` destination and does not change plain-production allocation or inline defaults. Handler commands may author routing without statically declared events; `handler` with `produces` is still prohibited.

Both viable stream/property interpretations remain blocking `PLAY0505` candidates, never a guessed route. Source/stream declarations and routed commands are not admitted by any supported executable model (ESM) version yet (`PLAY0268`). Per-production overrides, reaction/reducer filters and new concurrency flags are not supported. The board shows only the authored route and readable key expression in existing command details.

## Operations and external systems (syntax-only)

A command can describe external effects through inline `produces operation <Name>` declarations or plain references to standalone operations. [Operations and external systems](operations.md) covers typed inputs, `uses`, execution/compensation intent and manual promotion. Event and operation productions stay in one ordered sequence, but operations do not participate in event destinations or payload identity diagnostics. **Operations are not admitted by any supported executable model (ESM) version yet**; binding reports `PLAY0268` without an executable model.

## Generated values and responses

Generated command properties, responses, generated fixtures and return expectations select **ESM v7**. Commands generate values after authorization and validation, then use complete values in productions and responses. A response exists only on acceptance; rejection, denial or an unsupported outcome has no response. A later reaction or scenario-query failure retains already accepted facts but clears the response. Syntax validation alone still does not prove execution or downstream rendering.

```screenplay
command RegisterProject
  projectId ProjectId generated identifier
  receiptId ReceiptId generated
  name ProjectName
  returns
    projectId = projectId
    receiptId ReceiptId = receiptId
```

This fragment assumes `ProjectId` and `ReceiptId` are concepts backed by `Uuid`, and `ProjectName` is a concept backed by `String`. The [complete example](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/generated-responses.play) includes their declarations and passing specifications asserting both emitted events and responses. [Invoicing](https://github.com/Cratis/Screenplay/blob/main/Samples/Invoicing/invoicing.play) shows scalar and record responses in its wider language showcase.

- `generated` is command-only and requires a required, scalar concept backed by `Uuid`; bare `Uuid`, optional values and collections are invalid. Generated values are not request inputs, form fields, invocation arguments or ordinary specification inputs.
- Modifier order is `Type optional generated identifier`. This order does not permit an optional generated value or optional identifier. `generated String` still declares a property named `generated`.
- `returns <property>` declares one scalar response. Bare `returns` opens an unnamed record, even when it has just one field. Declare at most one unconditional response, as a command sibling, not inside a production.
- Each record field is `<name> [<Type>] = <property>`. Its type is inferred from a direct command property, or explicitly annotated with exactly the same type identity and optionality. Fields are unique and stay in authored order. Arithmetic, read aliases, collections and whole read-model responses are not supported.
- A two-token `returns name` is a response when `name` identifies another property of **this command**, regardless of declaration order; otherwise it remains a property named `returns` whose type is `name`. This is a local property lookup, not a type-inventory or capitalization rule. Use `returns @name` to force a source reference, including an unknown source that needs a diagnostic; use `@returns Type` to force a property declaration. Returning a property named `returns` uses `returns @returns`.

Authorization policies (including inherited/composed policies and the implicit subject of a generated identifier), property rules and requirements cannot reference generated values: binding reports `PLAY0273`, because those values do not exist before generation. A generated property's concept must declare no validation rules; v7 refuses concepts with declarative, named or code rules with `PLAY0268`, rather than dropping them. Opaque pre-generation contexts expose input properties only and keep their existing `Unsupported` execution behavior.

A generated identifier supplies an inline production's implicit destination. Plain `produces` without `for` keeps its separate legacy allocation channel; a generation fixture never satisfies that allocation. A response-only command records no facts. No response differs from a scalar response whose optional source is `Null`. Record fields preserve authored order and have external contract names, not independent semantic identities.

Responses also parse beside a handler, but handlers, streams, operations and exact numeric mode remain unadmitted independently. The compiler does not emit a renderer's `<Command>Response` type. Form `on submit` and interaction `on success` response-name binding remains separate downstream work; ESM v7 is not end-to-end navigation. Generated values provide no idempotency, retry or deduplication guarantee.

The board excludes generated properties from the request schema and shows generated/response details in the existing command description. It creates no response event or response identity. See [specification fixtures and return assertions](specifications.md#generated-fixtures-and-return-expectations).

## The identifier

Every command that changes state changes the state of *something*. `identifier` marks which property names it:

```screenplay
command RegisterInvoice
  invoiceId     InvoiceId identifier
  invoiceNumber InvoiceNumber
```

The identifier names the command's event source; a `produces` that states [`for <identifier>`](#where-an-event-lands) appends to it. The event source id is never event payload. Leave it out and the code Stage renders for Arc lets Arc allocate a fresh `Uuid` instead — which is right for a command that creates something whose identity the caller does not supply. The executable model does not allocate one: it needs an allocated identity from the execution request, or an explicit `for`, and reports `IdentityAllocation` otherwise ([see below](#where-an-event-lands)):

```screenplay
command ArchiveOldInvoices
  olderThan Date
```

The trailing `subject` role belongs only to [event properties](events.md#data-subject), including inline events before `=`. Commands and response fields refuse it (decision 0008); it is report-only metadata and does not promise provider output.

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
    for engagementId
    consultantId = EngagementScope.consultantId
```

This is the read-model-to-command arrow of Event Modeling — one of the four the method is built on, and the only one a document could not draw. Without it, a command that decides against state shows its inputs and its events but not what it consulted in between, and the mapping fed from that state has nowhere to come from.

- `<ReadModel>` names a read model some [projection](projections/index.md) produces. Reading something no projection produces is a warning — the document says it depends on state nothing in it explains.
- A `reads` declaration is one line and takes no indented children. A child line is an error and is not interpreted as a command property.
- `by <property>` names the command property the read model is looked up by, and must be one of the command's own properties. Leave it out for a read model that is not looked up by a key — a single view the whole application shares rather than one instance per identifier. The name starts with a lower-case letter or underscore, followed by letters, digits or underscores.
- `as <alias>` distinguishes instances of the same view. If a command reads a view more than once, **every** instance needs an alias. Aliases start with a lower-case letter or underscore, followed by letters, digits or underscores; `as`, `by` and `reads` are reserved. Aliases must be unique in the command and must not match any of its property names. A single read may also be named by an alias.

For example, a transfer can name both accounts without conflating their state:

```screenplay
command TransferFunds
  sourceId Uuid
  destinationId Uuid
  reads Account as source by sourceId
  reads Account as destination by destinationId
  validate
    require source.balance > 0
      message "Source account must have funds"
```

The view name still qualifies a `require` path when only one instance of that view is read. An alias qualifies the path when present; with repeated reads, use the alias instead of the ambiguous view name. With the read model in scope, its properties are also addressable in produces mappings, as above.

The executable semantic model does not yet bind command `reads` or read-model paths in requirements ([#129](https://github.com/Cratis/Screenplay/issues/129)).

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
| `matches email` | `email matches email` |
| `matches "<regex>"` | `invoiceNumber matches "^INV-[0-9]{6}$"` |
| `all > <value>` (on collection) | `lines.quantity all > 0` |
| `all >= <value>` (on collection) | `lines.unitPrice all >= 0` |
| `rule <Name>` | `orgNumber rule BeAValidOrganizationNumber` |

`max` and `min` take their meaning from the property's type: on text they bound its length (`reason max 500` — at most 500 characters), and on a number they bound its value (`quantity min 1`). There is one rule for each, not a separate length and value form.

`matches "<regex>"` uses ECMAScript regular expression syntax. Like JavaScript `RegExp.test`, it succeeds if **any part** of the text matches; use `^` and `$` to require a whole-value match. Patterns are checked at binding, and an execution timeout rejects rather than accepting the value. It applies only to a single text value (not an enum or collection). `matches email` expands to `^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$`: exactly one `@`, a non-empty local part without whitespace or `@`, and a domain with at least two non-empty dot-separated parts without whitespace or `@`. This is a conservative pattern, **not** RFC 5322 email validation. Other bare names have no definition and are rejected; quote a pattern to use a custom regular expression.

A rule may have a `message` shown when it fails. Add `severity information`, `severity warning` or `severity error` before the optional end-of-line `message`. The default is `error`; the printer omits it. Message remains last so its quoted text or `$strings` key is unambiguous:

```screenplay
validate
  invoiceNumber not empty                  message "Invoice number is required"
  invoiceNumber matches "^INV-[0-9]{6}$"  severity warning message "Invoice number must match INV-000000"
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

The message and optional severity go in the body rather than on the end of the line. A condition is as long as the rule it states, and metadata pushed out past it is the part nobody reads. Use a quoted literal or an unquoted `$strings.<key>` for the message. Write `severity warning` or `severity information` as a sibling of `message` (in either order); omitting severity means `error`:

```screenplay
command ConfirmOrder
  total Decimal
  validate
    require total > 0
      severity warning
      message $strings.orders.totalMustBePositive
```

Every failed rule and requirement **rejects** the command, even at warning or information severity. A rejection carries each failed rule's message and severity for UI presentation; severity is not a pass/fail threshold.

An operand is either a property of the command or a path into state the command declares it reads (qualified by its alias, or by its view name if unambiguous). Anything else is a warning — a requirement a reader cannot resolve says less than it appears to. A rule whose logic is not a comparison at all still belongs in a named `rule` or an inline block, as below; `require` is for the rules that *can* be stated.

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
    ```csharp
      string orgNumber = context.Value;
      return orgNumber.Length == 9 && orgNumber.All(char.IsDigit);
      ```
````

Both forms are optional and mutually exclusive with each other — a rule with neither stays the bare, undetermined-location form from above. The `file`/fenced-code shapes and their compiled representation (`FileReferenceSyntax` / `CodeBlockSyntax`) are exactly the ones used by [`handler`](#the-handler-block) and [reactions](reactions.md), so a reader who knows one already knows the other. The same body is available on a concept's own `rule <Name>` (see [Concepts](concepts.md#validation)) — the implementation travels with the value everywhere it appears.

Cross-field or complex rules drop into C#. The block yields the message of every rule the command breaks, and yields nothing when the command is valid:

````screenplay
validate
  ```csharp
  if (context.Artifact.PaymentTerms == "immediate" && context.Artifact.Total > 1_000_000)
  {
      yield return "Invoices over 1,000,000 cannot require immediate payment";
  }
  ```
````

Declarative `validate` and fenced `validate` can coexist on the same command.

### Named-rule implementation intent (commands only)

Add guidance without changing an existing command predicate's source:

```screenplay
command Submit
  label String
  validate
    label rule CheckLabel severity warning message "Invalid label"
      implementation
        hint "Preserve the existing acceptance criteria"
        file Rules/CheckLabel.cs
```

This is a command fragment; the file link selects ordinary team-owned predicate source. A wrapper accepts ordered nonblank quoted `hint` lines and at most one `file` or registered-language tagged fence. Direct forms remain valid. The rule owns its File/Code; metadata owns only hints. Do not mix direct and wrapped sources or repeat the wrapper.

An empty or hints-only wrapper is pending intent: it is valid authoring syntax but executable binding fails with `PLAY0268`. Attached wrappers bind to the existing `RulePredicate` contract, exactly like direct source. Adding, editing or reordering hints does not change canonical ESM bytes, requirement identity, context/result versions, capability or source content hash. Source coordinates move with the text. Reached opaque predicates remain unsupported by the reference runner, never a passing test or confirmation.

Concept predicates, builtin property rules and whole-command `require`/`validate` bodies do not accept this wrapper. Handler and operation-phase wrappers have their own [support boundaries](context.md#implementation-wrapper-support). Realization, committed locks, drift checks, confirmation and AI actions remain deferred under [#307](https://github.com/Cratis/Screenplay/issues/307); builds never invoke AI.

### What a rule can see

Inside a `rule` body and inside a `validate` block with a ` ```csharp ` fence, `context` is the `RuleContext`:

| Member | Value |
| --- | --- |
| `context.Artifact` | The whole thing under validation — the command here, the concept's own value on a concept rule. |
| `context.Value` | The value the rule is declared on. Equal to `Artifact` for a fenced `validate` block. |
| `context.Property` | Where that value sits in the artifact — `orgNumber` above, empty for a whole-command block. |
| `context.Tenant` | The tenant the command is executing for. |
| `context.CausedBy` | The identity that caused the command, so a rule such as "you may not approve your own request" is expressible. |
| `context.Occurred` | When the command was received. |

A rule can see **who** is calling but not **what they are allowed to do** — there are no roles and no claims in a `RuleContext`. A rule that inspects those is an authorization decision wearing a validation hat, and belongs in a [policy](policies.md). See [Contexts](context.md) for all four shapes.

### What the executable model admits

Every rule above parses, prints and round-trips. The executable semantic model (ESM) — what the [reference execution](specifications.md#reference-execution) runs and what Stage renders — admits the rules that have one portable meaning in v1, and binds code validation as opaque implementation attachments in v3. It reports blocking diagnostic `PLAY0268` for rules it still cannot represent, with a message that says why.

| Rule | Admitted on | Operand |
| --- | --- | --- |
| `not empty` | text, or a collection | none |
| `max`, `min` | text (bounds its length) or a whole or decimal number (bounds its value) | a non-negative whole number on text; a number of the property's type otherwise |
| `>`, `>=`, `<`, `<=` | a whole or decimal number | a number of the property's type |
| `==`, `!=` | text, an enum member, a whole or decimal number, or a boolean | a literal of the property's type, or a bare member name of an enum concept |
| `length ==` | text that is not an enum | a non-negative whole number |
| `all >`, `all >=` | a collection of whole or decimal numbers | a number of the element type |

```screenplay
command RegisterInvoice
  invoiceId InvoiceId identifier
  currency String
  amount Decimal
  lineAmounts Decimal[]
  status InvoiceStatus

  validate
    currency length == 3        message "A currency code has three letters"
    amount > 0                  message "An invoice for nothing is not an invoice"
    lineAmounts all >= 0        message "No line can be negative"
    status != cancelled         message "A cancelled invoice cannot be registered"
```

The meaning is fixed so every target agrees:

- An absent value of an optional property satisfies every rule except `not empty` — presence is what `not empty` states. An empty collection satisfies `all >` and `all >=`.
- Text length counts UTF-16 code units, the length .NET and JavaScript both report.
- An operand is a literal. A property reference is not admitted, and an operand that does not fit the property's type — `quantity min 1.5` on an `Int`, `status == "pending"` on an enum without that member — is a binding error (`PLAY0273`) on the rule.
- A failed rule rejects the command at every severity, with its `message` (or a generated description) and its severity.

Still rejected, and why:

| Rule | Why |
| --- | --- |
| any comparison on `Date` or `DateTime`, and `today` | ESM v1 has no runtime date value — a date is text in a fixed format — so it has nothing to compare against. |
| a named `matches` pattern other than `email` | Only `email` has a portable definition; other names are rejected (`PLAY0366`). Invalid quoted ECMAScript patterns are rejected (`PLAY0367`). |
| `require` over a read-model path | A consistent decision snapshot of declared reads is not yet available ([#129](https://github.com/Cratis/Screenplay/issues/129)). |
| a bare `rule <Name>` | Its logic lives outside the document, so it has no portable meaning. |
| a rule on a nested path such as `lines.quantity` | ESM v1 validates command properties; put the rule on the nested value's [concept](concepts.md#validation) instead. |

A bodied `rule <Name>` on a command property or concept value carries its name, property (or concept value), message, severity and stable implementation requirement id in ESM v3. Fenced `validate` blocks on commands and concepts also bind: both yield zero or more rejection messages, with the concept's own value guarded by a concept block. Each yielded message rejects at error severity. A rule predicate receives the guarded value (with the command as its artifact for a command rule) and returns accept or reject. The context follows the documented `RuleContext` above; Screenplay neither executes the C# nor enforces a provider's API allowlist. Requirements use context/result contract version 1 and capability `pure`. Files need host-supplied contents to acquire a content hash; the path is never a code revision. The reference executor reports `SemanticUnsupported` naming the opaque rule for any command carrying it, including concept values nested in a composite or collection. It cannot establish a portable ordering with other validation rules, so it never treats an earlier declarative rejection as proof that the predicate was run. Other commands and read-only specifications remain executable.

Command `require` conditions over command properties and constants are admitted in ESM v1: equality on scalar text, enumeration, number or Boolean; ordering on numbers only. Both property operands must be command properties with compatible types. Requirements run after property validation and before production; a false requirement rejects with its message (or a default message). Concept `require` is not admitted: declare a concept validation rule instead.

## Authorization

```screenplay
authorize <requirement>
```

Two policies written next to each other mean both must pass, and `and` says the same thing out loud. `or` makes them alternatives. Repeated `authorize` lines on a command or query combine with AND in authored order; printing writes them as one `authorize A and B` line. A requirement may continue on the next line at deeper indentation:

```screenplay
authorize CanManageInvoice IsAdultCustomer

authorize IsAccountant
          or IsCustomerSelf
```

This is the language's [one condition grammar](grammar.md) again, over policies instead of comparisons — so `and` binds tighter than `or`, and parentheses group:

```screenplay
authorize IsAccountant or IsFinance and OwnsInvoice     // IsAccountant, or both of the others
authorize (IsAccountant or IsFinance) and OwnsInvoice   // one of the first two, and OwnsInvoice
```

Those two admit different callers, and the parentheses are the only thing that distinguishes them. Printing writes them back wherever the grouping is not the one precedence gives, so a document always says which one it means.

Policies are declared at the top of the file — see [Policies](policies.md). Declarative policies bind to ESM v1 and are evaluated before validation using an explicitly supplied caller; a failed gate returns the typed `Unauthorized` rejection without producing events. Module and feature gates are composed with the command's gate by AND. Inline `csharp` and file policies bind as opaque ESM v3 predicates: the reference evaluator reports `SemanticUnsupported` if evaluation reaches one, and a target provider must supply its implementation.

## The `produces` block

Declares what events a command emits. Supports single, multiple, and conditional forms. For a fully imperative implementation, use a [handler](#the-handler-block) instead.

### Single event with property mapping

```screenplay
produces InvoiceRegistered
  for invoiceId                          // event source, never payload
  invoiceNumber = invoiceNumber          // from command property
  registeredAt  = $context.occurred      // event occurrence time
  registeredBy  = $context.identity.id  // caller identity
  source        = $env.SERVICE_NAME      // environment variable
  status        = "draft"                // string constant
  lineCount     = 0                      // numeric constant
```

### Tags

`tag` lines before the mappings attach [tags](events.md#tags) to the event appended by this specific production. ESM carries literal tags as ordered append metadata, not event payload; computed `$context` tag values are not yet admitted:

```screenplay
produces InvoiceRegistered
  tag audit
  for invoiceId
  invoiceNumber = invoiceNumber
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

Every `$context.` path names a member of the `CommandContext` an inline handler compiles against — see [Contexts](context.md). The ESM v2 `produces` subset is narrower: occurrence time and the three audit identity fields (`identity.id`/`causedBy.subject`, `name`, `userName`). Other entries above remain syntax-only for portable produces mappings and report `PLAY0268`. Explicit `for <command-identifier>` binds a typed state-change destination separately from event properties. When the event does not duplicate that identifier as a payload property, it selects ESM v2; historical v1 models that copy it into both places retain their canonical v1 bytes. Produced v2 facts carry the identity in event context even when the payload has no ID.

### Where an event lands

A plain `produces <Event>` without `for` does not say where the event lands, and it does not mean the command's [identifier](#the-identifier). What happens next depends on which runtime handles the command:

- **Executable model** (reference execution and Stage's semantic host). A plain `produces` is appended to an identity the execution request allocates for the command. When none is supplied, the command is reported as unsupported with `IdentityAllocation`, so name the destination with `for` or supply an allocated identity.
- **Rendered Arc commands** (code generated by Stage's default renderer). The renderer does not read `for`: the command's identifier is used as the event source for every append, whatever `for` says, and a command with no identifier leaves Arc to allocate the id. This is tracked in [Cratis/Stage#187](https://github.com/Cratis/Stage/issues/187); until it is fixed, treat `for` as binding only in the executable model.

A plain omission with an available identifier reports information diagnostic
[`PLAY0478`](diagnostics.md#production-destination-advice-and-repairs). Its typed
[MCP repair](mcp/authoring-tools.md#fix-a-diagnostic) inserts `for <identifier>` after
review; it does not apply automatically. Accept it when the identifier is the
intended destination, not when you deliberately want an allocated identity.

The rest of this section describes the executable model. In ESM v2, a plain `produces` in a command where another production states `for` uses that destination. State `for` on every production that belongs to the command's event source; the event source id is never payload. An explicit `for` can name the command's required scalar identifier on an indented line:

```screenplay
command Activate
  requestId  Uuid identifier
  contractId Uuid

  produces RequestActivated
    for requestId

  produces ContractPolicyActivated
    for requestId
    contractId = contractId
```

Both facts address the command's `requestId` event source; the contract ID remains an event payload property. The executable model cannot bind `for contractId` here: explicit destinations must resolve to the command's required scalar identifier. Use an imperative handler for fan-out to other event sources.

`for` is an indented line rather than an argument on the header, so the target sits beside the mappings that fill the event instead of out past the end of the line. Only one `for` per `produces`: an event is appended to one event source.

### Multiple unconditional events

Repeat `produces` for each event; all are emitted. This excerpt illustrates the syntax, not event naming or a recommended model. `InvoiceRunningTotalUpdated` is a generic, redundant event: normally the projection derives the total from `InvoiceLineItemAdded`. A separate adjustment event is warranted only for an independent business decision, named for that decision rather than for updating a total:

```screenplay
produces InvoiceLineItemAdded
  for invoiceId
  addedAt    = $context.occurred

produces InvoiceRunningTotalUpdated
  for invoiceId
  adjustment = lines.sum(l => l.quantity * l.unitPrice * (1 - l.discountPct / 100))
```

### Conditional produces

`produces when <condition>` emits the indented event only when the condition holds. In ESM v1, comparisons use declared command properties and constants with `==`, `!=`, `>`, `>=`, `<`, `<=`, combined with `and`/`or` (`and` binds tighter; parentheses group). Ordering is numeric; equality admits scalar text, enumeration, number and Boolean. `$env` conditions remain syntax-only because environment values vary across realizations. the supported scalar `$context` mappings select v2 ([#226](https://github.com/Cratis/Screenplay/issues/226)), and paths into reads wait on [#129](https://github.com/Cratis/Screenplay/issues/129):

```screenplay
produces when isProForma == true
  ProFormaInvoiceIssued
    for invoiceId
    issuedAt   = $context.occurred

produces when paymentTerms == "net30" or paymentTerms == "net60"
  DeferredPaymentInvoiceRegistered
    for invoiceId
    paymentTerms = paymentTerms

produces when $env.WELCOME_EMAILS_ENABLED == "true"
  CustomerWelcomeEmailRequested
    for invoiceId
    customerId  = customerId
```

Multiple `produces when` blocks form mutually exclusive or overlapping branches — each condition is evaluated independently. When all conditions are false, execution accepts the command and appends no facts. ESM v1 also rejects arithmetic in mappings: Chronicle's projection language has no binary operators (Decision 0001).

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
  ```csharp
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

### Implementation intent (handlers only)

You can keep implementation guidance beside the handler's existing attachment:

```screenplay
handler
  implementation
    hint "Archive only invoices dated before the requested cutoff"
    file Handlers/ArchiveOldInvoicesHandler.cs
```

This is the handler from [Invoicing](https://github.com/Cratis/Screenplay/blob/main/Samples/Invoicing/invoicing.play). The wrapper also accepts an existing tagged fence instead of `file`. Omit the payload to record **pending** intent; bare `implementation` is valid too. Hints are ordered, nonblank quoted strings, with the usual string escaping. Their decoded text is retained without trimming. A handler allows one wrapper and at most one payload. Unknown children, duplicate wrappers, multiple payloads and mixed direct/wrapped sources are errors.

| Owner | `implementation` wrapper |
| --- | --- |
| Command handler | Supported; direct file/fence forms remain supported |
| Query performer, validation rule, reducer rule, policy, reaction trigger | Deferred; existing direct forms only |
| Operation execute/compensate phases | New syntax-only authoring wrapper; not admitted by any supported executable model (ESM) version yet |
| Provisioning | Deferred |

`implementation` and `hint` are contextual, not globally reserved property names. Intent authoring does not execute, confirm or regenerate code. Command handlers still fail executable admission with `PLAY0268`, including when attached. There is no lock, drift checker, confirmation or AI action in this slice. See [AST authoring](ast-authoring.md#handler-intent-edits) and [MCP inventory](mcp/reference.md#handler-intent-inventory).

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

All dimensions are optional and each appears at most once, but the block must declare at least one — an empty `concurrency` block is a compile error. A command without a `concurrency` block does not append unchecked: Chronicle's default optimistic concurrency strategy applies to every append, narrowed by the append's routing metadata. It skips only the first append into a scope, unless `CheckFirstAppendIntoAScope` is turned on or the append uses `ExpectingNoMatchingEvent()`. The executable model does not bind the `concurrency` block yet. See [Chronicle concurrency](https://github.com/Cratis/Chronicle/blob/main/Documentation/events/concurrency.mdx).
