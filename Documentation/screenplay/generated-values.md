---
title: Generated values
description: Understand concept generation strategies, pinned fixtures, and generated event-source freshness.
---

Your shop needs a customer identity before it can record a registration, an order identity when checkout starts, and an opaque invitation code. Asking callers to invent these values turns server decisions into request inputs. Screenplay separates them: a concept declares **how** values are generated, and a command property declares **that** the server generates it. For the full cross-product journey, follow the [shop tutorial work](https://github.com/Cratis/Documentation/issues/187); this page explains the model contract.

## The concept owns the strategy

```screenplay
concept CustomerId : String
  generate ulid prefix "cus_" case lower
  validate
    matches "^cus_[0-7][0-9a-hjkmnp-tv-z]{25}$"
concept InviteCode : String
  generate nanoid
  validate
    matches "^[A-Za-z0-9_-]{21}$"
module Shop
  feature Customers
    slice StateChange Register
      command Register
        customerId CustomerId generated identifier
        inviteCode InviteCode generated
        produces event CustomerRegistered
          inviteCode InviteCode = inviteCode
        returns customerId
```

Generation follows authorization and validation. Generated values are not request inputs, cannot authorize the caller, and cannot appear in pre-generation rules. A strategy does not automatically add validation to input or stored values; changing it does not retroactively invalidate old identities.

A Uuid concept has implicit `uuid` generation. `generate uuid` without modifiers on that concept has the same executable bytes and revision as omitting the line. String concepts need an explicit strategy. Required scalar properties only are admitted; optional values and collections are not.

## Freshness is about the whole source

A model with at least one serialized concept `generation` member selects ESM v15 or a later version when another feature needs it. Its generated event-source destinations are **Guarded**: an append to a value generated in the invocation requires that the whole source has no existing facts, regardless of stream or event type. Several initial facts in one command are fine. A skipped production does not make a freshness claim. A violation rejects the whole command with `Concurrency` and appends nothing; generation is not silently retried.

Models without a serialized generation strategy remain **Unguarded**, including a model whose only declaration is unmodified Uuid `generate uuid`, even if rules select V20. They retain legacy fixture reuse. A model declaring a serialized strategy guards its implicit UUID generated values too. Providers must carry this policy explicitly when realizing commands; freshness is not idempotency and does not authorize access.

## Pin a value, not a random seed

The reference evaluator never draws randomness. Specifications pin a generated identifier with `for`, and other generated properties with `generated <name> = <value>`:

```screenplay
concept OrderId : String
  generate typeid "order"
module Shop
  feature Orders
    slice StateChange StartOrder
      command StartOrder
        id OrderId generated identifier
        produces event OrderStarted
        returns id
      specification StartsWithThePinnedIdentity
        when StartOrder
          for "order_0000000000e008000000000000"
        then OrderStarted
          for "order_0000000000e008000000000000"
        then returns "order_0000000000e008000000000000"
```

A model declaring a serialized strategy checks each generated fixture's exact encoding, size, letter case, UUID version and variant. A generated concept with rules selects ESM V20 and checks its fixtures against both shape and rules, including implicit UUIDv4 generation. A failing rule returns `Rejected(Contract)` with `Generated fixture '<property>' fails concept '<Concept>' rules`; it is a server contract defect, not a client validation error, and nothing is redrawn. Opaque validation rules route to Unsupported because the reference evaluator never runs attached code. A generation implementation alone does not block execution: the runner accepts its String fixture without invoking the body. Older rule-free, implicit-UUID models keep canonical UUID type validation. Seeds are not portable: a provider or algorithm upgrade could change their output.

ESM v21 admits finite `pattern`, exact inclusive Int64 `random`, `generate rule` attachments and String-concept checksum validation. The shop's `InvoiceRef` is a date/checksum value; `TicketNumber` is a numeric value, not an Int identifier. Pin date-segment fixtures with `given clock`: all segments compare against that same UTC instant. Without a clock the runner checks digit widths only, never the wall clock. Checksum suffixes need only digit shape unless `validate checksum` is authored; that separate rule enforces arithmetic on supplied and stored values too. See [patterns and checksums](generated-values/patterns.md).

TypeID carries time, not a tenant slot. Keep tenant isolation in the namespace; a tenant-bearing value needs a separate concept such as `TenantOrderId`, not a modifier on `OrderId`.

## Make the strategy and rules agree

The customer and invitation rules above are Proven: every value those exact shapes produce satisfies them. A Refuted rule stops binding with `PLAY0688`, naming the strategy and a quoted violating value. An Unproven rule produces `PLAY0689` with the reason: a regex outside fragment R, a construction budget, or opaque validation. That warning is not a proof. Providers still validate every generated value and treat a failure as a server defect; they never silently draw another one.

Rules select V20 even when an implicit-UUID concept serializes no strategy. No proof status is serialized. Changing generation never adds or changes validation automatically, so stored values keep their authored rules. The [strategy reference](generated-values/strategies.md#rule-consistency) describes fragment R and proof limits.

## Work with strategies through MCP

You can inspect and change the concept's strategy without editing generated values on each command. `declaration-details` with `kind: "Concept"` and `view: "summary"` shows the authored strategy, modifiers and `arguments`, random bits, the count at 1% collision probability, and compiler refusal codes. Pattern and random strategies include an exact JSON integer `supportCount` and its base-two logarithm as `entropyBits`; checksums add no entropy, and date tokens use the minimum conditioned support across the compiler's 100-year UTC assessment horizon. Preserve large counts without floating-point rounding. A `rule` argument names the generation rule; find its ValueGeneration requirement with `read-workspace` and `view: "implementation-requirements"`. Its entropy is unknown because the implementation is opaque. An implicit UUID default has no authored `generation` block.

The `generation.executable` object reports `status: "admitted"` only after the whole model produces an executable model, `"refused"` for compiler errors at this concept, its generation or its rules, or `"notAssessed"` with a `reason` when unrelated failures prevent an executable model. Its `diagnosticCodes` lists errors at this declaration, so an empty list is not proof of admission. Invalid alphabets, non-positive sizes/counts, malformed patterns/ranges, support-budget exhaustion (`PLAY0691`) or empty conditioned support (`PLAY0692`) report null entropy and collision estimates with `entropyUnknownReason`; inspection preserves the compiler diagnostics rather than failing. Pattern and range assessment failures carry the compiler's message.

The existing `propose-ast` tool replaces a `ConceptSyntax` with its typed `GenerationSyntax`; `propose-source` accepts the equivalent whole source document. Set, change or remove `generation`, review the proposal, then explicitly apply it. Canonical printing retains `generate`. Authoring acceptance does not imply executable readiness: generation budgets, empty conditioned support and other compiler refusals still matter; generated-concept rules report their proof diagnostics.

For `PLAY0683` or `PLAY0684`, the repair inventory can offer a larger NanoID or byte count reaching 64 random bits. A non-identifier mapped into one event property can instead gain an explicit unique constraint (`PLAY0683`). These are alternative model changes, not silent fixes: use the selected subject with `propose-repair`, review the changed documents, then apply. A length change affects every property using the concept; a unique constraint changes append-time behavior.

For `PLAY0688`, `propose-repair` can replace the refuted rule with `matches "<derived pattern>"`. Review it carefully: the rule also validates input and stored values of that concept, not just new fixtures. For patterns, the derived expression must be deterministic and bounded; otherwise no repair is available, with a reason. If generation declares a checksum, the same proposal explicitly adds checksum validation unless already present. Date/checksum shape approximation is not a containment proof. See the [MCP generation reference](mcp/reference.md#generated-values-and-responses) for the fields and repair contract.

Built-ins remain executable: `uuid`, `uuidv7`, `ulid`, `typeid`, `nanoid`, and `bytes`. `pattern`, `random`, `rule` and checksum validation select ESM v21; built-in-only models retain their prior versions and bytes. `sequence` is reserved. Compliance markers on explicitly generated concepts remain refused. In the shop, `ApiKey` is a secret value (32 random bytes), never an event-source identity: use a separate `ApiKeyId`. Its `secret`/encryption contract protects storage; `[NotAudited]` only withholds command values from the causation chain, not event payloads or logs. Generated secrets await separate admission. See [strategy reference](generated-values/strategies.md) and [choosing a strategy](generated-values/choosing.md).
