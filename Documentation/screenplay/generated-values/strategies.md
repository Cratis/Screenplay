---
title: Generation strategies
description: Look up generation syntax, exact encodings, random bits, and collision bounds.
---

Each concept has at most one `generate` line. Modifiers may occur in any order, once each; canonical printing orders strategy arguments, `alphabet`, `prefix`, `case`, then `checksum`. Every random choice uses a cryptographically secure source. The reference evaluator checks pinned fixtures rather than generating values.

## Public strategy catalog

The .NET library publishes `GenerationStrategyCatalog.All` and `Find(string name)` in `Cratis.Screenplay.Semantics`. `Find` uses exact, case-sensitive source spelling and returns null for unknown names. Use this data to populate an editor instead of maintaining another option matrix. For a particular declaration, use [the single-concept check](../concept-checks.md).

| Strategy | Source primitives | First ESM version | Fixed random bits | Option names |
| --- | --- | --- | --- | --- |
| `uuid` | Uuid, String | 15.0 | 122 | prefix, case |
| `uuidv7` | Uuid, String | 15.0 | 74 | prefix, case |
| `ulid` | String | 15.0 | 80 | prefix, case |
| `typeid` | String | 15.0 | 74 | prefix |
| `nanoid` | String | 15.0 | - | size, alphabet, prefix |
| `bytes` | String | 15.0 | - | count, encoding, prefix, case |
| `pattern` | String | 21.0 | - | pattern, checksum |
| `random` | Int | 21.0 | - | range |
| `rule` | String | 21.0 | - | rule, implementation |
| `sequence` | - | reserved | - | - |

The table is checked against the catalog. Every descriptor gives the source `Name`, serialized `EsmKind`, source `Primitives`, nullable `IntroducedIn`, `Status`, `Executability`, nullable `FixedBits` and ordered `Options`. All listed strategies are `Admitted` except `sequence`, whose status is `AwaitingAdmission` and whose version and ESM kind are null. Source `rule` serializes as `implementation`; it requires a target implementation, while built-ins have reference fixture checking. Listing a strategy does not execute it or admit an invalid declaration. Enum defaults are non-admitting.

Each option describes its `Placement` (positional Argument, keyword Modifier or Body), `Kind`, `Required`, inclusive `Minimum`/`Maximum`, `Choices`, decoded `Default`, `Charset`, `Distinct`, optional portable `MatchPattern`, `Conditions` and value-constraint `DiagnosticCode`. Bounds apply to numbers or text lengths; a Pattern's bound is its final emitted length, not the source pattern's length. An Int64Range's bounds constrain both endpoints, which must be ordered. A null default means no single unconditional default: byte `case`, for example, defaults differently for hex and base32 as described below.

All option conditions must hold. `PrimitiveIs` names compatible source primitives; `OptionIn` constrains another option. For example, UUID `prefix` and `case` require String, while bytes `case` requires `encoding` in hex/base32. A Body means exactly one file or tagged inline body. Pattern grammar and bounded support assessment still require the compiler; metadata alone is not a validator. Check a TypeID prefix's portable `MatchPattern` with [value-rule checks](../value-rule-checks.md), not a platform-specific regular expression. `IdentifierMinimumBits` publishes the current 64-bit property threshold; exact support and collision data are computed per declaration.

## Entropy and collision bounds

For `b` random bits, the approximate number of draws at 1% collision probability is `sqrt(2 × 2^b × ln(1/0.99))`. Time and literal prefixes add no randomness.

| Strategy | Random bits | Values at 1% collision probability |
| --- | ---: | ---: |
| uuid | 122 | 3.27 × 10^17 |
| uuidv7 / typeid | 74 | 1.95 × 10^10 |
| ulid | 80 | 1.56 × 10^11 |
| nanoid (21, default alphabet) | 126 | 1.31 × 10^18 |
| bytes (16) | 128 | 2.62 × 10^18 |
| bytes (32) | 256 | 4.82 × 10^37 |

Below 64 random bits, binding warns with the approximate draw count. A unique constraint on the mapped event property suppresses the warning for a non-identifier property. Composite uniqueness is not uniqueness of that property alone. A generated identifier below 64 bits is an error; freshness does not lift this limit. Generated concepts with rules select ESM V20; consistency is checked per rule.

