# Event sources and streams

Name the business classification of an event source and its streams, then reference a stream from a command. This is **authoring-only**: these constructs are not admitted by any supported executable model (ESM) version yet; executable binding refuses them with `PLAY0268`. No stream routing, identity conversion or provider execution takes place.

## Declare a source and its streams

`eventsource` belongs to the application, including declarations in imported or placed files. A `stream` belongs to its exact physical source declaration. Both support one optional description and one rename-only `id "<old stored name>"` pin. New declarations omit pins. Pins do not create semantic identities or change the identity catalog.

| Member | Required | Meaning |
| --- | --- | --- |
| Source name | Yes | Exact, case-sensitive application-owned name |
| `identifier Type` | No | Nonoptional scalar identifier type; not a destination value |
| `stream Name` | No; repeatable | A source-owned stream declaration |
| Stream `streamId Type` | No | Nonoptional scalar key type; omission of both key forms declares an unkeyed stream |
| Stream `streamId` block | No | Two or more named scalar parts, in identity-bearing declaration order |
| `description` | No | Quoted text or an existing text/Markdown description fence |
| `id "OldName"` | No | Retains an old stored name during a deliberate rename |

Known stream-id types are `String`, `Uuid`, their nominal concepts and integer-backed concepts. Bare `Int`, enums, optional values, collections and other known value types are rejected. An imported type with an unavailable shape remains unresolved; tooling does not guess its primitive. Supported value types describe future portable formatting, not a formatter executed by this authoring increment.

## Composite stream ids

Use a `streamId` block when one stream is keyed by several values, rather than inventing a joined command property. Each child declares `<part> <Type>`. A stream declares either the scalar type or the block, never both.

