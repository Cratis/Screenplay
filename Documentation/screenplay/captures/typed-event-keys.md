---
title: "Screenplay captures: preserve typed event identities"
description: "A Screenplay event capture can preserve a payload key's type on local event identities, so specifications and read models use the same domain concept."
---

# Typed event capture keys

A Screenplay capture over `source events` can preserve the payload key's type as the identity of the local events it appends. Use this when an incoming order id is an `OrderId` concept and the local event, specification and read model should use that same concept rather than a plain text identity.

The payload key determines which local instance receives the event. Its type can be inferred when every consumed event declares exactly one required scalar property with that name, every capture appending the same event agrees on the type, and no reaction producer supplies a different identity type. Commands keep their existing identity inference.

In this example, `OrderPlaced` carries an `orderId` of type `OrderId`. `key orderId` uses that value to identify the order, and the append copies it into the local `OrderReceived` event:

```screenplay
concept OrderId : Uuid
module Sales
  feature Orders
    slice Translate ReceiveOrders
      direction inbound
      event OrderPlaced from "storefront"
        orderId OrderId
      event OrderReceived
        orderId OrderId
      capture OrderIntake
        source events
          from OrderPlaced
        key orderId
        append OrderReceived
          orderId = $.orderId
```

For an incoming `orderId` of `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`, the capture appends `OrderReceived` to that order's event source with an `OrderId` identity. The event's `orderId` field also receives the payload value through `orderId = $.orderId`. These are separate roles: `key` chooses the event-source identity, while the mapping supplies an event property.

A specification's `given OrderReceived` or `then OrderReceived` with `for "<uuid>"` is bound as `OrderId`. A projection keyed by `$eventSourceId` can therefore populate an `OrderId`-keyed read model without reducing that identity to Text.

The incoming foreign event's optional `for` remains opaque text. It might identify an external route that differs from `orderId`; the capture still uses the payload's `orderId` for the local identity. If those values must be equal, mark the key `is event source` as described in [Keying by the event source](../captures.md#keying-by-the-event-source).

When no command supplies a type, inference falls back to Text for untyped sources such as webhook and poll records, non-scalar keys, disagreeing capture key types, or a reaction producer with a different identity type. A plain String key keeps the same model and version as before. A missing or invalid event key still fails source-events key validation; type inference does not relax that check.

## ESM v32 contract

The Executable Semantic Model (ESM) carries the inferred identity type for consumers of the compiled model. Only models whose captured identity changes select language version `32.0` and semantic version `32.0` (schema v32). Marking a key `is event source` selects v33 instead. The existing `eventSourceType` member of capture appends and
the typed identities in specification fixtures carry the inferred type; no JSON member
is added. These changed canonical bytes have new semantic revisions. All other models
retain their existing canonical bytes and selected versions. Older full ESM goldens
remain frozen.

The compiler and strict reader admit v32. Earlier version readers reject it, and marking
a typed capture-only identity as an earlier version is rejected before revision checks.
Each consumer must explicitly admit the selected version; compiler admission does not extend a downstream consumer's supported-version list.

Stage's Cratis backend admits the exact `32.0/32.0` and `33.0/33.0` pairs and assesses newer `32.x` pairs within its declared `>=32.0 <33` range. Its typed capture identities must be Text, Uuid, WholeNumber or non-enumerated concepts over those types. It requires one consistent, required scalar key type across the source events and the same identity type on every append. It refuses an incompatible typed key as `STAGE-ESM-052` rather than coercing it to text. Version admission alone does not make every capture shape executable.

The TypeScript compiler owns syntax only and preserves the typed source property and key in the shared conformance fixture; it does not emit ESM. To check the local events and read-model state produced by your capture, continue to [capture specifications](../specifications.md#clocks-triggers-and-captures).
