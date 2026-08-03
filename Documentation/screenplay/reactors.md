# Reactors

Reactors are event reaction rules — the "if this then that" of a Screenplay. They observe events and produce side effects: notifications, calls to external systems, or follow-up events. Reactors live inside `Automation` slices.

## Syntax

```screenplay
reactor <Name>
  [description "<text>"]
  on <EventType>
    [description "<text>"]
    [file <Path>]
    [csharp
      ```
      <C# returning event side effects>
      ```]
```

Each `on` clause names the event that triggers the reaction. Everything under it is optional.

## A trigger states intent on its own

Screenplay's workflow is *author the document first, then Stage performs it*, so a reactor must be describable before any code exists. `on <EventType>` with nothing under it is already a complete statement — this reactor observes this event:

```screenplay
reactor PaymentReconciler
  description "Matches settled payments against outstanding invoices and closes them out"
  on InvoicePaid
  on InvoiceMarkedOverdue
    description "Re-checks whether a late payment has since arrived"
```

That document parses, and it tells a reader exactly what the reactor is for — with no file to point at and no code to invent. The `file` reference and the inline block are [realization metadata](grammar.md#declarative-first--file-is-never-required), attached once the slice is implemented.

Give the reactor a `description` for what it does overall, and a trigger its own `description` when *that* reaction needs explaining beyond the event name.

## Examples

Delegating to a file:

```screenplay
reactor NotifyCustomer
  description "Emails the billing contact once an invoice is registered"
  on InvoiceRegistered
    file Reactors/NotifyCustomerReactor.cs
```

Inline C#:

````screenplay
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
````

Inside the block, `@event` is the triggering event; returned events are appended as side effects.

## Guidance

- Reactors that only translate events into other events belong in `Translate` slices when driven by external data ([captures](captures.md)); event-to-event automation stays in `Automation` slices.
- Keep reaction logic small; anything substantial belongs in a `file` reference where it can be tested on its own.
- Describe the reaction before you implement it — a document full of `file` lines and nothing else tells a reader nothing.
