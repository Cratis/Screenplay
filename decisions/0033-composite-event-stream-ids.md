---
id: 0033
title: Declare composite event stream ids as named parts and encode them with an escaped separator
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/EventModels/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

**Stream ids today.** An event source declares streams. A keyed stream names one id type, as in `streamId Month`. A command routes to it with `stream Account.Transactions` and one child mapping, `streamId = month` ([event sources](../Documentation/screenplay/event-sources.md)).
- The parser accepts a single mapping and no grandchildren (`Parsing/EventSourceParser.cs:51-85`).
- An optional, collection or composite `type` stream id is refused with PLAY0503 (`Parsing/EventSourceValidator.cs:55-59`).
- Other unsupported scalar types are refused with PLAY0506. The allowed types are String, Uuid, a concept of either, and Int-backed concepts.
- The TypeScript compiler mirrors these rules.
- Decision [0031](0031-event-source-and-stream-in-specifications.md) put the same `stream` / `streamId = <literal>` lines into specifications. It deferred composite ids to this decision on two conditions: specifications mirror the route's parts with literal values, and declared parts are never written as one pre-joined string.

**What the runtime stores.** Chronicle's `EventStreamId` is one string. Arc gets it from `[EventStreamId]` or from `ICanProvideEventStreamId`, a method that returns one string (Arc `EventStreamIdValuesProvider.cs`). Neither defines a composite stream id.
- Applications that key a stream by several values join them by hand in that method. For example, a project ledger per period becomes `"p-1:2026-10"`, with a separator and escaping each application chooses for itself.
- Chronicle's own composite unique constraints join with `-` and collide ([Chronicle#4131](https://github.com/Cratis/Chronicle/issues/4131), noted in [constraints](../Documentation/screenplay/constraints.md)). That is the precedent not to repeat.

