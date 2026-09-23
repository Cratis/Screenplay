# Constraints

Constraints are server-side rules enforced in the Chronicle kernel **before events are committed**. They protect invariants that must hold under concurrency — uniqueness being the canonical case. Two built-in forms cover the common cases; the third refers to an implementation in a file.

## Syntax

```screenplay
constraint <Name>
  unique <property> on <EventType>         ← unique property constraint

constraint <Name>
  unique event <EventType>                 ← unique event type constraint

constraint <Name>
  file <Path>                              ← implementation in a file
```

| Form | Meaning |
| --- | --- |
| `unique <property> on <EventType>` | No two event sources may hold the same value for the property on the event type. |
| `unique event <EventType>` | The event type may occur at most once per event source. |
| `file <Path>` | The constraint is implemented in C# in the referenced file. |

## Examples

```screenplay
constraint UniqueInvoiceNumber
  unique invoiceNumber on InvoiceRegistered

constraint OneRegistrationPerInvoice
  unique event InvoiceRegistered

constraint InvoiceStatusTransition
  file Constraints/InvoiceStatusTransitionConstraint.cs
```

## What a constraint means

The two `unique` forms have a portable meaning: they are part of the semantic model, and any runtime that executes the model enforces them the same way Chronicle does.

### The name is the constraint's identity

A constraint's name is what the event store knows it by. The name keys the index a unique property constraint keeps, it is what a violation reports, and it is how a constraint is looked up. Two consequences follow:

- A name is unique across the whole application, not just its slice. Declaring the same name twice is an error (`PLAY0392`).
- Renaming a constraint does not carry its history over. The renamed constraint starts from an empty index, so values claimed under the old name are no longer protected by the new one.

### Where it applies

A constraint applies to the whole event sequence, across every event source. Nothing narrows it today; see [what cannot be said yet](#what-cannot-be-said-yet).

### Unique property values

`unique <property> on <EventType>` means that a value, once an event source holds it, is not available to any other event source.

- **A value belongs to an event source.** The event source that holds a value may append the event with the same value again — re-claiming its own value is never a violation.
- **An event source holds one value per constraint.** When it appends the event with a different value, the new value replaces the old one, and the old value becomes available to others.
- **A null value is skipped.** When the property has no value, the event is neither checked nor does it claim anything. Two event sources may both leave the property empty.
- **Values are compared exactly,** including their casing.

The property must be one the event declares directly (`PLAY0391`), and the event must be one the application declares (`PLAY0390`). A nested path such as `address.street` is not supported yet.

### Unique events

`unique event <EventType>` means an event source may have the event at most once. A second occurrence for the same event source is a violation; the same event for a different event source is not.

## When a constraint is violated

A violation is an outcome, not an error in the system. The command that would have appended the violating event is **rejected**, and nothing it would have appended is committed — one violating event rejects the whole command. The rejection names the constraint, and its message says which kind of rule was broken:

| Form | Message |
| --- | --- |
| `unique <property> on <EventType>` | `Constraint '<Name>' is violated: another event source already holds the constrained value.` |
| `unique event <EventType>` | `Constraint '<Name>' is violated: the event source already has the constrained event.` |

The message never contains the value that collided. A value can be personal data, and a rejection travels further than the event store does.

A constraint is checked against what the command would append, together with everything already in the event sequence — including events the same command appends before the one being checked.

### Specifying a violation

A specification's `given` events belong to the event source the command runs against, so a specification can pin a violation of a unique event constraint with `then error`:

```screenplay
module Billing
  feature Payments
    slice StateChange RecordPayment
      command RecordPayment
        paymentId Uuid identifier
        produces PaymentRecorded
          for paymentId
          paymentId = paymentId

      event PaymentRecorded
        paymentId Uuid

      constraint OnePaymentPerAttempt
        unique event PaymentRecorded

      specification RecordingAPaymentTwice
        given PaymentRecorded
          paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        when RecordPayment
          paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then error "Constraint 'OnePaymentPerAttempt' is violated: the event source already has the constrained event."
```

For the same reason, a specification can show that an event source may re-claim its own value, but it cannot yet show a collision between two event sources — a `given` event cannot name another event source.

## What cannot be said yet

Chronicle can enforce more than the language can declare. The semantic model already has room for each of these, so adding them is a language change, not a change to the model:

- **Composite keys.** Uniqueness over a combination of properties, such as a code within a year.
- **Release.** An event that frees a claim, so a value or an event can be used again — for example, a reversal that allows a payment to be recorded again. Without it, a claim lasts forever.
- **Several events under one constraint.** One name covering more than one event type, so they share one index.
- **Messages.** A message of your own in place of the default one.
- **Ignoring casing.** Treating values that differ only in casing as the same value.
- **Narrower scope.** Limiting a constraint to an event source type, an event stream type or an event stream.

A `file <Path>` constraint is not part of the semantic model: binding it reports that it requires a constrained implementation attachment, the same way every other code attachment is reported.

## Guidance

- Constraints belong in the `StateChange` slice whose events they guard.
- Use a constraint — not command validation — for any rule that must hold under concurrent writers; validation happens before the append, constraints are enforced atomically at the event store.
- Choose the name as carefully as an event name. It is the constraint's identity, and renaming it starts a new, empty index.
