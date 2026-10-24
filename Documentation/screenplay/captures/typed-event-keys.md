---
title: "Typed event capture keys"
description: "ESM v32 preserves the type of an incoming event key on captured identities."
---

# Typed event capture keys

A key can also be marked `is event source`, which selects ESM v33 instead; see [Keying by the event source](../captures.md#keying-by-the-event-source).

A capture over `source events` can obtain its event-source identity type from its `key`.
When every consumed event declares exactly one required, scalar property of that name,
and all captures appending the same event agree on its type, the appended event uses that
type instead of Text. For example:

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

Here `OrderReceived` has an `OrderId` event-source identity. A specification's
`given OrderReceived` or `then OrderReceived` with `for "<uuid>"` is bound as `OrderId`,
so a projection keyed by `$eventSourceId` can populate an `OrderId`-keyed read model.
The incoming foreign event's optional `for` remains opaque text: the capture's identity
comes from `orderId` in its payload, not that foreign routing value.

Commands retain their existing identity inference. When no command supplies a type,
untyped sources (including webhook and poll records), non-scalar keys, and disagreeing
capture key types retain the Text fallback. A plain String key therefore keeps the same
model and version as before. An invalid or missing event key still fails the existing
source-events key validation; inference does not weaken that check.

## ESM v32 contract

Only models whose captured identity changes select language version `32.0` and semantic
version `32.0` (schema v32). The existing `eventSourceType` member of capture appends and
the typed identities in specification fixtures carry the inferred type; no JSON member
is added. These changed canonical bytes have new semantic revisions. All other models
retain their existing canonical bytes and selected versions. Older full ESM goldens
remain frozen.

The compiler and strict reader admit v32. Earlier version readers reject it, and marking
a typed capture-only identity as an earlier version is rejected before revision checks.
Consumers, including Stage, must explicitly admit v32 before running these models;
compiler admission does not extend any downstream consumer's supported-version list.
The TypeScript compiler owns syntax only and preserves the typed source property and key
in the shared conformance fixture; it does not emit ESM.
