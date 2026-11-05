---
title: Screenplay generation strategy selection
description: Choose a Screenplay generation strategy for identities, readable references, or short codes, with collision limits and deterministic fixtures.
---

Choose a Screenplay generation strategy by what the value must do: identify an event source, give someone a readable reference, or provide an opaque code. In the shop, a customer identity and an invoice reference serve different purposes. Decide whether people need to read the value, whether time ordering matters, and how collisions will be handled before choosing its format. The [cross-product shop tutorial work](https://github.com/Cratis/Documentation/issues/187) tracks the wider application journey.

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
| Readable date/checksum reference | pattern | Executable Semantic Model (ESM) v21; dates and checksums add no random bits. Pin the UTC clock and declare checksum validation separately. |
| Numeric queue ticket | random | ESM v21 inclusive signed Int64 range; value, never an identifier. |
| Tenant-bearing custom identity | rule | ESM v21 ValueGeneration attachment with Now/Tenant context; entropy is unknown, not zero. |

Prefixes help a person recognize a value but add no random bits. UUIDv7, ULID and TypeID expose creation time, so choose them only when that is acceptable. For NanoID, a smaller alphabet needs a longer code to retain the same randomness.

Entropy measures the randomness in a value, in bits. Generated identifiers require at least 64 random bits; a unique constraint cannot waive that floor. For a shorter non-identifier code, add a single-property unique constraint on the event property it populates. That prevents a duplicate append, but does not make collisions less likely or cause generation to retry. Dates and checksums do not enlarge the random portion of an invoice reference. Use the [strategy reference](strategies.md) to compare exact encodings and collision bounds.

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

The `cus_` prefix makes the customer identity recognizable, while ULID supplies 80 random bits. Every value from this exact strategy satisfies the customer rule, so the compiler reports Proven and selects ESM v20. `OrderId` carries time but no tenant slot; keep tenant isolation separate, or introduce a separate `TenantOrderId` contract. `InviteCode` has 126 random bits with its default alphabet. Compile, bind, then run the specifications: parsing alone does not establish that the model is executable or that a target provider supports it.

If a rule is Refuted (`PLAY0688`), the strategy can produce a value that violates it. Change the strategy or the rule deliberately; retrying cannot resolve that mismatch. Review a derived-`matches` repair only if the strategy is the intended contract, because the repair changes validation for all values of the concept. Unproven (`PLAY0689`) means the compiler could not establish compatibility. Providers must still validate generated values, and opaque rules remain Unsupported in the reference evaluator. Keep intended business rules even when they cannot be proven.

Pin the complete generated identifier with `for` and other generated values by name. The fixture is the value the scenario expects; a seed is not portable across providers or algorithm versions. Use `given clock` for pattern date segments and scenarios asserting `$context.occurred`. The clock does not generate a time-ordered identifier, so that identifier still needs a complete pin.

Then specify a second creation against prior facts on the same source to check Guarded freshness: the reused destination must be rejected without appending events. Models without a serialized strategy remain Unguarded, even if a rule selects v20; adding a serialized generation declaration guards implicit UUID generated destinations too. Chronicle rechecks `BeforeFirst` on renumbered retries from 19.40.1; batch scopes are checked once and the batch is all-or-nothing. See [Chronicle concurrency](/chronicle/events/concurrency/).

For an API key, plan a separate `ApiKeyId` identity and a secret `ApiKey` value with 32 random bytes. Do not put the key into plaintext event-source metadata. Generated secret admission is not available yet; `[NotAudited]` protects only command causation values, while encryption protects the stored value, not arbitrary logs.

For an invoice reference or numeric ticket, continue with [Patterns and checksums](patterns.md) to define the format and pin its date. For a command's generated event-source identity, use [Generated values](../generated-values.md#freshness-is-about-the-whole-source) to check freshness and provider obligations.

Before finishing, check that the strategy fits the value's role and has enough entropy, that the pinned fixture is valid, and that the command's freshness policy is explicit.
