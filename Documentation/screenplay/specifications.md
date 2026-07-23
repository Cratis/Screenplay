# Specifications

Specifications express Given/When/Then test scenarios directly against a slice's own command and events — executable documentation for the behavior a slice implements. A `specification` block lives inside a `slice`, alongside its `command`, `event`, `projection` and other constructs, and is compiled by the Screenplay compiler like every other sub-language.

## Syntax

```screenplay
specification <Name>
  given <EventType>
    <property> = <value>
  given readmodel <ReadModelType>
    <property> = <value>
  when <CommandType>
    <property> = <value>
  then <EventType>
    <property> = <value>
  then readmodel <ReadModelType>
    <property> = <value>
  then error "<message>"
```

- `given <EventType>` — zero or more. Establishes prior state by replaying events onto the slice's event source before the command runs.
- `given readmodel <ReadModelType>` — zero or more. Establishes prior read model state directly, for scenarios where expressing the state as events would be noise.
- `when <CommandType>` — zero or one. The command being exercised. A specification can declare at most one `when`; declaring a second is a compile error.
- `then <EventType>` — zero or more. An event expected to be produced by the command.
- `then readmodel <ReadModelType>` — zero or more. The read model state expected after the command has run and its events have been projected.
- `then error "<message>"` — zero or more. An expected rejection, matching how other Screenplay constructs already use quoted-string literals for messages (see [Commands](commands.md)).

Property values (`<property> = <value>`) accept the same expressions as `produces` and `capture` mappings — string, number and boolean literals, and `$context.*`/`$env.*` expressions.

## Example

```screenplay
slice StateChange RegisterInvoice

  command RegisterInvoice
    invoiceId  InvoiceId
    customerId CustomerId

  event InvoiceRegistered
    invoiceId  InvoiceId
    customerId CustomerId

  specification RegisteringADraftInvoice
    given CustomerRegistered
      customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      name       = "Acme Corp"
    when RegisterInvoice
      invoiceId  = "9c858901-8a57-4791-81fe-4c455b099bc9"
      customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    then InvoiceRegistered
      invoiceId  = "9c858901-8a57-4791-81fe-4c455b099bc9"
      customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"

  specification RejectingAnInvoiceWithNoLines
    when RegisterInvoice
      invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
    then error "An invoice must have at least one line"
```

## Read model state

When a scenario is really about derived state rather than events, `given readmodel` seeds the read model directly and `then readmodel` asserts what it should look like afterwards:

```screenplay
  specification SendingADraftInvoice
    given readmodel InvoiceListReadModel
      invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
      status    = "draft"
    when ChangeInvoiceStatus
      invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
      status    = "sent"
    then InvoiceSent
      invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
    then readmodel InvoiceListReadModel
      status = "sent"
```

Both forms combine freely with `given`/`then` events in the same specification — establish state with events or read models, and assert on events, read models and errors as the scenario requires.

## The specification vocabulary at a glance

| Construct | Meaning |
| --- | --- |
| `given <EventType>` | Prior state, established by one or more events before the command runs. |
| `given readmodel <ReadModelType>` | Prior read model state, established directly. |
| `when <CommandType>` | The command under test, with its property values. |
| `then <EventType>` | An event expected to be produced by the command. |
| `then readmodel <ReadModelType>` | The read model state expected after the command. |
| `then error "<message>"` | An expected rejection message. |
| `<property> = <value>` | A property value, using the same expression grammar as `produces`/`capture` mappings. |

## Compiling specifications

Specifications compile as part of a full application document via `IScreenplayCompiler.Compile`, or standalone — source rooted at a `specification` declaration — via `IScreenplayCompiler.CompileSpecification`, mirroring `CompileProjection` for the [Projection Declaration Language](projections/index.md).
