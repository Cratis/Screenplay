---
title: Choose a generation strategy
description: Select a generated identity or reference and test it with portable pinned fixtures.
---

Your shop needs recognizable customer and order identities without turning them into caller inputs. Decide whether the server owns each new value, choose its shape, then pin a valid fixture. Use the [cross-product shop tutorial work](https://github.com/Cratis/Documentation/issues/187) for the wider application journey.

## When not to generate

- A client-minted identity is an input. Do not mark it `generated`; enforce its uniqueness through a constraint or unique event contract.
- A generated identifier is not an authorization mechanism. Apply policies independently.
- An idempotency key represents a repeated request, not a newly drawn value. Generation supplies neither idempotency nor deduplication. A deterministic future `derive` also needs uniqueness or freshness enforcement; derivation alone cannot enforce idempotency.
- A gapless sequence needs an allocator, not randomness. `generate sequence` is reserved.
- Do not generate secrets with the admitted strategies. Secret generation requires a cryptographically secure source, `secret` classification and the separate hashing/derive admission; it is not executable yet.

## Match the shape to the job

| Need | Choose | Check |
| --- | --- | --- |
| Ordinary Uuid identity | Implicit uuid | No additional generation declaration required; 122 random bits. |
| Time-ordered Uuid identity | uuidv7 | Time is visible; ordering within a millisecond is not monotonic. |
| Compact time-ordered String | ulid | 26 characters, uppercase by default; 80 random bits. |
| Typed String identity | typeid | Lowercase prefix plus UUIDv7 base32; empty prefix has no separator. |
| Compact opaque String | nanoid | Default 21 characters, 126 bits; custom alphabets reduce bits per character. |
| Exact random-byte budget | bytes | Count × 8 random bits; choose an encoding, not a padding convention. |
| Readable date/checksum reference | pattern | Parsed, not yet executable. |
| Numeric range or custom implementation | random or rule | Parsed, not yet executable. |

Prefixes help a person recognize a value but add no random bits. Generated identifiers require at least 64 bits. For short non-identifier codes, add a unique constraint on the event property they populate rather than ignoring the collision warning. See the [strategy reference](strategies.md) for exact encodings and collision bounds.

## Declare and verify

```screenplay
concept CustomerId : String
  generate ulid prefix "cus_" case lower
  validate
    matches "^cus_[0-7][0-9a-hjkmnp-tv-z]{25}$"
concept OrderId : String
  generate typeid "order"
concept InviteCode : String
  generate nanoid
module Shop
  feature Customers
    slice StateChange Register
      command Register
        id CustomerId generated identifier
        produces event CustomerRegistered
      specification Registration
        when Register
          for "cus_01arz3ndektsv4rrffq69g5fav"
        then CustomerRegistered
          for "cus_01arz3ndektsv4rrffq69g5fav"
```

The customer prefix helps you recognize the value; it adds no entropy. ULID supplies 80 random bits and the customer rule is Proven, selecting ESM V20. `OrderId` carries time but no tenant slot; keep tenant isolation separate, or introduce a separate `TenantOrderId` contract. `InviteCode` has 126 random bits with its default alphabet. Compile, bind, then run the specifications: parsing alone does not prove executable admission or target-provider support.

Inspect rule diagnostics before proceeding. A Refuted rule (`PLAY0688`) needs a model change, not a retry. Review the derived-`matches` repair if the strategy is the intended contract; it changes validation for all values of that concept. An Unproven rule (`PLAY0689`) needs provider validation, and opaque rules remain Unsupported in the reference evaluator. Never remove an intended business rule merely to silence a warning.

Pin the generated identifier with `for` and other generated values by name. Use `given clock` for scenarios asserting `$context.occurred`; the evaluator does not draw a time-ordered identifier from that clock, so pin the whole identifier. A seed is not a portable fixture. Test a second creation against prior facts on the same source to witness v15 Guarded freshness. Models without a serialized strategy remain Unguarded, even if a rule selects V20; adding a serialized generation declaration guards implicit UUID generated destinations too. Chronicle rechecks `BeforeFirst` on renumbered retries from 19.40.1; batch scopes are checked once and the batch is all-or-nothing. See [Chronicle concurrency](/chronicle/events/concurrency/).

For an API key, plan a separate `ApiKeyId` identity and a secret `ApiKey` value with 32 random bytes. Do not put the key into plaintext event-source metadata. Generated secret admission is not available yet; `[NotAudited]` protects only command causation values, while encryption protects the stored value, not arbitrary logs.

Your result is a concept with an exact strategy, enough entropy for its role, a valid pinned fixture, and an explicit understanding of [freshness](../generated-values.md#freshness-is-about-the-whole-source).