## uuid

```screenplay
concept Id : Uuid
  generate uuid
concept Reference : String
  generate uuid prefix "ref_" case upper
```

Random version 4, RFC 9562 variant, 122 random bits. Default lowercase derived regex: `^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$`. The second example prefixes that shape and uses uppercase letters. A Uuid concept accepts neither `prefix` nor `case`; implicit or explicit unmodified `uuid` writes no ESM `generation` member.

## uuidv7

```screenplay
concept EventId : Uuid
  generate uuidv7
```

Version 7, RFC variant, 74 random bits; no counter or sub-millisecond fields. Derived regex: `^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$`. It exposes time and is not monotonic within a millisecond. String-backed UUIDv7 also accepts `prefix` and `case`.

## ulid

```screenplay
concept CustomerId : String
  generate ulid prefix "cus_" case lower
```

26 Crockford base32 characters; first character 0–7, uppercase by default. 80 random bits. Derived regex for this example: `^cus_[0-7][0-9a-hjkmnp-tv-z]{25}$`. ULID reveals time; do not assume strict ordering within a millisecond or mistake its 128 encoded bits for 128 random bits.

## typeid

```screenplay
concept OrderId : String
  generate typeid "order"
concept BareId : String
  generate typeid ""
```

TypeID 0.3: lowercase Crockford base32 encoding of UUIDv7; 74 random bits. Prefix regex: `^(?:[a-z]|[a-z][a-z_]{0,61}[a-z])?$`. Empty prefix emits no underscore separator. Other prefixes are 1–63 characters; they are not subject to the modifier's 32-character limit.

Derived regex for the first example: `^order_[0-7][0-9a-hjkmnp-tv-z]{9}[ef][0-9a-hjkmnp-tv-z]{2}[89abrstv][0-9a-hjkmnp-tv-z]{12}$`. Decoding must recover a UUIDv7 with the RFC variant. Case is fixed; `case`, `alphabet`, and the separate `prefix` modifier are not allowed. Like UUIDv7, it reveals time.

## nanoid

```screenplay
concept InviteCode : String
  generate nanoid
concept BinaryCode : String
  generate nanoid 64 alphabet "ab" prefix "code_"
```

Size is 1–256, default 21. Default alphabet has 64 symbols: `A-Za-z0-9_-`. An explicit alphabet has 2–256 distinct printable ASCII characters (the printable ASCII repertoire itself has only 95). Entropy is `size × log2(alphabet size)`, not the length of the rendered prefix. Default derived regex: `^[A-Za-z0-9_-]{21}$`; second example: `^code_[ab]{64}$`. Characters follow the alphabet exactly; there is no `case` modifier. Short codes can collide quickly even when they look unpredictable.

## bytes

```screenplay
concept EncodedRandom : String
  generate bytes 32 base64url
concept HexReference : String
  generate bytes 16 hex prefix "ref_" case upper
concept Base32Reference : String
  generate bytes 16 base32
```

Count is 1–512; entropy is exactly `8 × count`. RFC 4648 encodings omit padding. Base64url length is `ceil(8n/6)`; base32 length is `ceil(8n/5)`; hex length is `2n`. Unused trailing bits must be zero: a one-byte base64url value may be `AA` or `_w`, but not `AB` or `_x`.

Derived regex for the first example: `^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$`. For the second: `^ref_[0-9A-F]{32}$`. For the third: `^[A-Z2-7]{25}[AEIMQUY4]$` (128 bits leave two unused bits). Hex defaults to lowercase; base32 defaults to uppercase. Both accept `case lower|upper`; base64url does not. A random value alone is not a credential contract: generated secrets remain unadmitted. The shop's future `ApiKey` uses `bytes 32 base64url` with `secret`; keep it in a value property beside a separate `ApiKeyId` identity. Encryption protects the event value at rest; `[NotAudited]` withholds only command causation values, not event payloads or logs.

## Rule consistency

Give your shop's invitations an explicit shape rule when the same concept also validates supplied or stored codes:

```screenplay
concept InviteCode : String
  generate nanoid
  validate
    matches "^[A-Za-z0-9_-]{21}$"
```

