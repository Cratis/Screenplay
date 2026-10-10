---
id: 0054
title: Declare how a concept generates its values, and refuse a generated value that is not fresh
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Contexts/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
---

## Context

`generated` admits only a required scalar property whose concept is over `Uuid` ([0026](0026-generated-values-and-command-responses-in-esm-v7.md), carrying [0023](0023-command-production-model.md)). Binding refuses a generated concept that declares validation rules (`PLAY0268`) and a pre-generation reference to a generated property (`PLAY0273`). The Uuid is always a random v4. Nothing says how a generated value is made, and nothing says what happens when a second creation reuses a generated identifier.

Real models need more. Studio makes about 4,200 `X.New(` calls, mostly `Guid`. It also has a secret link token made from 32 random bytes, and Stage-rendered applications ask for readable order numbers and check-digit references. Without one contract, the compilers, the evaluator, Arc (`[GeneratedValue]`, Cratis/Arc#3125, Cratis/Arc#3128, Cratis/Arc#3129), Stage (Cratis/Stage#175) and Studio would each invent their own strategy names, entropy claims and collision behavior. Arc's generated identifiers are also unprotected against reuse today: the reference runner reuses one pinned fixture across creations by design (0026 *Not guaranteed*), and Chronicle accepts a second append to a stream that already has facts unless a scope forbids it.

The language owns the contract; this record fixes it. The design went through three review rounds; the findings that changed it are folded in below.

`matches` today builds `Regex(pattern, ECMAScript | CultureInvariant)` and calls `IsMatch`, an unanchored search with a one-second timeout (`SemanticMatchPattern.cs`). In .NET, `$` also matches before a trailing `\n`; in JavaScript it does not. A derived rule and a hand-written rule must mean the same thing in both compilers.

## Decision

**1. Generatable concepts.** A concept may declare `generate <strategy> [modifiers]`. A concept that declares a strategy, and every `Uuid` concept (which gets an implicit `uuid`), is a *generatable concept*. `generated` applies to a required, non-collection property of a generatable concept. "Generatable concept" replaces "concept over `Uuid`" wherever the amended records use that phrase. The concept owns the strategy; there is no per-property override. The property says only *that* the value is generated, never *how*. `generated identifier` additionally requires a `Uuid` or `String` concept without a `pii` or `secret` marker (`PLAY0515` stays).

**2. Closed strategy set and allowed primitives.** Names describe meaning, not provider APIs.

| Strategy | Meaning | Primitive |
| --- | --- | --- |
| `uuid` | Random UUID, 122 random bits. The implicit default. | Uuid, String |
| `uuidv7` | Time-ordered UUID. | Uuid, String |
| `ulid` | Time-ordered 26-character identifier. | String |
| `typeid "<prefix>"` | Specified TypeID: `[a-z_]` prefix plus base32 UUIDv7. | String |
| `nanoid <size>` | `size` characters from an alphabet, default 21 over `A-Za-z0-9_-`. | String |
| `bytes <count> base64url\|hex\|base32` | Random bytes, encoded. Exact entropy: 8 x count bits. | String |
| `pattern "<pattern>"` | A readable value from the pattern language (item 4). | String |
| `random <min>..<max>` | Unbiased integer in the inclusive range. | Int |
| `rule <Name>` | Escape hatch: an implementation attachment (item 9). | any generatable primitive |

`Uuid` concepts accept only `uuid` and `uuidv7`. `Int` concepts accept only `random`. Every other strategy needs `String`. `Decimal`, `Bool`, `Date`, `DateTime` and `Enum` cannot be generated. Every random choice uses a cryptographically secure source; none is injectable.

**3. Modifiers.**

| Modifier | Applies to | Rule |
| --- | --- | --- |
| `prefix "<text>"` | `uuid`, `uuidv7`, `ulid`, `nanoid`, `bytes` | Printable ASCII, at most 32 characters. Not on `pattern` (use literals) or `typeid` (it has its own prefix). |
| `case lower\|upper` | `ulid`, hex and base32 `bytes`, String-backed `uuid` and `uuidv7` | Letter case of the encoded value. |
| `alphabet "<chars>"` | `nanoid` | 2 to 256 distinct printable ASCII characters. |
| `checksum luhn\|mod11\|mod97` | `pattern` only | Item 5. |

Proposed syntax (not admitted until the ESM version of item 11 is allocated):

```screenplay
concept ProjectId    : Uuid                                  # implicit uuid; unchanged
concept EventId      : Uuid
  generate uuidv7
concept CustomerId   : String
  generate ulid prefix "cus_" case lower
concept ApiKey       : String secret
  generate bytes 32 base64url prefix "sk_"
concept OrderNumber  : String
  generate pattern "ORD-[A-HJ-NP-Z2-9]{4}-#{4,6}"
concept InvoiceRef   : String
  generate pattern "INV-{yyyy}-#{7}" checksum luhn
concept TicketNumber : Int
  generate random 100000..999999
```

**4. Pattern language.** `#` is a digit, `?` a letter A to Z, `*` a letter A to Z or digit. `[...]` is an ASCII class with ranges and the escapes `\]`, `\-`, `\\`; negation is not supported. `{n}` or `{m,n}` after a token repeats it, with 1 <= m <= n <= 64. `{yyyy}`, `{yy}`, `{MM}` and `{dd}` are date segments taken from the generation clock in UTC; braces containing letters are always segments. Other characters are printable ASCII literals; `\` escapes `# ? * [ ] { } \`. A binding error is an empty pattern or class, a repeat with no token, m > n, n > 64, an expanded length over 256, non-ASCII, a dangling escape, or no random token.

The binder compiles a pattern to a deterministic automaton and counts the accepted strings per length. Generation samples uniformly over distinct strings, so entropy is exactly log2 of that count. Weighting each repeat independently is not used: ambiguous decompositions such as `[AB]{1,64}[AB]{1,64}` would overstate entropy. Construction is bounded before allocation, and a pattern whose automaton or count exceeds the limits fixed in #616 is a binding error, not a warning. The derived regex (for example `^ORD-[A-HJ-NP-Z2-9]{4}-[0-9]{4,6}$`) is available to tooling.

**5. Checksums condition the support.** `luhn` appends one digit, `mod11` one digit with weights 2 to 7 cycling from the right, and `mod97` (ISO 7064 MOD 97-10) two digits. Each is computed over every digit that precedes the check digits, literals included. The binder computes the admissible support after applying the checksum. For `mod11` the excluded remainder 10 removes values from the support; a pattern whose support is empty, for example `"000000006-[AB]" checksum mod11`, is a binding error. Date segments are evaluated over every year in [current, current+100]; if any year yields an empty support, that is an error. Entropy is computed over the conditioned support. Generation samples directly from the admissible set. There is no redraw loop and no attempt limit.

**6. Entropy.** Entropy is log2 of the exact count of the conditioned support, shown by tooling. Below 64 bits a binding warning names the number of values at 1% collision probability. A `generated identifier` below 64 bits is refused until the freshness prerequisite in item 8 is met. On a non-identifier property the warning is suppressed when a `constraint ... unique` covers the event property it maps to. A `rule` strategy has *unknown* entropy; the language never estimates it.

**7. Rule consistency.** Every built-in strategy has a shape: a sequence of runs `(ASCII class, min, max)` and literals. The shape is exact for `uuid`, `ulid`, `typeid`, `nanoid`, `bytes`, `random` and plain patterns. It conservatively over-approximates checksum and date segments, treating them as digits. This record lifts 0026's refusal of rules on a generated concept. Each rule on a generatable concept gets one status:

| Status | Meaning | Outcome |
| --- | --- | --- |
| Proven | Every value the shape can produce satisfies the rule. | Admitted silently. |
| Refuted | A value in an exact shape violates the rule, or length or numeric intervals are disjoint or partly disjoint. | Binding error showing the witness value. |
| Unproven | An opaque `rule` or code block, a regex outside fragment R, an exhausted budget, an over-approximated shape, or a `rule` strategy. | Warning naming the reason. |

Proof covers string length rules, numeric `min` and `max` against `random`, and `matches` (including `matches email`) when the regex is in the regular fragment **R**: literals, escapes, classes, `\d`, `\w`, `\s`, `.`, alternation, groups without backreferences, quantifiers `? * + {m} {m,n}`, and `^` and `$` only at the ends. R excludes flags, lookaround, `\b` and non-ASCII. Containment intersects the acyclic shape automaton (at most 256 positions) with the complement of the regex DFA, wrapping an unanchored regex in `.*`; a state budget of 10,000 exceeded makes the rule unproven. Sample values (all-minimum, all-maximum, alternating) are advisory: a failing sample is a real counterexample and becomes a refutation, and passing samples prove nothing. Both compilers implement this one algorithm and share conformance vectors for status, witness and runtime validation. At runtime every generated value is checked by the concept's validator; a failure is a server defect, never a client error, and generation is never silently redrawn. Specification fixtures must satisfy the rules and the shape; date segments are checked against `given clock` ([0022](0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md)) when present and as digits otherwise. Validation is never derived automatically, so changing a strategy cannot invalidate stored ids. A typed repair offers "add `matches` (and `validate checksum`) derived from the strategy".

**`validate checksum <alg>`** is a new concept validation that accepts exactly the values a `checksum` modifier can produce. **`$` in `matches` means end of input** (JavaScript semantics) in both compilers and the evaluator; the C# evaluator is fixed under Cratis/Screenplay#617, with a trailing-`\n` vector in both compilers. **`sequence` is reserved**: `generate sequence` reports a new binding diagnostic until the sequence design (Cratis/Screenplay#619) is decided.

**8. Freshness.** *Fresh* means: an append whose target event source equals a value generated in this invocation must find no existing facts for that source, in the whole source and not a narrowed stream or event type. The target is compared in canonical form (lowercase `D` for UUIDs, exact ordinal comparison for strings). A violation rejects the command with a concurrency outcome; the handler is never rerun and the caller's retry gets a new value. The policy is explicit per command, never inferred: `Guarded` or `Unguarded`. Handwritten backends default to `Guarded`. Models below the ESM version of item 11 keep reference semantics (`Unguarded`: a pinned fixture may be reused across creations, as at v7 today), and that version means `Guarded`. Both the evaluator and the backends get the same v7 and new-version scenario vectors. A scope attached for freshness is immutable; a conflicting explicit scope for the same (sequence, source) is a command error, and competing `BeforeFirst` scopes merge. A client-minted identifier gets no freshness promise; model `constraint` or `unique event` for it instead. Freshness depends on Chronicle revalidating scopes inside its storage retry loop (Cratis/Chronicle#4713). **A generated identifier below 64 bits stays refused until a Chronicle release with that fix is the declared minimum.** Below 64 bits is admitted nowhere before that, in any version.

**9. `ValueGeneration` and the generation context.** 0002 gains a role `ValueGeneration` for the `rule` strategy: an implementation attachment returning the primitive, with a closed context exposing `Now` (`DateTimeOffset`) and `Tenant` (the portable `TenantId` of `context.md`: the zero GUID for the default tenant, translated per that page, ambiguous mappings rejected). These are the same names as Arc's `GenerationContext`; Arc adapts its tenant accessor into the portable form. Time-ordered strategies (`uuidv7`, `ulid`, `typeid`) take their clock from `Now` in the context form. Inputs and random state are not in the context: a value computed from inputs belongs to `derive` (Cratis/Screenplay#308). A body using other ambient state, or comparing the tenant with a raw `"Default"`, cannot round-trip and is reported as incomplete recovery. The reference runner never runs a `rule` body; the generated value is a pinned fixture.

**10. Secrets.** A `secret` concept that is generated must use `bytes`, `nanoid` or `pattern` with at least 128 bits of exact entropy. `uuidv7`, `ulid` and `typeid` are refused: they reveal time and carry fewer random bits. A `rule` strategy on a secret is refused because its entropy is unknown. Validator warnings and exceptions for a secret are sanitized. Binding a secret waits for compliance-marker admission ([0041](0041-personal-data-secrets-and-processing-purposes.md), `PLAY0268` today); it does not land with the other strategies. Hash-before-store depends on `derive` (Cratis/Screenplay#308).

**11. ESM.** A concept gains an optional last member `generation`, written only when declared: `{"kind":"uuidv7"}`, `{"kind":"typeid","prefix":"user"}`, `{"kind":"bytes","count":32,"encoding":"base64url"}`, `{"kind":"pattern","pattern":"...","checksum":"luhn"}`, `{"kind":"random","min":1,"max":9}`, `{"kind":"implementation","requirement":"<id>"}`, and so on. An explicit `generate uuid` lowers to absent. Proof status is derived and never stored. The version number is allocated at admission under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md); this record names none. Models without `generate` keep their version, bytes, revision and outcomes.

**12. Pin, do not bind.** Specifications pin a generated value by name (`generated <name> = <value>`, `for`), never by seed. A seed's output depends on implementation version and call order and cannot be recovered from code. Generated values are never request inputs and never authorize anything.

### Amendments to accepted records

0021 is `superseded` by 0023, but its item 3 text is the sentence the others carry. This record amends; none of them is superseded, and their decision text is not edited.

| Record | Sentence or rule that changes |
| --- | --- |
| 0021 item 3 and 0023 *Generated values and responses* | "a concept over `Uuid`" and "only to command properties whose type is a concept over `Uuid`" become "a generatable concept". |
| 0026 *Pre-generation references are refused* | The refusal of a generated property whose concept declares validation rules is replaced by item 7: rules are admitted under the proof statuses. |
| 0026 *Generated identifiers* and the `Uuid`-only admission | Non-`Uuid` generated properties and identifiers are admitted for generatable concepts, except below 64 bits (item 8). |
| 0026 *Reference runner* and canonical form | A programmatic `GeneratedValues` entry that fails the concept's rules or shape is `Rejected(Contract)`. |
| 0026 *Not guaranteed* | Fixture reuse stays permitted at v7 and is refused under `Guarded` (item 8). Generation still gives no idempotency guarantee. |
| 0002 | Adds the `ValueGeneration` role (item 9). |

## Options considered

- **Keep `Uuid` only, push the rest to code.** Not taken: modelers stay outside the model, and recovered Studio code would have no home for tokens and order numbers.
- **Open-ended, provider-named strategies** such as `nanoid(21)` as library calls. Not taken: ties the language to libraries and cannot be checked against rules.
- **Regex-driven generation with a redraw loop.** Not taken: unbounded time and overstated entropy. The pattern language is regular and finite, so entropy and the derived regex are exact.
- **Independent weighting of repeats instead of an automaton.** Not taken: ambiguous patterns overstate entropy and clear the secret threshold wrongly.
- **Checksum by redraw (at most 64 attempts).** Not taken: a pattern whose support is empty accepts and then always fails.
- **Sampling as proof of rule consistency.** Not taken: a passing sample proves nothing. Sampling is advisory only.
- **Infer freshness policy from the runtime or ESM version alone.** Not taken: the same `[GeneratedValue]` parameter cannot then preserve v7 fixture reuse. Policy is explicit per command and Stage emits it from the model version.
- **Per-property strategy override.** Not taken: if two properties need different shapes they are different concepts, as with compliance markers and validation.
- **Tenant or input in generation.** Not taken: tenant is a reserved system value and namespaces already partition data; inputs make a pinned value able to contradict them. Both belong to `rule` or `derive`.
- **Built-in KSUID, snowflake, `words`, composites.** Not taken: no demand, or they need worker allocation or a shared word list. Adding one later is additive.
- **Sequences through `generate rule`.** Rejected: a rule has no store. A gapless sequence needs a Chronicle allocator and the retry fix (Cratis/Screenplay#619).

## Default if unanswered

`generated` stays `Uuid`-only and v4-only. Every non-Uuid generated value stays handwritten in code, outside the model and recovered as an incomplete body. The cost is that tokens, order numbers and check-digit references cannot be modeled, and Arc, Stage and Studio will each choose their own strategy names and collision behavior. A reused generated identifier stays undetected.

## Timeline and scope

Holds until superseded. Implementation follows under Cratis/Screenplay#616 in phases: freshness and high-entropy strategies first, then patterns, checksums and context, then secrets.

In scope: the strategy set and modifiers, the pattern language, checksums, entropy, rule consistency, `validate checksum`, `$` semantics, the `sequence` reservation, freshness policy, the `ValueGeneration` role and context, secrets rules, the ESM `generation` member and the documentation outline below.

Out of scope: the implementation itself (#616); sequences (#619); `words`; TypeScript runtime generation; the MCP tools (#618); input-derived values and `derive` (#308); an ESM version number; the Arc, Chronicle, Stage, Studio and AI skill changes, each in its own repository.

## Verification

**Done when:** the record is accepted; #616 admits the strategies in an allocated ESM version; both compilers share the status, witness and runtime-validation vectors of item 7; the evaluator and a Stage-rendered backend agree on the v7 and new-version freshness vectors; and the documentation pages below ship with #616.

**Verify by:** the shared conformance documents in `Source/Screenplay/Compiler/Conformance/`, the canonical corpus and golden ESM bytes for a model using each strategy and one using none (byte-identical to the previous version), a vector for a refuted rule, an empty `mod11` support and a trailing-`\n` `matches`, the Arc scenario vectors for `Guarded` and `Unguarded`, and the CI-equivalent local gates.

## Documentation

Pages that ship with #616, all under `Documentation/screenplay/`:

- `generated-values.md` (explanation). What generated values are and why; the concept declares how and the property declares that; pin by name, not by seed; how freshness and the `Guarded` and `Unguarded` policies behave.
- `generated-values/choosing.md` (how-to). A decision guide, including when not to generate: client-minted ids are inputs; not for gapless sequences; not for secrets without a cryptographically secure source and `secret`; never for authorization; never as idempotency. States that a deterministic `derive` does not by itself enforce idempotency; enforcement needs a unique constraint or freshness. Covers testing with pinned values and `given clock`.
- `generated-values/strategies.md` (reference). One section per strategy and modifier with an example, the derived regex, an entropy table (bits, values at 1% collision probability) and pitfalls: time leakage, non-monotonic order within a millisecond, short patterns, UTC year boundaries, checksum support.

`concepts.md`, `commands.md`, `grammar.md` and `diagnostics.md` are updated with #616. Until then `concepts.md` and `grammar.md` carry a note that the rules are being extended by this proposed record.

## Related

Amends 0021 (item 3), 0023, 0026 and 0002. Builds on 0022 (`given clock`), 0025 (numbering at admission), 0034 and 0041 (secrets). Tracked in Cratis/Screenplay#615, #616, #617, #618, #619, #308 and Cratis/Chronicle#4713.
