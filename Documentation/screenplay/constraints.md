# Constraints

Chronicle checks unique constraints before an append commits. The two declarative `unique` forms describe the constraints Chronicle can enforce. A `file` form names a hand-written Chronicle `IConstraint` class, but it cannot add an arbitrary append-time rule.

## Syntax

```screenplay
constraint <Name>
  unique <property>[, <property>...] on <EventType>   ← unique property constraint

constraint <Name>
  unique event <EventType>                 ← unique event type constraint

constraint <Name>
  unique <property> on <EventType>
  unique <property> on <OtherEventType>
  released by <ReleaseEventType>           ← repeatable
  ignore casing                            ← property constraints only
  message "<text>"

constraint <Name>
  file <Path>                              ← hand-written Chronicle IConstraint
```

| Form | Meaning |
| --- | --- |
| `unique <property>[, <property>...] on <EventType>` | No two event sources may hold the same value (or composite value) for those properties. Repeat the line for other event types that share the claim. |
| `unique event <EventType>` | The event type may occur at most once per event source. |
| `released by <EventType>` | Release the claim when this event occurs on the claiming event source. Repeat for independent release events. |
| `ignore casing` | Compare property values without regard to casing. Not valid for unique events (`PLAY0393`). |
| `message "<text>"` | Use this exact violation message; a `$strings.*` key is kept as written, not resolved. |
| `file <Path>` | Names a hand-written Chronicle `IConstraint` class in a repository-relative file. Chronicle's builder supports only unique property and unique event-type constraints; the parser warns with `PLAY0396`, and the executable semantic model rejects this form (`PLAY0268`). Prefer a declarative `unique` rule. |

## Examples

```screenplay
constraint UniqueInvoiceNumber
  unique invoiceNumber on InvoiceRegistered

constraint OneRegistrationPerInvoice
  unique event InvoiceRegistered
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
- **A null value is skipped.** When all constrained properties have no value, the event is neither checked nor does it claim anything. Two event sources may both leave the property empty.
- **Composite keys preserve property order.** `unique year, code on InvoiceRegistered` compares the combination, not either value alone.
- **Values are compared exactly,** including their casing, unless you declare `ignore casing`.

The property must be one the event declares directly (`PLAY0391`), and the event must be one the application declares (`PLAY0390`). A nested path such as `address.street` is not supported yet.

### Unique events

`unique event <EventType>` means an event source may have the event at most once. A second occurrence for the same event source is a violation; the same event for a different event source is not. Several `unique event` lines under one name are mutually exclusive: once any listed event occurs, another listed event violates the rule until a `released by` event ends the cycle. Do not mix event and property rules under one name.

## When a constraint is violated

A violation is an outcome, not an error in the system. The command that would have appended the violating event is **rejected**, and nothing it would have appended is committed — one violating event rejects the whole command. The rejection names the constraint, and its message says which kind of rule was broken:

| Form | Message |
| --- | --- |
| `unique <property> on <EventType>` | `Constraint '<Name>' is violated: another event source already holds the constrained value.` |
| `unique event <EventType>` | `Constraint '<Name>' is violated: the event source already has the constrained event.` |

The reference executor compares composite constraint values component-wise; Chronicle currently joins components with `-` before hashing, so values containing `-` can collide there until [Cratis/Chronicle#4131](https://github.com/Cratis/Chronicle/issues/4131) is fixed.

The default message never contains the value that collided. A value can be personal data, and a rejection travels further than the event store does. If you declare `message`, its text is returned instead; do not include a sensitive value in that text.

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

Chronicle can enforce more than the language can declare. The language cannot yet express **narrower scope**: limiting a constraint to an event source type, an event stream type or an event stream. The default remains the event sequence within a namespace.

A `file <Path>` constraint names a hand-written Chronicle `IConstraint` class, not a general C# predicate. The Chronicle builder can only declare unique property or unique event-type constraints. Keep existing files parseable, but new declarations should use `unique` so they are portable (`PLAY0396`). The executable semantic model rejects `file` constraints (`PLAY0268`) rather than silently treating them as enforceable rules.

A state-transition rule is not uniqueness. Express it with command validation or a `require` condition over command properties. Conditions over read-model state need decision-consistent reads, which are deferred until [#129](https://github.com/Cratis/Screenplay/issues/129); do not use a `file` constraint as a substitute.

## Guidance

- Constraints belong in the `StateChange` slice whose events they guard.
- Use `unique` for supported uniqueness rules. Chronicle checks uniqueness before the append commits, but updates the index used by later checks **after** the commit; index update failures are logged, not rolled back ([Cratis/Chronicle#4123](https://github.com/Cratis/Chronicle/issues/4123)). Do not describe this as an atomic index-and-append guarantee.
- Choose the name as carefully as an event name. It is the constraint's identity, and renaming it starts a new, empty index.
