# Reactors

Reactors are event reaction rules — the "if this then that" of a Screenplay. They observe events and produce side effects: notifications, calls to external systems, or follow-up events. Reactors live inside `Automation` slices.

## Syntax

```screenplay
reactor <Name>
  on <EventType>
    [file <Path>]
    [csharp
      ```
      <C# returning event side effects>
      ```]
```

Each `on` clause names the event that triggers the reaction. The reaction body is either a `file` reference to an external C# implementation, or an inline `csharp` block. Inside the block, `@event` is the triggering event; returned events are appended as side effects.

## Examples

Delegating to a file:

```screenplay
reactor NotifyCustomer
  on InvoiceRegistered
    file Reactors/NotifyCustomerReactor.cs
```

Inline C#:

```screenplay
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

## Guidance

- Reactors that only translate events into other events belong in `Translate` slices when driven by external data ([captures](captures.md)); event-to-event automation stays in `Automation` slices.
- Keep reaction logic small; anything substantial belongs in a `file` reference where it can be tested on its own.
