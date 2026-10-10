# Invoicing

An invoicing system for a finance department and its customers, written as one Screenplay document that uses
every construct except the [preview constructs](../../.cratis/ai/rules/project/samples.md#preview-constructs).
When you want to know how something is written, find it in the table below and
jump to the slice that uses it.

```text
Invoicing/
  invoicing.play         the whole application - 31 slices in one module
  invoicing.en.strings   English text for every $strings key the document references
  invoicing.nb.strings   the same keys in Norwegian
```

Processing purposes are declared at the top level: Billing covers the module, Bookkeeping is added on Auditing, and PaymentDisputes on CollectionsBoard. Their union is report-only; it does not enforce retention or make a legal-compliance claim. Concept reasons describe the value rather than its lawful basis.

Descriptions on InvoiceId, IsAuthenticated, RegisterInvoiceForm, UniqueInvoiceNumber, InvoiceList and RegisterInvoiceScreen explain values, rules, builders and input/view surfaces. They are report-only metadata, not UI titles, validation or generated code comments.

## Who uses it

| Persona | Holds | Sees the screens of |
| --- | --- | --- |
| `InvoiceDraftCreator` | `IsAuthenticated`, `CanManageInvoice` | a focused authenticated draft-creation specification witness; broader screen roles remain below |
| `InvoiceManager` | `IsAuthenticated`, `IsPerson`, `IsInvoicingStaff`, `CanManageInvoice` | StartInvoiceDraft, RegisterInvoice, CancelInvoice, TagInvoice, UpdateBillingContact, InvoiceList, InvoiceDetails |
| `Accountant` | the above, plus `IsAccountant`, `IsFinanceDepartment` | everything the invoice manager sees, plus ChangeInvoiceStatus, ProcessInvoiceBatch, ArchiveOldInvoices, InvoiceLineReport, InvoiceDashboard, ApplyDiscount, RecordPayment, InvoiceBalances, InvoiceAging, CollectionsBoard, ExchangeRates |
| `FinanceController` | `IsAuthenticated`, `IsPerson`, `IsInvoicingStaff`, `IsFinanceDepartment`, `CanWriteOff` | WriteOffInvoice |
| `Customer` | `IsAuthenticated`, `IsPerson`, `IsCustomer`, `OwnsInvoice`, `IsAdultCustomer`, `IsWithinCreditLimit` | MyInvoices, RequestPaymentPlan, CreditStatus |
| `Auditor` | `IsAuthenticated`, `IsPerson`, `IsAuditor` | CancelledInvoices, SystemActivity |

The event model board draws a slice's screens in the row of every persona whose policies satisfy what gates the
slice: the module's `authorize`, each enclosing feature's, and the slice's command or query.

## The slices

| Feature | State change | State view | Automation | Translate |
| --- | --- | --- | --- | --- |
| InvoiceManagement | StartInvoiceDraft, RegisterInvoice, CancelInvoice, TagInvoice, ChangeInvoiceStatus, ProcessInvoiceBatch, ArchiveOldInvoices, UpdateBillingContact | InvoiceList, InvoiceDetails, InvoiceLineReport, InvoiceDashboard | | |
| InvoiceManagement › Adjustments | ApplyDiscount, WriteOffInvoice | | | |
| Payments | RecordPayment | InvoiceBalances, InvoiceAging, CollectionsBoard, ExchangeRates | ReconcilePayments, ChaseOverdueInvoices | PaymentProviderSync |
| CustomerPortal | RequestPaymentPlan | MyInvoices, CreditStatus | | |
| Auditing | | CancelledInvoices, SystemActivity | | |
| Integrations (› Notifications) | | | NotifyCustomerOnInvoiceRegistered, DetectOverdueInvoices, SyncBillingDirectory | LegacyInvoiceSync, InvoiceSentPublication |

Every slice has Given/When/Then specifications, and every state change and state view slice has a screen.
Click sections and row-click links on the lists, details, dashboards and customer portal open the registration, draft, status,
batch, archive, cancellation, tagging, billing-contact, payment and payment-plan command screens before input
is supplied. Each command screen issues its own action; action navigation such as returning to `MyInvoices`
happens only after success, never to the command's own input screen. Row clicks carry `invoiceId` to status,
tagging, billing-contact, cancellation, payment and payment-plan screens. Payments are opened from the
balance or due/overdue invoice rows; the payment form loads the balance by that identity.
`ExchangeRates`, `CreditStatus` and the overdue list on `InvoiceDashboard` are views no event builds: their
queries' performers read the central bank feed, a credit bureau and stored invoices.

`RequestPaymentPlan` maps `requestedBy` from `$identity.userName`, the caller's user name. This is equivalent to `$context.identity.userName`; both spellings remain supported.

`InvoiceDraftStarted.customerId` uses the trailing event-property `subject` role to name the customer instead of the invoice stream. This is report-only lineage metadata, with no executable model or provider output yet.

## Where each construct is used

| Construct | Where |
| --- | --- |
| `domain` with a qualified name, `import` | top of the file |
| Fenced Markdown `documentation` on module, feature, slice, command, read model and reaction; specification `description` | Invoicing, InvoiceManagement, StartInvoiceDraft, InvoiceListReadModel, PaymentReconciler, StartingAnInvoiceDraft |
| `concept` of every primitive, `Enum`, `pii`/`secret` with reasons, secret scope and personal-data special/criminal qualifiers, `file`, concept `validate` with `matches email`, `rule` with a `file` and an inline body, `severity` | Concepts |
| `type` with `description`, `file`, optional and collection properties | Composite value types |
| `policy` with `require` (`authenticated`, `role`, `claim … matches` a literal, `subject` or `$context` path, `not`/`and`/`or`/parentheses, continuation lines), inline ```` ```csharp ```` and `file` bodies | Authorization |
| Typed specification `parameter`, named `case` and `case.<parameter>` values | StartingDraftsForCustomers |
| `persona` with single-line and fenced descriptions; `given caller as` with a deterministic authenticated witness | Authorization; StartingAnInvoiceDraft |
| `authentication` with named providers | Authorization |
| `trigger` with `description`, `file`, typed and untyped values | Triggers |
| `theme`, two `layout`s with `arrangement flow`, `when width compact`, `gap`; two `ui profile`s | Look and shell |
| `behavior` with `description`, typed and untyped `parameter`s, `order`, `confirm` and nested continuations | Behaviors |
| module `description` (fenced), `authorize`, `on event`, `contribute to` | `module Invoicing` |
| `screen template` with `fits slot`, `flow` with `grid`/`span`, `freeform` with `variant`/`place … hidden`, a `contributes` slot; `dialog template` with `on leave` | `module Invoicing` |
| `form` with `populate via query`, `populate from item`, `field … label/from/compose using`, `on submit navigate`, `on change` | `module Invoicing`; `TagInvoiceForm` reuses the selected row |
| feature `authorize`, `depends on`, `uses` with arguments, `on <ApplicationTrigger>`, nested features, feature `contribute to` | InvoiceManagement, Adjustments, Payments, Integrations |
| slice `description`, `file` | RegisterInvoice |
| `generated identifier`, `generated`, record `returns` with inferred/explicit types, `when … for`, generation fixtures and `then returns`; scalar `returns` and `then returns` | StartInvoiceDraft; CancelInvoice |
| inline `produces event`, typed mappings, event `description` and Markdown `documentation`, implicit identifier destination | TagInvoice |
| command `description`, `identifier`, multi-line `authorize`, every validation rule, named-rule `implementation` hints with the existing file link, `severity`, `require`, inline `validate` block, `$strings` messages | RegisterInvoice, CancelInvoice, TagInvoice, ProcessInvoiceBatch, ApplyDiscount |
| `produces` with `for`, `tag`, every mapping source (`$identity.*`, `$context.*`, `$env`, `$strings`, literals, Booleans, lists, expressions); `produces when` with `and`/`or`/parentheses, `contains`, `starts with` | RegisterInvoice, ApplyDiscount, RecordPayment, NotifyCustomerOnInvoiceRegistered |
| `reads … as … by` and `require` over read state | RecordPayment |
| `handler` inline and `implementation` with a hint and existing `file`; `concurrency` | ProcessInvoiceBatch, ArchiveOldInvoices, RegisterInvoice |
| `event` with `generation 2`, `file`, `tag` (name, string, `$env`, `$context`), an `@tag` escaped property | RegisterInvoice, TagInvoice |
| `constraint` with `unique … on`, a composite `unique a, b on`, `unique event`, `released by`, `ignore casing`, `message` | RegisterInvoice, RecordPayment |
| `readmodel` with `description` and `file`; `query` with `observable`, `by`/`filter … from`, `scoped to identity`/`global`, `performer` in ```` ```sql ````, ```` ```csharp ```` and `file` | InvoiceList, InvoiceLineReport, ExchangeRates, MyInvoices, CreditStatus |
| `projection` with `file`, `sequence`, `every`/`exclude children`, `all`, `from A, B`, inline and block keys, a composite `key`, `parent`, `join` with `automap`/`no automap`, `children`, `nested`, `clear with`, `remove with`, `remove via join`, `set … to`, `clear`, `increment`/`decrement`/`count`/`add`/`subtract`, templates, `$eventSourceId`, `$eventContext`, `$causedBy` | InvoiceList, InvoiceDetails, InvoiceLineReport, InvoiceDashboard, InvoiceBalances, SystemActivity |
| projection `variant`s with `enters on` and a shared handler | CollectionsBoard |
| `reducer` with inline and `file` rules | InvoiceAging |
| `screen` at all three levels: intent (`data`, `action`, `label`, `navigate to`), structure (`template`, slots, `section`, `title`, `table`, `summary`, `on row-click`) and inline ```` ```react ````/```` ```html ````/```` ```typescript ````; a `file` screen | every state change and state view slice |
| guarded interaction: block-form `when item.status …` and `otherwise` selecting an action list | InvoiceList — double click opens editing only for a draft; InvoiceDetails — click opens cancellation only for a draft |
| guarded screen action: label header, `when item.status … execute`, explicit `with … from` and `otherwise hidden` | CancelInvoiceScreen — cancellation is offered only for a draft |
| interactions: `on load`, `enter`, `click`, `double click`, `select`, `submit`, `change`, `leave`, `interval`, `event … where`; `execute`, `navigate to`/`back`, `open dialog … with … from`, `close dialog`, `refresh`, `set`, `notify`, `confirm`, `raise`; `on success`/`failure`/`result` | InvoiceList, InvoiceLineDetail, ChangeInvoiceStatus, CollectionsBoard, behaviors |
| `reaction` with `when` an event, `Startup`, a declared trigger; `every`; `at`, `at … on Monday`, `at … on day 1`; trigger values, `reads`, `produces`, `invokes`, `where`, inline and `file` bodies | Automation and Translate slices |
| `direction outbound`, a `public event`, and an event-target `projection` that folds private events into it | InvoiceSentPublication; the inbound half (`direction inbound`, an event `from "origin"`, `source events`) is in the Commerce sample's ReceiveCarrierDispatches, because Invoicing's `all` projection would consume any foreign event |
| `capture` with `source`, `key`, `map`/`translate`/`split`/templates, `append` with `tag` and every `when` form, `children`, `nested` | LegacyInvoiceSync |
| `specification` with `file`, `given caller`, `given clock`, `given <Event> for`, `given readmodel`, `given capture`, `when <Command> for`, `when append`, `when clock`, `when trigger`, `when capture`, `when query`, `then events in any order`, `then <Event> for`, `then readmodel exactly`, `then no readmodel`, `then query` with `arguments`/`result`, `then result exactly`, `then no result`, `then error` with and without a message, `then denied` | throughout |
| `example` with structured values, indented and inline overrides | RegisterInvoice: `AcmeInvoice` supplies repeated command inputs without hiding the caller or outcome |
| `seed` - two blocks | bottom of the file |

`IsAuthenticated` requires only authentication. The module's `IsPerson` policy also excludes the `Service` role and an `actorKind` claim matching `service`. `RejectingAServiceRegisteringAnInvoice` demonstrates a denial even when that service holds the `InvoiceManager` role.

## Specifying what is not a command

Automation and translate slices are driven by time, by application triggers and by capture source records, and the specifications say so:

- `IssuingTheWeeklyDigestOnMondayMorning` lets the clock reach Monday 07:30 with `when clock`.
- `SynchronizingWhenSomeoneAsks` fires the `BillingDirectorySyncRequested` trigger with `when trigger`.
- `SeeingAnInvoicePaidInTheLegacySystem` hands the legacy capture the record as it was (`given capture`) and as it is now (`when capture`).
- `given clock` fixes when a scenario happens, so `CancellingAnInvoiceWithARefund` can assert the `cancelledAt` mapped from `$context.occurred`.
- `ExchangeRates`, `CreditStatus` and the auditor denial perform their query with `when query` and assert `then result`, `then no result` or `then denied`.

`RegisterInvoice` retains the existing `BeUnusedInvoiceNumber` file selection inside an `implementation` wrapper:

```screenplay
invoiceNumber rule BeUnusedInvoiceNumber message "Invoice number is already in use"
  implementation
    hint "Preserve the existing invoice-number acceptance criteria"
    file Validations/BeUnusedInvoiceNumber.cs
```

This excerpt belongs inside the command's `validate` block. Its hint records guidance without changing the predicate contract or claiming that the selected code ran. The referenced implementation files are not included in this syntax showcase.

`Payments` declares `depends on InvoiceManagement`: it reads invoice state and reacts to invoice events from that feature. The declaration documents that boundary for authoring checks; it does not change execution or slice order.

`StartInvoiceDraft` creates a draft with a generated invoice identity and receipt and returns both only on acceptance. Its specification supplies deterministic UUID fixtures separately from `customerId`, then asserts the fact and response. `CancelInvoice` returns the cancelled invoice identity as a scalar. These constructs select ESM v7; generated values are not request/form inputs and give no retry or idempotency guarantee. Response-name binding in UI continuations remains downstream work.

The cancellation row-click in `InvoiceDetails` opens `CancelInvoiceScreen` with `invoiceId` before input. That screen's guarded cancellation button reads the invoice's status and binds its identity explicitly, without navigating to its own input screen after execution. The guard offers an action; it does not authorize it or satisfy `CancelInvoice`'s validation. Remaining inputs come from the renderer. Guarded actions are syntax and renderer contracts, not executable-model assertions; downstream renderer support is required.

## Parsed is not executable

This document is a showcase of the language, not of what runs today. The
[preview constructs](../../.cratis/ai/rules/project/samples.md#preview-constructs) stay in dedicated fixtures
because they refuse the whole model before binding. Other syntax kinds covered by focused, tested fixtures
are pinned in `when_holding_invoicing_to_the_language`. This document compiles with no errors or warnings, but much
of it is outside what the executable semantic model admits: imported events, reducers, performers, list
queries, handlers, command `reads`, `pii` concepts and code policies among them. Clocks, triggers, captures and
reactions are admitted as ESM v6 by
[decision 0022](../../decisions/0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md), but
because the document as a whole does not bind, the reference specification runner can not execute most of
these specifications here. Direct-producing reactions with `reads` also refuse binding rather than
silently discarding unprotected decisions. Focused executable clock, reaction, trigger and capture
vectors live in `ReactionsCorpus.V6`; full-sample syntax compilation is not reference-execution admission.
For a model whose
core specifications do run, see [Library](../Library).

## Verify

```bash
screenplay --warnaserror Samples/Invoicing
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Invoicing
```

`dotnet test` runs the same check for every sample in `for_Samples/when_compiling_the_samples`.