- Declare at least two parts. Names are exact, case-sensitive and unique.
- Each part uses the scalar stream-id subset above. No optional or collection types, composite `type`s, enums, modifiers or children.
- A command route maps every declared part exactly once as `<part> = <source>` under its own `streamId` header. Sources are nominally compatible nonoptional command property paths or scalar literals. Unknown and duplicate parts are refused.
- A [specification route](specifications.md#event-routes-syntax-only) uses the same named mappings with concrete literals only. Empty text parts are refused; whitespace is not empty.
- Mapping order is authored presentation, not identity. Declaration order determines stored identity. Neither form supplies the event source id: `for` remains independent, even when a part has the same name or value.

**Evolution rule:** changing the key schema of a stream with stored events requires a new stored stream identity (a new stream name or its own `id` pin), or an explicit validated migration. Adding, removing or reordering parts, changing a part type, or switching between scalar and composite can split or alias existing ids. Renaming a part preserves stored ids only when its position, type and every mapping keep their meaning. Part-name rename is not supported by the authoring tools.

**Legacy hand-joined ids stay scalar.** An existing text id such as `"p-1:2026-10"` passes through unchanged. Screenplay never splits it or guesses parts; migration is explicit.

## Reference a stream from a command

This complete [authoring fixture](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/source-streams.play) declares scalar and composite keyed streams and maps command properties to them:

```screenplay
concept AccountId : Uuid
concept Month : Int

eventsource Account
  description "A customer account"
  identifier AccountId
  stream Transactions
    description "Account activity for one month"
    streamId Month
  stream Ledger
    streamId
      account AccountId
      month Month

module Banking
  feature Deposits
    slice StateChange Deposit
      command Deposit
        accountId AccountId identifier
        month Month
        amount Decimal
        stream Account.Transactions
          streamId = month
        produces event Deposited
          amount Decimal = amount
    slice StateChange Book
      command BookEntry
        accountId AccountId identifier
        month Month
        stream Account.Ledger
          streamId
            month = month
            account = accountId
        produces event EntryBooked
          for accountId
          month Month = month
```

A scalar keyed stream requires `streamId = <value>`; a composite stream requires the named part block. The forms cannot substitute for each other, and an unkeyed stream accepts neither. Known command paths must match the declared nominal type and be nonoptional scalars. Scalar literal values are checked against known types, but are not converted or executed.

`stream Account.Transactions` classifies the command's events. It does **not** supply `for`, change plain-production allocation, or override inline-production destination rules. A handler command may author a route even when its returned events are unavailable statically. The existing prohibition on combining `handler` and `produces` is unchanged.

## Canonical stored encoding

This is the **normative contract for future executable admission**, not a formatter executed by this authoring surface. Binding still refuses routes with `PLAY0268`.

1. Format each part: text unchanged and ordinal (no normalization), UUID lowercase and hyphenated, integer invariant decimal with no fraction or exponent and `0`, never `-0`.
2. Escape `%` as `%25`, then `|` as `%7C`. Only those two escapes are defined, with uppercase hex.
3. Join with `|` in declaration order. For example, the account/month fixture encodes as `3fa85f64-5717-4562-b3fc-2c963f66afa6|202610`; a text part `a|b%` encodes as `a%7Cb%25`.

Decode by splitting on literal `|` first, checking the declared arity, decoding each component exactly once (only `%25` and `%7C`), then checking canonical scalar spelling. Wrong arity, lowercase or unknown escapes, a trailing `%` and noncanonical scalar spelling are refused, never repaired. Generic URL decoding and decoding before splitting are incorrect. Lone UTF-16 surrogates are refused at literal binding or atomically at execution; runtime integer bounds follow the admitting execution contract.

Collision freedom holds within one declared schema, not across schemas or scalar ids. A scalar `"a|b"` can equal the encoding of composite parts `"a"` and `"b"`. Route identity includes source type, stream type and encoded id, with `for` independent; providers must disclose which dimensions they filter and never assume global stream-id uniqueness.

This is stored identity encoding, not transport or log escaping. Parts can contain Unicode, control characters, `/`, `?` or `#`; transport and logging must escape separately while preserving the identity bytes. The encoding is reversible and offers no confidentiality or erasure protection. Use non-sensitive surrogate identifiers. Formatting and decoding diagnostics must not disclose part values; `PLAY0515` rejects `@pii` and `@sensitive` concepts as scalar stream-id types, composite part types, and command route mapping sources, including nested property paths. Use a non-protected surrogate instead of personal data or an operational secret. A mapping using the same protected concept already rejected at its resolved declaration does not repeat that error; a different protected source concept is reported separately. Specification routes state literals, so their declaration check supplies the protection. This authoring rule implements [#525](https://github.com/Cratis/Screenplay/issues/525); it does not admit routing into an executable model.

## Ambiguity and ownership

Resolution uses the complete compilation input, independently of declaration or file order. A duplicate parent source makes **every** child owner ambiguous, even if only one duplicate contains the requested stream. A reference is exactly `Source.Stream`; arbitrary suffixes, foreign module prefixes and fuzzy names do not select an owner.

Properties named `stream`, `eventsource`, `from`, `streamId` and `identifier` remain legal. `stream String`, optional/collection/modifier property forms and `@stream Account.Transactions` are properties. Payload mappings named `stream` remain mappings. If both a unique stream reference and a known imported qualified value type are viable, `PLAY0505` blocks the model and retains both candidates. Choose the intended property with `@stream`. A [typed route selection](ast-authoring.md#source-and-stream-edits-syntax-only) can promote an exact retained route candidate while explicitly resolving the competing qualified type import/declaration in the same canonical proposal. Its source and stream must remain unchanged unique physical owners; unrelated reference removals and structural selection with `PreserveTrivia` are refused. Removing a retained JSON candidate alone cannot change the grammar's ambiguity. Neither interpretation resolving preserves legacy property syntax and its unresolved-type evidence rather than inventing a route.

## Rename a source or stream

Use MCP `propose-rename` with the source or stream's original `read-ast` handle, both expected revisions, `expectedName` and `newName`. Preview with `read-proposal` before `apply`. A source rename repairs the source member of command and specification routes; a stream rename repairs only routes bound to that stream under its source. Given, `when append` and then routes are included; `no stream` assertions stay unchanged.

Existing `id` pins stay untouched, even when the new name equals the pin. No pins are added automatically, and no source or stream identity-catalog entries are created or migrated. If an external stored name matters, add `id "OldName"` with a typed edit before renaming. `eventNeverPersisted` has no effect on source or stream renames.

Colliding names, duplicate physical source declarations, captured route debt and affected opaque references refuse the proposal. The default `PreserveTrivia` patches only proved identifier spans; canonical formatting does not waive reference safety.

## Tooling support

| Surface | Supported in this increment | Not available |
| --- | --- | --- |
| C# and TypeScript syntax | Declarations, command routes, key mappings, strict typed JSON, full-input ambiguity validation | Not admitted by any supported executable model (ESM) version yet |
| Monaco and VS Code | Typed symbols, contextual tokens, reference/key completion, hover and exact source navigation where the host has authoritative source | Guessed effective routing, automatic source/stream rename, routing quick fixes |
| MCP | Paged source/stream inventories, exact owner keys, physical AST handles, type and route links, Authoring add/replace/remove, `propose-rename` with command and specification route repair | Source/stream semantic or requirement IDs, execution or identity refactors |
| Board | Authored stream and readable key expression in existing command details | Stream event cards, inferred facts or successful routed specification states |
| Documentation fixture | Source validation and explicit semantic refusal | Runtime sample or proof of routing execution |

Source queries expose original source locations, not merged document line numbers. Physical read inventories include retained declarations and route candidates from errorful documents, separately from write eligibility. Parser and whole-assembly diagnostics remain visible; unknown parsed extent reports incomplete ownership rather than a unique survivor. Unresolved or conflicting import placement does not yield confident navigation. [Typed edits](ast-authoring.md#source-and-stream-edits-syntax-only) and [MCP inventories](mcp/authoring-tools.md#event-source-and-stream-authoring) retain revision checks, preview and explicit acceptance.

Per-production overrides, reaction/reducer `from` filters, new concurrency flags, occurrence-time routing and constraint scopes are later increments. Existing `concurrency` remains distinct from routing; omission retains its existing meaning. See [Commands](commands.md) and the [grammar](grammar.md).
