---
title: "Screenplay systems and abilities: declare what an external system can be asked"
description: "Declare external systems in Cratis Screenplay with typed abilities, returned values, named outcomes and effect classes, and see how they reach the executable semantic model."
---

# Systems and abilities

A model often has to reach something outside itself: a carrier that books a pickup, a ledger that opens a cost center, a chat tool that asks someone to approve. A `system` names that business system. An `ability` says one thing the system can be asked to do, with typed inputs, the values it returns, the business outcomes other than success, and what a retry may do. With abilities declared, invocations of the system can be checked, specified and run.

This page covers the declaration. A reaction, a workflow action or a state's `entering` line invokes an ability; see [Invoke an ability](#invoke-an-ability).

## Declare a system with abilities

````screenplay
concept ParcelId : Uuid
concept TrackingNumber : String

type Address
  street String
  postalCode String

system Carrier
  description "The parcel carrier's booking service"
  ability BookPickup idempotent
    description "Books a pickup for one parcel"
    parcelId ParcelId
    address Address
    returns
      trackingNumber TrackingNumber
      pickupAt DateTime
    outcome NoCapacity
      description "No pickup slot on the requested day"
  ability CancelPickup idempotent
    trackingNumber TrackingNumber
  ability Quote safe
    address Address
    returns
      price Decimal
````

A system belongs to the application, including when its file is imported into a module or feature. It names the business system, not a provider interface: the model says what the business can ask, and an extension or an implementation owns how.

Each `ability` line names the ability and, optionally, its effect class. Everything under it is optional and may appear in any order; the [canonical printer](printing.md) writes them in the order of this page.

| Line | Meaning |
| --- | --- |
| `description "..."` | What the ability does, for readers and tools. |
| `<input> <Type>` | A typed input. |
| `returns` | A block of typed fields the ability gives back. An ability without `returns` returns nothing. |
| `outcome <Name>` | A named business result other than success, such as a refusal the external system gives. |
| `answers later` | The answer arrives after the invocation returns. |
| `implementation`, `file`, a tagged fence | The code that carries the ability out. |

## Choose the effect class

The effect class says what a retry may do when the outcome of an attempt is unknown. Write it after the name: `ability BookPickup idempotent`.

| Class | Meaning | When an attempt's outcome is unknown |
| --- | --- | --- |
| `safe` | No external effect, such as a lookup. | Retry freely. |
| `idempotent` | Repeating with the same idempotency key has the effect once. | Retry with the same key. |
| `once` | Repeating may repeat the effect, as when posting a message without deduplication. | Never retry; stop for reconciliation. |

`once` is the default, because it is the only class that is never wrong. You opt into retries by saying the system can take them. An effect class other than `safe`, `idempotent` or `once` is `PLAY0984`.

## Inputs and returned values

