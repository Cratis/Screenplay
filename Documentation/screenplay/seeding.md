# Event seeding

A running application often needs a known starting state — reference data, demo customers, the fixtures a test environment expects. Seeding declares that state the same way everything else in Screenplay is declared: as events. A top-level `seed` block lists the events to append per event source id, mirroring Chronicle's `IEventSeedingBuilder.For(eventSourceId, events)`.

## Syntax

```screenplay
seed
  for "<event source id>"
    <EventType>
      <property> = <value>
      ...
    ...
  ...
```

- `seed` — top level, alongside concepts, policies and modules. Multiple `seed` blocks are allowed; they accumulate.
- `for "<event source id>"` — one group per event source id, holding the events to seed for it. The id is a quoted string.
- `<EventType>` — an event declared in the document or made available through an `import`. Referencing an unknown event is reported as a warning.
- `<property> = <value>` — property values using the same expression grammar as `produces` mappings and specification events.

An empty `seed` block — one without any `for` group — is a compile error.

## Example

```screenplay
seed
  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    CustomerRegistered
      customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      name       = "Acme Corp"
    CustomerUpgraded
      tier = "gold"
  for "9c858901-8a57-4791-81fe-4c455b099bc9"
    CustomerRegistered
      name = "Globex"
```

Each group appends its events, in declaration order, to the event stream of its event source id. The runtime performs the seeding through Chronicle's event seeding API, so seeded events flow through projections and reactors exactly like events produced by commands.
