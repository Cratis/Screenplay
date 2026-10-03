# Invoicing

An invoicing system for a finance department and its customers, written as one Screenplay document that uses
every construct the language has. When you want to know how something is written, find it in the table below and
jump to the slice that uses it.

```text
Invoicing/
  invoicing.play         the whole application - 30 slices in one module
  invoicing.en.strings   English text for every $strings key the document references
  invoicing.nb.strings   the same keys in Norwegian
```

## Who uses it

| Persona | Holds | Sees the screens of |
| --- | --- | --- |
| `InvoiceManager` | `IsAuthenticated`, `IsInvoicingStaff`, `CanManageInvoice` | RegisterInvoice, CancelInvoice, TagInvoice, UpdateBillingContact, InvoiceList, InvoiceDetails |
| `Accountant` | the above, plus `IsAccountant`, `IsFinanceDepartment` | everything the invoice manager sees, plus ChangeInvoiceStatus, ProcessInvoiceBatch, ArchiveOldInvoices, InvoiceLineReport, InvoiceDashboard, ApplyDiscount, RecordPayment, InvoiceBalances, InvoiceAging, CollectionsBoard, ExchangeRates |
| `FinanceController` | `IsAuthenticated`, `IsInvoicingStaff`, `IsFinanceDepartment`, `CanWriteOff` | WriteOffInvoice |
| `Customer` | `IsAuthenticated`, `IsCustomer`, `OwnsInvoice`, `IsAdultCustomer`, `IsWithinCreditLimit` | MyInvoices, RequestPaymentPlan, CreditStatus |
| `Auditor` | `IsAuthenticated`, `IsAuditor` | CancelledInvoices, SystemActivity |

The event model board draws a slice's screens in the row of every persona whose policies satisfy what gates the
slice: the module's `authorize`, each enclosing feature's, and the slice's command or query.

## The slices

| Feature | State change | State view | Automation | Translate |
| --- | --- | --- | --- | --- |
| InvoiceManagement | RegisterInvoice, CancelInvoice, TagInvoice, ChangeInvoiceStatus, ProcessInvoiceBatch, ArchiveOldInvoices, UpdateBillingContact | InvoiceList, InvoiceDetails, InvoiceLineReport, InvoiceDashboard | | |
| InvoiceManagement › Adjustments | ApplyDiscount, WriteOffInvoice | | | |
| Payments | RecordPayment | InvoiceBalances, InvoiceAging, CollectionsBoard, ExchangeRates | ReconcilePayments, ChaseOverdueInvoices | PaymentProviderSync |
| CustomerPortal | RequestPaymentPlan | MyInvoices, CreditStatus | | |
| Auditing | | CancelledInvoices, SystemActivity | | |
| Integrations (› Notifications) | | | NotifyCustomerOnInvoiceRegistered, DetectOverdueInvoices, SyncBillingDirectory | LegacyInvoiceSync |

Every slice has Given/When/Then specifications, and every state change and state view slice has a screen.
`ExchangeRates`, `CreditStatus` and the overdue list on `InvoiceDashboard` are views no event builds: their
queries' performers read the central bank feed, a credit bureau and stored invoices.

## Where each construct is used