**Why it matters.**
- A model can only express a multi-value stream by inventing a joined text property on the command. That hides the parts and has no collision-free encoding. Stage, Arc's generator and the reference runner are left guessing at the shape.
- [#407](https://github.com/Cratis/Screenplay/issues/407) is about to bind routes into the executable model (ESM) with a scalar stream id. If composites later reshape that rather than extend it, every consumer migrates twice.

**Related idioms.**
- PDL composite projection keys use a named `type` followed by `Part = expr` child lines ([projection keys](../Documentation/screenplay/projections/keys.md)). Chronicle stores those keys as objects, not strings.
- Constraints use a positional list (`unique year, code on E`).
- Composite read-model keys ([#459](https://github.com/Cratis/Screenplay/issues/459)) are proposed but not decided.

## Decision

### Spelling

A keyed stream declares either one id type, as today, or a `streamId` block of two or more **named parts**. A command route and a specification map each part by name under the same `streamId` header:

```play
eventsource Project
  identifier ProjectId
  stream Ledger
    streamId
      projectId ProjectId
      period Period
  stream Notes
    streamId NoteBucket            // scalar: unchanged

command BookHours
  projectId ProjectId identifier
  month Period
  hours Hours
  stream Project.Ledger
    streamId
      projectId = projectId
      period = month               // a command property path or a literal, as for scalar routes
  produces HoursBooked
    for projectId

specification Booking_the_second_entry_of_a_period
  given HoursBooked
    for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    stream Project.Ledger
      streamId
        projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        period = "2026-10"
    hours = 2
  ...
```

**Parts**
- A composite `streamId` block has at least two parts. Part names are unique, exact and case-sensitive.
- Each part's type comes from the scalar stream id subset. Parts cannot be optional, collections, `type`s or enums.
- Parts take no modifiers or children.
- A stream declares either `streamId T` or the block, never both.
- One part would just be another spelling of the scalar form, so it is refused.

**Mapping**
- A route or specification on a composite stream maps every declared part exactly once, by name. Mapping lines may be in any order.
- A scalar mapping (`streamId = x`) on a composite stream is refused, and so is a part block on a scalar stream.
- Each part source follows the existing scalar route rules: a command property path whose type matches nominally, or a literal. Raw, object, list and null sources are refused.
- Specifications map literals only, as in 0031.

**`for` stays independent.**
- The event source id comes from `for` (or the production's allocated destination per [0023](0023-command-production-model.md)), never from a stream part.
- A part may carry the same value as the source identifier. That can be useful when a stream id must stand on its own, but it is optional.
- Nothing infers or checks that a part equals `for` because the names match.

**Empty text is refused.** A composite part that is empty text is refused, in literals at authoring time and at execution in the admitting version. Whitespace is not empty. This is domain policy for one consistent rule across scalar and composite ids. It is not needed for collision freedom: `("", "x")` would encode as `|x`.

**Parsing**
- The bare `streamId` header is recognized only as a child of a `stream` declaration, a command route or a specification route. It never changes existing PLAY0505 handling of `stream Source.Stream` versus a qualified property type.
- Payload properties and mappings named `streamId`, parts named `streamId` or `stream`, `@stream`, and the `concurrency` block's own `streamId <dimension>` line all keep their current meaning.

**Diagnostics.** These reuse existing codes, so this decision claims no new code:

| Code | Covers |
| --- | --- |
| PLAY0503 | A malformed stream declaration, including a one-part block, duplicate parts, modifiers, children, and both forms on one stream |
| PLAY0506 | An unsupported part type |
| PLAY0504 | A route mapping that is missing, unknown, duplicated, mismatched in shape or incompatible |
| PLAY0547 / PLAY0549 | The same problems in specifications |
| PLAY0526 | Route lines in examples |
| PLAY0268 | Executable binding until admission |

### Canonical encoding

Runtimes that store one string encode a composite stream id like this:

1. **Format** each part with the portable scalar rule: text unchanged, compared ordinally and never normalized; UUID lowercase hyphenated; integer in invariant decimal with no fraction or exponent, and `0`, never `-0`.
2. **Escape** each formatted part: replace `%` with `%25`, then `|` with `%7C`. These are the only escapes, always uppercase hex.
3. **Join** the escaped parts with `|` in the stream's **declaration order**.

Example: `3fa85f64-5717-4562-b3fc-2c963f66afa6|2026-10`. The text part `a|b%` encodes as `a%7Cb%25`.

**Decoding**
1. Split on literal `|` first.
2. Check the arity against the declared part count.
3. Decode each component **exactly once**, recognizing only `%25` and `%7C`.
4. Check that each decoded part is in canonical scalar spelling for its type.

A wrong arity, a `%` not followed by `25` or `7C` (including lowercase hex and a trailing `%`), or a noncanonical part is a malformed id and is refused. It is never repaired. Decoding before splitting, decoding more than once, and generic URL decoding are wrong.

**What collision freedom covers.**
- Within **one** declared key schema, the mapping from part tuples to strings is injective, and the result always contains `|`.
- It says nothing across schemas. A scalar text id `"a|b"` and the composite `("a", "b")` encode to the same string, and so can two schemas whose values format alike.
- A route's identity is the source type, the stream type and the encoded id, with the event source id (`for`) independent. Providers must show which of these each operation they support actually filters on. They may not assume stream ids are globally unique: some Chronicle storage keys only by stream type and id.

**Identity encoding, not transport encoding.** The result is the stored identity. It is neither a URL path segment nor safe raw log text. Parts may contain `/`, `?`, `#`, control characters or any Unicode. Transport and logging apply their own escaping on top and must preserve the bytes; providers need byte-preserving transport tests.

**Ill-formed values.** Text containing lone UTF-16 surrogates is refused: at binding for literals, and at execution as an atomic failure of the command, with no partial append. Integer bounds and the failure phase for runtime values follow the execution contract #407 admits. Until then, the vectors pin the ±(2^53−1) bound #407 proposes for Double mode.

The encoding is Screenplay's portable contract, like 0023's scalar rules, and a provider may not choose another.

### Identity and evolution

**What is identity-bearing.** Declaration order and the formatted values are identity-bearing. Part names and the order of mapping lines are not.
- Renaming a part keeps every stored id **only if** its position, its type and every mapping that feeds it keep their meaning. The name is not recorded anywhere, so a rename that silently changes which value goes in a position is not detected.
- Adding, removing or reordering parts, changing a part's type, or switching a stream between scalar and composite **may** split, preserve or alias existing ids. For example, an Int part with `1` and a String part with `"1"` format the same. Reordering two parts with equal values keeps the id. Reordering unequal parts can land on another instance's id.

**Rule.** Changing the key schema of a stream that has stored events requires a new stored stream identity, meaning a new stream with its own name or `id` pin, or an explicit, validated migration. The documentation states this next to the syntax. Where tooling has a baseline (a persisted contract or a previous revision), it reports a schema-order or type change as potentially destructive.

**Legacy hand-joined ids stay scalar.** Text such as `"p-1:2026-10"` keeps passing through unchanged until an application migrates it explicitly.

### Comparison in specifications

A `then` route on a composite stream matches when, under the same resolved stream declaration, every part's **canonical formatted value** equals the expected one. That is the same as comparing canonical encoded ids. It is not a comparison of authored literals or a generic value equality: `"3FA8…"` and `"3fa8…"` are the same UUID part. The checker's contradiction tests (`SpecificationStreamValidator`) use the same canonical comparison.

### Generators and renderers

- Stage emits exactly this encoding when it generates `ICanProvideEventStreamId`.
- An extractor or generator, such as Arc's Screenplay generator, may recover named parts only from a known Screenplay declaration with a matching schema, or from a recognized formatter. Otherwise it reports a classified loss or refuses.
- It never guesses parts from a stored string. `"a|b"` might be a scalar.

### Personal data

The encoding is reversible. It gives no confidentiality and no erasure protection, so stream ids show up in storage, telemetry and errors as written. Use non-sensitive surrogate identifiers for parts. Formatting and decoding diagnostics never include part values. Policy for `@pii` values flowing into stream ids, parts or identifiers is tracked in [#525](https://github.com/Cratis/Screenplay/issues/525).

### Representation

**Syntax (additive)**
- `EventStreamSyntax` gains `StreamIdParts` (name, type, location), mutually exclusive with `StreamId`.
- `CommandStreamSyntax` and `SpecificationStreamSyntax` gain `StreamIdParts`, a list of part mappings, mutually exclusive with `StreamId`.
- Both printers keep declaration order for stream parts and authored order for mapping lines. They never sort.

**ESM (with or after #407)**
- **Declarations.** A stream is unkeyed (no stream id type and no parts), scalar (`streamIdType` only) or composite (`streamIdParts: [{ name, type }]` only, with two or more parts in declaration order).
- **Routes.** A route mirrors its stream: no stream id, a scalar `streamId`, or `streamIdParts` covering every part exactly once, serialized in declaration order.
- **Readers.** Strict readers refuse every other combination: both forms, an empty or one-part list, a missing or extra part.
- **Admission scope.** Authoring accepts command property paths per part. The executable admission accepts the same sources #407 admits for scalar routes, and widens only with it.
- **Runner.** The reference runner keeps #407's eager resolution and atomic failure: if any part fails to resolve or format, the command fails and nothing is appended.
- **Formatter.** `SemanticStreamIdFormatter` gains the composite rule and its decoder.
- **Ask of #407.** Keep its scalar fields shaped so this addition is additive.

### Versioning

The authoring surface can land now: both compilers, the printer, the typed AST, MCP, Monaco, the VS Code grammar, board route rendering and the docs. Binding refuses composite streams with PLAY0268, as it refuses every route today.

Executable admission follows 0031's rule: it joins #407's ESM version if the evidence is ready together, otherwise it takes the next claimed one.

## Options considered

### Spelling

- **A. Named parts under a `streamId` block.** Chosen.
  - It reuses the `streamId` keyword and two line shapes Screenplay already has: declaration (`name Type`) and mapping (`name = source`).
  - It mirrors the named `Part = expr` bindings of PDL composite keys.
  - Mapping by name makes the line order in routes and specifications irrelevant.
  - Scalar syntax is byte-identical.
  - A bare `streamId` header with children is an error today wherever it can appear, so the extension is unambiguous for both parsers.
  - The cost is one more nesting level: one mapping line per part under a header.
- **B. A stream id typed by a shared composite `type`.** Rejected. A general-purpose `type` is a reusable structural shape, used by events and commands, where reordering properties is harmless. Using one here would tie that shape to positional stored identity, so an edit made for another reason would re-key streams. Named child mappings could still be used with a type, so mapping style is not the objection. This does not rule out a future **dedicated** reusable key schema declaration, if repeated part lists become a real burden.
- **C. An inline positional tuple:** `streamId (projectId ProjectId, period Period)` / `streamId = (projectId, month)`. Rejected.
  - It needs a new tuple expression in both expression parsers, the printer and both editor grammars.
  - Positional mapping hides which property feeds which part.
  - 0031 already rejected positional stream ids.
- **A′. A flat repeated `streamId <part> <Type>`.** Rejected. It tells `streamId Month` and `streamId month Month` apart only by token count, which is fragile in both parsers and in the TextMate grammar.

### Encoding

- **`|` with `%` escaping of exactly two characters.** Chosen.
  - It is injective within a schema, never equals the empty or `"Default"` id, and leaves single-component values unescaped and readable.
  - Canonical formatted parts that contain neither `%` nor `|`, which covers UUIDs and integers, are unchanged by identity escaping. Transport and logging may still escape them on their own.
- **A canonical JSON array of already formatted strings.** A credible alternative. Formatting each part first removes the number-formatting concern, but JSON string escaping of control characters and non-ASCII text must then be pinned exactly across runtimes. It is also noisier in stores and logs. Not chosen, but not rejected as unsound.
- **Length-prefixed parts** (`5:p-1|7:2026-10`). Rejected: injective, but hard to read in stores and logs.
- **A plain join.** Rejected: it repeats Chronicle's colliding constraint behaviour.
- **A `:` or `/` separator.** Rejected.
  - `:` is what applications join with today, so ids that look identical would silently mean something else under the escaping rules.
  - `/` suggests hierarchy and URL paths.

### Single-part composites

- **At least two parts.** Chosen. A single value already has the scalar spelling, and two spellings of one identity invite mismatched routes. The encodings would agree for most values, but not for text containing `|` or `%`.
- **Allow one part, "to grow later".** Rejected: adding a part changes the key schema anyway, which requires a new stored stream identity or a migration.

### Alignment with composite read-model keys (#459)

- **What differs.** Read-model keys are objects in Chronicle, while stream ids are one string, so the two need not share an encoding.
- **What this decision recommends.** That #459 adopt the same **name-based value binding** (`part = source` children) wherever it maps values to multi-part keys, such as `reads … by`.
- **What it does not do.** It does not require #459 to spell read-model key declarations or query `by` declarations the same way. #459 makes its own decision.

## Default if unanswered

- Models keep inventing joined text properties for multi-value streams.
- Every application keeps choosing its own separator.
- Stage, Arc's generator and the reference runner can't tell parts from opaque text.
- #407 binds a scalar-only shape that a later composite form would have to reshape.

## Timeline and scope

**In scope:**
- the composite `streamId` block on streams, command routes and specification routes, with its checks;
- the canonical encoding and decoding;
- the evolution rule;
- canonical part-wise specification comparison;
- the generator preservation rule;
- the syntax and ESM invariants;
- the version rule;
- both compilers, the printer, the typed AST, MCP, Monaco, the VS Code grammar, the event model board's route rendering, documentation and conformance vectors.

**Out of scope:**
- composite read-model keys (#459);
- composite `concurrency` dimensions;
- `@pii` policy (#525);
- rename support for part names, a follow-up after #467;
- migration tooling for hand-joined ids;
- any change to scalar stream ids.

## Verification

**Done when:**
- a stream can declare a composite id, a command route can map each part, and a specification can state each part;
- both compilers parse, validate, print and round-trip these identically on shared conformance vectors;
- binding refuses composite routes with PLAY0268 until the admitting version;
- in that version, the C# formatter, the TypeScript formatter and the reference runner agree on a shared encode/decode vector set, and Stage and Arc's generator meet the preservation rule.

**Verify by:**

**Parser, validator and printer specs, in C# and TypeScript.**
- **Accepted:**
  - two-part and three-part streams;
  - literal parts;
  - specification parts in `given`, `when append` and `then`;
  - mapping lines in a different order from the declaration;
  - printer round-trips that keep declaration order.
- **Refused:**
  - a one-part block, a duplicate part, or a part with a modifier or child;
  - both forms on one stream, a duplicate header, an empty header, or wrong indentation;
  - unsupported part types;
  - a route or specification that misses a part, maps an unknown or duplicate part, puts a scalar mapping on a composite stream (or the reverse), or maps an incompatible property type;
  - empty text literals. Whitespace literals are accepted.
- **Unchanged meaning:**
  - command properties and event payloads named `streamId`;
  - parts named `streamId` or `stream`;
  - `@stream`;
  - PLAY0505 ambiguity;
  - `concurrency` dimensions;
  - an opaque scalar text id such as `"p-1:2026-10"`.

**Encode/decode vectors**, shared by C#, TypeScript and the runner.
- **Values:**
  - UUID case variants;
  - negative values, zero, `-0` and the integer bounds;
  - text containing `|`, `%`, a literal `%7C` and a literal `%257C`;
  - non-ASCII and supplementary characters;
  - normalization-equivalent strings, which stay distinct.
- **Decoder refusals:**
  - a lone surrogate (also refused when encoding);
  - lowercase escapes, unknown escapes, a trailing `%`, and wrong arity.
- **Round trip:** encode, decode and encode again gives the same bytes.
- **Order:** part order follows the declaration, not the mapping lines.

**Admitting version.**
- canonical vectors for a routed `given`, `when append` and `then` on a composite stream;
- a `then` that fails on one differing part;
- a `then` that passes with a UUID written in a different case;
- an atomic failure when a part fails to format;
- every earlier golden stays byte-identical.

**Downstream.**
- Stage renders the encoding.
- Arc's generator recovers parts only under the preservation rule and reports a loss otherwise.

## Consequences

- **Easier:**
  - modeling multi-value streams honestly;
  - generating `ICanProvideEventStreamId` without guessing;
  - writing and reading specifications for such streams;
  - getting a collision-free id within a schema without each application inventing one.
- **Harder:**
  - Routes and specifications for composite streams take one line per part under a header.
  - Tools that edit streams and routes (printer, AST, MCP, board) carry one more list.
  - Renderers, generators and transports must implement the encoding, the decoding and byte preservation exactly.
  - Key schema changes need a new stream identity or a migration.
- **Forecloses:**
  - composite stream ids typed by a shared general-purpose `type`;
  - a positional tuple spelling;
  - any other encoding for declared composites.
  Once ids are stored, changing the encoding means re-keying data, which is why this is costly to reverse.

## Verdict

The decider delegated this verdict to the orchestrating agent. The choices below were made under that delegation on 2026-10-08, after an independent cross-provider critique (GPT-6 Astra). That review accepted it with changes, and all its findings are folded in above.

1. **Spelling?** A, named parts under a `streamId` block, mapped by name in routes and specifications. It reuses existing line shapes, mirrors PDL's named key bindings, and keeps scalar syntax byte-identical.
2. **Encoding?** Each part formatted by the portable scalar rule, escaped `%`→`%25` then `|`→`%7C`, and joined with `|` in declaration order, with a strict decoder. It is injective within a schema and readable, and it avoids Chronicle#4131's collision.
3. **Evolution?** Key schema changes on stored streams require a new stored stream identity or an explicit migration. Encoded values cannot prove separation across schemas.
4. **Single-part blocks and empty text?** Both are refused: one spelling per identity, and one empty-value rule across stream ids.
5. **Comparison?** Canonical formatted part values under the same resolved stream, which is the same as comparing encoded ids.
6. **#459?** It is recommended to adopt name-based value binding. It is not bound by this decision.
7. **Version?** The authoring surface lands now, refused at binding. Execution joins #407's version or takes the next claimed one.

## Status notes

**2026-10-08.** Decision [0036](0036-admit-event-sources-streams-and-command-routes.md) narrows this decision's text domain for execution. Stream id text, scalar or a composite part, must be NFC, because executable model text is NFC. Non-NFC values are refused, never normalized. Normalization-equivalent spellings therefore cannot both appear, instead of staying distinct.
