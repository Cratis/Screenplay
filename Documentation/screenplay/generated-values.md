---
title: Generated values
description: Understand concept generation strategies, pinned fixtures, and generated event-source freshness.
---

A creation command needs an identity before it can record its first fact. Asking the caller to choose that identity also makes it the caller's input. Screenplay separates those decisions: a concept declares **how** values are generated, and a command property declares **that** the server generates it.

## The concept owns the strategy

```screenplay
concept InvoiceId : Uuid
  generate uuidv7
concept ReceiptCode : String
  generate nanoid
module Billing
  feature Invoices
    slice StateChange StartDraft
      command StartDraft
        invoiceId InvoiceId generated identifier
        receipt ReceiptCode generated
        produces event DraftStarted
          receipt ReceiptCode = receipt
        returns invoiceId
```

Generation follows authorization and validation. Generated values are not request inputs, cannot authorize the caller, and cannot appear in pre-generation rules. A strategy does not automatically add validation to input or stored values; changing it does not retroactively invalidate old identities.

A Uuid concept has implicit `uuid` generation. `generate uuid` without modifiers on that concept has the same executable bytes and revision as omitting the line. String concepts need an explicit strategy. Required scalar properties only are admitted; optional values and collections are not.

## Freshness is about the whole source

A model with at least one serialized concept `generation` member selects ESM v15. Its generated event-source destinations are **Guarded**: an append to a value generated in the invocation requires that the whole source has no existing facts, regardless of stream or event type. Several initial facts in one command are fine. A skipped production does not make a freshness claim. A violation rejects the whole command with `Concurrency` and appends nothing; generation is not silently retried.

Models below v15 remain **Unguarded**, including a model whose only declaration is unmodified Uuid `generate uuid`. They retain legacy fixture reuse. A v15 model guards its implicit UUID generated values too. Providers must carry this policy explicitly when realizing commands; freshness is not idempotency and does not authorize access.

## Pin a value, not a random seed

The reference evaluator never draws randomness. Specifications pin a generated identifier with `for`, and other generated properties with `generated <name> = <value>`:

```screenplay
concept Id : Uuid
  generate uuidv7
module Billing
  feature Invoices
    slice StateChange Start
      command Start
        id Id generated identifier
        produces event Started
        returns id
      specification StartsWithThePinnedIdentity
        when Start
          for "00112233-4455-7677-8899-aabbccddeeff"
        then Started
          for "00112233-4455-7677-8899-aabbccddeeff"
        then returns "00112233-4455-7677-8899-aabbccddeeff"
```

From v15, each generated fixture must match the strategy's exact encoding, size, letter case, UUID version and variant. Invalid fixtures are contract rejections, not client validation errors. Older models keep canonical UUID type validation, without a version check. Seeds are not portable: a provider or algorithm upgrade could change their output.

High-entropy built-ins are executable: `uuid`, `uuidv7`, `ulid`, `typeid`, `nanoid`, and `bytes`. `pattern`, `random`, and `rule` are parsed and printed but not yet executable. `sequence` is reserved. Rules and compliance markers on generated concepts remain refused. See [strategy reference](generated-values/strategies.md) and [choosing a strategy](generated-values/choosing.md).