This rule is Proven and admitted silently. A rule requiring only digits would be Refuted: NanoID can emit letters, `_` and `-`. `PLAY0688` is an error regardless of the rule's authored severity and includes a quoted witness. `PLAY0689` is a warning with an Unproven reason, not evidence that every generated value is valid. Code and named-rule predicates are opaque. Patterns inside the [portable match dialect](../commands.md#portable-match-dialect) but outside fragment R can still be refuted by a failing sample; passing samples prove nothing. Older ESM containing a refused construct is deterministically Unproven with reason `outside the portable match dialect: <construct>`, without sampling; re-authoring reports `PLAY0699`. A containment proof is Proven only when the pattern's Glushkov automaton is deterministic (1-unambiguous): the first-position character classes are pairwise disjoint, as are the follow-set classes for every position, after projection onto printable ASCII, and no repeat that can run more than once (such as `*`, `+` or `{2,5}`) applies to a group that can match nothing, like `(a?)*`. Otherwise it is Unproven with reason `ambiguous pattern; the runtime regex may backtrack`. Refuted results are unchanged. Determinism checks retain authored branches before shape-length clipping; construction budgets still apply. This conservative check can warn on bounded optional repeats and unanchored search wrappers even when samples pass. Built-in strategy-derived patterns are deterministic and stay Proven. A finite-pattern repair is offered only when its derived regex is deterministic and bounded; ambiguous repeats may have no repair. Date/checksum digit shapes are approximations and do not establish a containment proof. Runtime samples remain defense-in-depth.

Proof covers `not empty` (at least one non-whitespace character under decision 0071), text `min`, `max`, `length ==`, text equality/inequality, and `matches`, including named match patterns such as email. ESM v21 adds exact numeric interval min/max checks and checksum-by-construction for a pattern declaring the same algorithm. Different checksum algorithms remain Unproven without a conditioned-support proof; a digit-shape counterexample is not a genuine refutation. Existing rule/type compatibility still applies. The generator's exact shape includes prefix, case, UUID version/variant and unused encoding bits; stored values are not checked against a new strategy unless their authored rules say so.

Fragment R accepts ASCII literals and syntax escapes; `\\t`, `\\n`, `\\r`, `\\f`, `\\v`; positive/negated classes and ranges; `\\d`, `\\D`, `\\w`, `\\W`, `\\s`, `\\S`; dot; alternation; ordinary and noncapturing groups; `?`, `*`, `+`, `{m}`, `{m,n}`, `{m,}` and lazy equivalents. Anchors are allowed only at the ends of top-level alternatives. Unanchored patterns retain search semantics. `$` means end of input. Lookaround, backreferences, named groups, flags, word boundaries, non-ASCII patterns and other escapes are outside R, as are empty `[]`/`[^]` classes. The alphabet is projected onto the printable ASCII emitted by these strategies.

### Proof status for renderers

If you consume a deserialized ESM through `Cratis.Screenplay`, call `RuleConsistency.Check(concept, utcAssessmentYear: null)` from `Cratis.Screenplay.Semantics.Generation` on each concept used by a generated property, whether its strategy is explicit or implicit UUID. It returns an `ImmutableArray<RuleConsistencyResult>` parallel to `concept.Validations`, in the same order. A null `concept.Generation` means implicit UUID, not an unknown strategy; do not call it on ordinary, non-generated concepts to infer that they should generate values.

For a renderer that supports unpinned generation, all rules Proven means unpinned generation is safe with respect to these rules. Any Unproven rule requires pinned fixtures; inspect each result's `Reason` to explain why. Refuted cannot occur in a model that passed validation with the same package version and assessment year. Providers still validate generated values, and the reference evaluator still requires pinned fixtures.

Proof status is deliberately not serialized: it is derivable from the concept and the UTC assessment year, and storing it could leave stale results after a prover update or a year change. Results are deterministic for a given concept, package version and UTC year. The optional year defaults to `DateTimeOffset.UtcNow.Year`; supply it explicitly when reproducing a compiler assessment. The existing three-argument `Check(generation, primitive, rule)` uses the same current-UTC-year path.

### Proof budgets

