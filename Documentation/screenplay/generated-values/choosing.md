---
title: Choose a generation strategy
description: Select a generated identity or reference and test it with portable pinned fixtures.
---

Use this guide when the server owns a new value. First decide whether generation belongs in the command at all; then select the concept's shape and pin a valid fixture.

## Decide who owns the value

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
concept Reference : String
  generate nanoid 16 alphabet "abcdef0123456789" prefix "ref_"
module Billing
  feature Invoices
    slice StateChange Create
      command Create
        reference Reference generated identifier
        produces event Created
      specification Creation
        when Create
          for "ref_0123456789abcdef"
        then Created
          for "ref_0123456789abcdef"
```

This alphabet has 16 symbols: 16 positions give 64 random bits. The fixture is exactly the prefix plus 16 alphabet characters. Compile the model, bind it, and run its specifications: parsing alone does not prove executable admission or target-provider support.

Pin the generated identifier with `for` and other generated values by name. Use `given clock` for scenarios asserting `$context.occurred`; the evaluator does not draw a time-ordered identifier from that clock, so pin the whole identifier. A seed is not a portable fixture. Test a second creation against prior facts on the same source to witness v15 Guarded freshness. Older models remain Unguarded; adding a serialized generation declaration guards implicit UUID generated destinations too.

Your result is a concept with an exact strategy, enough entropy for its role, a valid pinned fixture, and an explicit understanding of [freshness](../generated-values.md#freshness-is-about-the-whole-source).