| Construct | Where |
| --- | --- |
| `domain` with a qualified name, `import` | top of the file |
| `concept` of every primitive, `Enum`, `@pii`/`@sensitive` with reasons, `file`, concept `validate` with `matches email`, `rule` with a `file` and an inline body, `severity` | Concepts |
| `type` with `description`, `file`, optional and collection properties | Composite value types |
| `policy` with `require` (`authenticated`, `role`, `claim … matches` a literal, `subject` or `$context` path, `and`/`or`/parentheses, continuation lines), inline ```` ```csharp ```` and `file` bodies | Authorization |
| `persona` with single-line and fenced descriptions | Authorization |
| `authentication` with named providers | Authorization |
| `trigger` with `description`, `file`, typed and untyped values | Triggers |
| `theme`, two `layout`s with `arrangement flow`, `when width compact`, `gap`; two `ui profile`s | Look and shell |
| `behavior` with `description`, typed and untyped `parameter`s, `order`, `confirm` and nested continuations | Behaviors |
| module `description` (fenced), `authorize`, `on event`, `contribute to` | `module Invoicing` |
| `screen template` with `fits slot`, `flow` with `grid`/`span`, `freeform` with `variant`/`place … hidden`, a `contributes` slot; `dialog template` with `on leave` | `module Invoicing` |
| `form` with `populate via query`, `populate from item`, `field … label/from/compose using`, `on submit navigate`, `on change` | `module Invoicing` |
| feature `authorize`, `uses` with arguments, `on <ApplicationTrigger>`, nested features, feature `contribute to` | InvoiceManagement, Adjustments, Payments, Integrations |
| slice `description`, `file` | RegisterInvoice |
| inline `produces event`, typed mappings, event `description` and Markdown `documentation`, implicit identifier destination | TagInvoice |
| command `description`, `identifier`, multi-line `authorize`, every validation rule, `severity`, `require`, inline `validate` block, `$strings` messages | RegisterInvoice, CancelInvoice, TagInvoice, ProcessInvoiceBatch, ApplyDiscount |
| `produces` with `for`, `tag`, every mapping source (`$context.*`, `$env`, `$strings`, literals, Booleans, lists, expressions); `produces when` with `and`/`or`/parentheses, `contains`, `starts with` | RegisterInvoice, ApplyDiscount, RecordPayment, NotifyCustomerOnInvoiceRegistered |
| `reads … as … by` and `require` over read state | RecordPayment |
| `handler` inline and `implementation` with a hint and existing `file`; `concurrency` | ProcessInvoiceBatch, ArchiveOldInvoices, RegisterInvoice |
| `event` with `generation 2`, `file`, `tag` (name, string, `$env`, `$context`), an `@tag` escaped property | RegisterInvoice, TagInvoice |
| `constraint` with `unique … on`, a composite `unique a, b on`, `unique event`, `released by`, `ignore casing`, `message` | RegisterInvoice, RecordPayment |
| `readmodel` with `description` and `file`; `query` with `observable`, `by`/`filter … from`, `scoped to identity`/`global`, `performer` in ```` ```sql ````, ```` ```csharp ```` and `file` | InvoiceList, InvoiceLineReport, ExchangeRates, MyInvoices, CreditStatus |
| `projection` with `file`, `sequence`, `every`/`exclude children`, `all`, `from A, B`, inline and block keys, a composite `key`, `parent`, `join` with `automap`/`no automap`, `children`, `nested`, `clear with`, `remove with`, `remove via join`, `set … to`, `clear`, `increment`/`decrement`/`count`/`add`/`subtract`, templates, `$eventSourceId`, `$eventContext`, `$causedBy` | InvoiceList, InvoiceDetails, InvoiceLineReport, InvoiceDashboard, InvoiceBalances, SystemActivity |
| projection `variant`s with `enters on` and a shared handler | CollectionsBoard |
| `reducer` with inline and `file` rules | InvoiceAging |
| `screen` at all three levels: intent (`data`, `action`, `label`, `navigate to`), structure (`template`, slots, `section`, `title`, `table`, `summary`, `on row-click`) and inline ```` ```react ````/```` ```html ````/```` ```typescript ````; a `file` screen | every state change and state view slice |
| interactions: `on load`, `enter`, `click`, `double click`, `select`, `submit`, `change`, `leave`, `interval`, `event … where`; `execute`, `navigate to`/`back`, `open dialog … with … from`, `close dialog`, `refresh`, `set`, `notify`, `confirm`, `raise`; `on success`/`failure`/`result` | InvoiceList, InvoiceLineDetail, ChangeInvoiceStatus, CollectionsBoard, behaviors |
| `reaction` with `when` an event, `Startup`, a declared trigger; `every`; `at`, `at … on Monday`, `at … on day 1`; trigger values, `reads`, `produces`, `invokes`, `where`, inline and `file` bodies | Automation and Translate slices |
| `capture` with `source`, `key`, `map`/`translate`/`split`/templates, `append` with `tag` and every `when` form, `children`, `nested` | LegacyInvoiceSync |
| `specification` with `file`, `given caller`, `given clock`, `given <Event> for`, `given readmodel`, `given capture`, `when <Command> for`, `when append`, `when clock`, `when trigger`, `when capture`, `when query`, `then events in any order`, `then <Event> for`, `then readmodel exactly`, `then no readmodel`, `then query` with `arguments`/`result`, `then result exactly`, `then no result`, `then error` with and without a message, `then denied` | throughout |
| `seed` - two blocks | bottom of the file |

## Specifying what is not a command

Automation and translate slices are driven by time, by application triggers and by capture source records, and the specifications say so:

- `IssuingTheWeeklyDigestOnMondayMorning` lets the clock reach Monday 07:30 with `when clock`.
- `SynchronizingWhenSomeoneAsks` fires the `BillingDirectorySyncRequested` trigger with `when trigger`.
- `SeeingAnInvoicePaidInTheLegacySystem` hands the legacy capture the record as it was (`given capture`) and as it is now (`when capture`).
- `given clock` fixes when a scenario happens, so `CancellingAnInvoiceWithARefund` can assert the `cancelledAt` mapped from `$context.occurred`.
- `ExchangeRates`, `CreditStatus` and the auditor denial perform their query with `when query` and assert `then result`, `then no result` or `then denied`.

## Parsed is not executable

This document is a showcase of the language, not of what runs today. It compiles with no diagnostics, but much
of it is outside what the executable semantic model admits: automation and translate slices, captures,
reactions, clocks and triggers in specifications, reducers, performers, list queries, handlers, `reads`, `@pii`
concepts and code policies among them. Admitting clocks, triggers, captures and reactions is proposed as ESM v6 in
[decision 0022](../../decisions/0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md).
The reference specification runner can therefore not execute most of these specifications. For a model whose
core specifications do run, see [Library](../Library).

## Verify

```bash
screenplay --warnaserror Samples/Invoicing
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Invoicing
```

`dotnet test` runs the same check for every sample in `for_Samples/when_compiling_the_samples`.