Inputs use the same property grammar as [operation inputs](operations.md#declare-intent): concepts, enums, composite types, collections and `optional`. They cannot be `identifier` or `generated`, and they carry no subject modifier. An input named like a keyword (`description`, `outcome`, `implementation` or `file`) is written with an `@` prefix, such as `@outcome String`.

`returns` holds typed fields in the same form, one per line, and needs at least one. Every type, in inputs and in `returns`, must be declared: a concept, a type, an enum or an import. An unknown type is `PLAY0986`.

## Name the outcomes

An `outcome` is a business result the external system can give instead of success. It is the external counterpart of a command refusal. An outcome has a name and an optional description. It carries no payload in this version.

````screenplay
system Warehouse
  ability Reserve idempotent
    sku String
    quantity Int
    outcome OutOfStock
      description "Not enough stock to cover the quantity"
    outcome Discontinued
````

An outcome cannot be named `next`, `refused`, `failed`, `returned` or `expired`, compared ignoring case, because those are outlets that invocation sites use (`PLAY0985`). Outcome names are unique within an ability.

## Answers that arrive later

Some systems answer after the call returns: a person approves in a chat tool, or a partner confirms by webhook. Mark such an ability with `answers later`:

````screenplay
concept ParcelId : Uuid

system Approvals
  ability RequestApproval once
    answers later
    parcelId ParcelId
    outcome Declined
````

Workflows and `approval ... via` use these abilities; see [Invoke an ability](#invoke-an-ability).

## Attach an implementation

An ability may carry the code that carries it out. Use the same wrapper that [operation phases](operations.md#attach-phase-intent-or-source) use: a `file`, a tagged fence, or an `implementation` block with ordered `hint` lines and an optional source.

````screenplay
concept ProjectId : Uuid
concept CostCenterCode : String

system Ledger
  description "The finance department's ledger"
  ability OpenCostCenter idempotent
    projectId ProjectId
    returns
      costCenter CostCenterCode
    implementation
      hint "Use the existing accounting adapter"
      file Adapters/OpenCostCenter.cs
````

An ability owns at most one `file` or tagged fence, directly or inside its `implementation` wrapper. Combining sources is `PLAY0494`. This is the `AbilityEffect` attachment role of the [implementation envelope](https://github.com/Cratis/Screenplay/blob/main/decisions/0002-implementation-attachments-envelope-and-reducer-role.md). Its result is exactly one of: values matching `returns`, a declared outcome, or a failure. A missing or mixed result is never success.

A hints-only block is intent. It leaves the ability unrealized.

## Names and diagnostics

Ability names are unique within their system, compared ignoring case (`PLAY0983`). A malformed ability, an outcome with a payload, an empty `returns` block, an identifier input or a repeated `answers later` is `PLAY0495`. The codes are listed in the [diagnostics reference](diagnostics.md#ability-diagnostics).

## What reaches the executable model

A model that declares at least one ability selects [executable semantic model (ESM) v50](interoperability.md). Models without abilities keep their bytes and their version. In ESM v50 and later, `application.systems` lists each system and its abilities:

| Member | Meaning |
| --- | --- |
| `systems[].id`, `name` | The system's stable identity and name. |
| `abilities[].name` | The ability name. |
| `abilities[].effect` | `safe`, `idempotent` or `once`; the default is written out as `once`. |
| `abilities[].inputs` | Typed inputs, `{ name, type }`, in declaration order. |
| `abilities[].returns` | Typed returned fields, omitted when there are none. |
| `abilities[].outcomes` | Outcome names, ordered by name. |
| `abilities[].answersLater` | Whether the answer arrives after the invocation returns. |
| `abilities[].realization` | `{ kind: none \| code \| extension, requirementId? }`. `code` carries the `AbilityEffect` implementation requirement. |

Descriptions are authoring metadata and stay out of the model. Which extension package, version, setting or activation realizes a system does not change what an invocation means, so it is realization metadata, not part of the ESM bytes. The `extension`, `setting` and `activation` lines are described in [Environments](environments.md#system-settings-and-named-secrets).

The [executable model schema](executable-model-schema.md) and the published [ESM v50 schema](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/esm/esm-v50.schema.json) describe the JSON.

A system declared in a model that declares no ability anywhere, operations, and operation specification steps remain authoring-only: binding reports `PLAY0268`.

## Invoke an ability

A reaction, a workflow action and a state's `entering` line ask a system for an ability with `invokes <System>.<Ability>`. The dotted name is what tells an ability from a command. Each input the ability declares is given a value, and the ability's answer is handled where the call is made.

```play
reaction PickupBooking
  runs as system
  when OrderPacked
    parcelId
    shippingAddress
    invokes Carrier.BookPickup
      parcelId = parcelId
      address = shippingAddress
      on returned
        produces PickupBooked
          trackingNumber = $result.trackingNumber
      on NoCapacity
        produces PickupUnavailable
      on failed
        produces PickupBookingFailed
          reason = $failure.reason
```

- Every declared input is mapped exactly once, with a value of a compatible type (`PLAY1003`, `PLAY1004`, `PLAY1005`). The system and ability must exist (`PLAY1001`).
- `on returned` handles a normal answer and reads `$result.<field>` from the ability's `returns`. `on failed` handles a failed call and reads `$failure.reason`. `on <Outcome>` handles one of the ability's declared outcomes (`PLAY1002`). A branch is declared once (`PLAY1006`), and a branch value read outside its branch is `PLAY1008`.
- A `once` ability is never retried, so `attempts` above one on its call site is the warning `PLAY1015`. A reaction has no place to wait, so an ability that `answers later` cannot be invoked from one (`PLAY1010`).
- Specifications script the answer with `given <System>.<Ability> returns`, `answers` or `fails` and assert the call with `then invokes`; see [Specifications](specifications.md#ability-answers).

Reactions, workflow actions and state entries that invoke an ability select [executable semantic model (ESM) v51](interoperability.md). Models that invoke no ability keep their bytes and their version. Operation phases that invoke an ability, an approval answered through an ability (`via`), a `compute` node, a `$trigger` value and a `decide` that reads an ability or compute result parse, print and validate but are not admitted yet; binding reports `PLAY0268` naming [#1252](https://github.com/Cratis/Screenplay/issues/1252).

[Cratis Stage](https://github.com/Cratis/Stage/blob/main/Documentation/reference/esm-feature-matrix.md) lists which ESM features it renders and runs. The design is [decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md).

## Related

- [Operations and external systems](operations.md) describes slice-owned operation intent that uses a system.
- [Reactions](reactions.md), [workflows](workflows.md) and [state machines](statemachines.md) are where invocations belong.
- [Extension contracts](extension-contracts.md) is the reference for the contract file, the `extension` reference, the lock and restore.
- [Environments](environments.md#system-settings-and-named-secrets) covers extension settings, named secrets and activation.
- [Grammar](grammar.md) holds the productions.
