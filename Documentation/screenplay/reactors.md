# Reactors

Reactors are event reaction rules — the "if this then that" of a Screenplay. They observe events and produce side effects: notifications, calls to external systems, or follow-up events. Reactors live inside `Automation` slices.

## Syntax

```screenplay
reactor <Name>
  on <EventType>
    [produces <EventType>]
    [executes <Command>]
    [file <Path>]
    [csharp
      ```
      <C# returning event side effects>
      ```]
```

Each `on` clause names the event that triggers the reaction. Everything under it is optional: what the reaction produces, and where its implementation lives - a `file` reference to an external C# implementation, or an inline `csharp` block. Inside the block, `@event` is the triggering event; returned events are appended as side effects.

## Say what the reaction does

`on` tells the reader what wakes the reactor up. `produces` and `executes` tell them what happens next - the output side of an automation slice, which is the whole reason the slice exists:

```screenplay
reactor AcceptedInvitationProvisioner
  on InvitationToJoinAccepted
    produces UserAccountProvisioned

reactor StockKeeping
  on BookReserved
    executes DecreaseStock
```

Both are repeatable, so one trigger can produce several events and execute several commands. `produces` names an event declared in the document (or imported); `executes` names a command. The compiler resolves both and warns about a name it cannot find, the same way `command produces` has always been checked.

Together with `on`, this closes the Event Modeling loop the document is meant to describe: state change → events → projections → automation → commands → state change.

## Declare the intent before the code exists

You write the document first and Stage performs it, so a reactor has to be sayable before anything implements it. A trigger with no body is a complete, valid statement of intent:

```screenplay
reactor AcceptedInvitationProvisioner
  on InvitationToJoinAccepted
```

That reads as "when an invitation is accepted, this reactor runs" - which is what an author knows at modeling time. The `file` line arrives later, when the slice is implemented, and it never changes what the reactor *means*:

```screenplay
reactor AcceptedInvitationProvisioner
  on InvitationToJoinAccepted
    file Admin/Invitations/Provision/Provision.cs
```

This is the general rule across the language: **a document must be expressible and meaningful with zero `file` references.** `file` is attachable realization metadata on any construct that supports it. Hand-authored documents precede the code and gain `file` lines as slices are implemented; generated documents arrive with them already attached. Same language, two directions.

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
