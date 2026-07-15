# Constraints

Constraints are server-side rules enforced in the Chronicle kernel **before events are committed**. They protect invariants that must hold under concurrency — uniqueness being the canonical case. Two built-in forms cover the common cases; complex rules delegate to a file.

## Syntax

```screenplay
constraint <Name>
  unique <property> on <EventType>         ← unique property constraint

constraint <Name>
  unique event <EventType>                 ← unique event type constraint

constraint <Name>
  file <Path>                              ← custom C# implementation
```

| Form | Meaning |
| --- | --- |
| `unique <property> on <EventType>` | No two event sources may commit the event type with the same value for the property. |
| `unique event <EventType>` | The event type may occur at most once per event source. |
| `file <Path>` | The rule is implemented in C# in the referenced file. |

## Examples

```screenplay
constraint UniqueInvoiceNumber
  unique invoiceNumber on InvoiceRegistered

constraint OneRegistrationPerInvoice
  unique event InvoiceRegistered

constraint InvoiceStatusTransition
  file Constraints/InvoiceStatusTransitionConstraint.cs
```

## Guidance

- Constraints belong in the `StateChange` slice whose events they guard.
- Use a constraint — not command validation — for any rule that must hold under concurrent writers; validation happens before the append, constraints are enforced atomically at the event store.