Rule consistency bounds construction before checking containment: at most 2,048 Glushkov character positions after repeat unrolling (not counting the synthetic start position), then at most 10,000 distinct DFA states reached in the product walk. Flat concatenations and alternatives use stack-safe construction. Portable-dialect admission first enforces 1,000 source code units and 3,000 expanded nodes, including optional, loop and alternative choice points. Fragment R separately limits group nesting to 64 simultaneously open groups outside escapes and character classes; exceeding it reports `group nesting budget exceeded`. Exceeding a construction limit makes the rule Unproven, not Proven. A containment proof is also checked against the runtime regex on the all-minimum, all-maximum and alternating samples: a non-match refutes it. Accepted patterns run on Screenplay's own bounded Thompson automaton, without a platform regex engine; there is no wall-clock timeout or host-engine-limit verdict. Passing samples alone prove nothing.

## Modifiers

| Modifier | Strategies | Contract |
| --- | --- | --- |
| `prefix "text"` | uuid, uuidv7, ulid, nanoid, bytes | Printable ASCII, at most 32 characters. UUIDs require String backing. Literal text is regex-escaped in derived shapes. |
| `case lower|upper` | String uuid/uuidv7, ulid, hex/base32 bytes | Changes only the encoded portion, never the prefix. |
| `alphabet "chars"` | nanoid | Distinct printable ASCII characters; replaces the entire alphabet. |
| `checksum luhn|mod11|mod97` | pattern | Appends one luhn/mod11 digit or two mod97 digits; adds no entropy. |

## pattern, random and rule (ESM v21)

````screenplay
concept InvoiceRef : String
  generate pattern "INV-{yyyy}{MM}-#{7}" checksum luhn
  validate checksum luhn
concept TicketNumber : Int
  generate random 100000..999999
concept TenantOrderId : String
  generate rule TenantOrder
    ```csharp
    return $"{context.Tenant}:order_{context.Now:yyyyMMdd}-{Guid.NewGuid():N}";
    ```
````

`GenerationFixturesCorpus.V21R2` publishes this portable `TenantOrderId` body in `generation-fixtures/v21r2`; use `SourceFormsV21R2` for its source. `V21` and `SourceForms` remain the frozen original with the non-compilable `return value;` body. This is a corpus revision, not a new ESM version: both revisions have identical canonical ESM bytes and semantic revision because body bytes belong to implementation attachments.

The `TenantOrderId` body above is portable: it uses only `context.Now`, the tenant in string interpolation and BCL types. A body that passes `context` to a Fundamentals API is bound to the Cratis C# target, where the context is `Cratis.Concepts.GenerationContext`:

````screenplay
concept TenantUlid : String
  generate rule TenantUlid
    ```csharp
    return $"{context.Tenant}:{GenerateValue.Ulid(context)}";
    ```
````

This second example is Cratis C# target-bound. Other providers reject it unless their own binding supports it. See [GenerationContext](../context.md) for the bindings and the portable subset.

`pattern` generates distinct strings from a finite printable-ASCII language. Dates and appended checksum digits add no entropy. See [patterns and checksums](patterns.md) for grammar, support budgets, UTC clocks and fixture policy. Short references are values: map them to a single-property unique constraint, not a low-entropy generated identifier.

`random` has inclusive signed Int64 endpoints, including the full interval -9223372036854775808..9223372036854775807 (2^64 values). Range cardinality and min/max proofs are exact; a fixture is an integral number, never numeric text. It is an Int value, not an identifier.

`rule` requires exactly one direct file or tagged fence. It creates a ValueGeneration implementation requirement with unknown entropy and String result. Its version-1 [GenerationContext](../context.md) is a language-neutral descriptor with only Now and Tenant that each provider binds to its own type. Requirement identity follows concept identity and rule name, not source path; body changes affect the attachment manifest, not ESM body bytes. Providers supply the implementation; the reference runner consumes a primitive String fixture and never invokes it. An opaque validator still makes execution Unsupported.

These constructs select ESM v21; earlier goldens and built-in defaults stay unchanged. `generate sequence` remains reserved (`PLAY0682`, #619). Explicit generation with compliance markers remains refused.

See [choosing a strategy](choosing.md) for the decision guide and [generated values](../generated-values.md) for freshness and pinned fixtures.
