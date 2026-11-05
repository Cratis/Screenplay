---
title: "Screenplay event sources and streams: route command events"
description: "Declare Screenplay event sources and streams to route command events with scalar or composite keys and filter reactions by stored routing names."
---

# Event sources and streams

In Cratis Screenplay, event sources and streams classify where a command's events belong. Declare a source for a business category and streams within it, then reference a stream from the command. The route classifies the events; `for` independently selects the event source instance that receives them.

For order fulfilment, keep that storage question separate from automation: routing a payment fact does not start fulfilment by itself. A [reaction](reactions.md) can observe matching facts, while a [state machine](statemachines.md) follows the order's lifecycle and a [workflow](workflows.md) describes the process that continues from it. The banking example below shows the routing mechanics with account and month keys.

The executable semantic model (ESM) admits source declarations, scalar and composite command routes, and specification routes from v8. ESM v10 adds production route overrides and observer filters. Models without these forms keep their existing versions, canonical bytes and outcomes. A renderer or other consumer must explicitly admit the contract; updating its package alone does not establish routing support.

## Declare a source and its streams

An `eventsource` belongs to the application, even in an imported or placed file. A `stream` belongs to its exact physical source declaration. Both accept one optional description and one rename-only `id "<old stored name>"` pin. Omit pins on new declarations.

Sources and streams have catalog identities independent of file placement. Their stored names (`sourceKind` and `streamKind`) come from the `id` pin when present, otherwise from the declaration name. Stored source names must be unique application-wide, and stored stream names unique within a source. This includes pin-versus-name collisions; comparison is ordinal.

Do not use exactly `Default` as a stored source name or exactly `All` as a stored stream name, including through `id "All"`. Both authoring compilers report `PLAY0802` at the declaration, with `PLAY0273` retained as a C# binder backstop. Although the ESM contract admits those names, the compiler and Cratis renderer refuse them; executable validation and MCP readiness report the refusal. Streams stored as `all`, `ALL` or `Default` remain valid.

| Member | Required | Meaning |
| --- | --- | --- |
| Source name | Yes | Exact, case-sensitive application-owned name |
| `identifier Type` | No | Nonoptional scalar identifier type; not a destination value |
| `stream Name` | No; repeatable | A source-owned stream declaration |
| Stream `streamId Type` | No | Nonoptional scalar key type; omission of both key forms declares an unkeyed stream |
| Stream `streamId` block | No | Two or more named scalar parts, in identity-bearing declaration order |
| `description` | No | Quoted text or an existing text/Markdown description fence |
| `id "OldName"` | No | Retains an old stored name during a deliberate rename |

Use `position <x> <y>` to place a source on the event source canvas. It takes two whole numbers, including negative numbers, and may appear once anywhere in the source body. This is authoring metadata, not part of the executable model. The canvas lays out sources that have no position.

```screenplay
eventsource Product
  description "One product in the catalogue"
  identifier ProductId
  position 110 150

sticky note on eventsources
  description "Open question: should a product have one stream per market?"
  colour pink
  position 480 150
  size 240 140
```

`sticky note on <canvas>` is the free-standing note of a domain canvas. `<canvas>` is `personas`, `eventsources`, `givencontexts` or `seeddata`; a note needs a `description` and a `position`, takes an optional `colour` (`yellow` is the default and never printed, `pink`, `green`, `blue`) and `size`, and is never part of the executable model.

Known stream-id types are `String`, `Uuid`, their nominal concepts and integer-backed concepts. Bare `Int`, enums, optional values, collections and other known value types are rejected. An imported type with an unavailable shape remains unresolved; tooling does not guess its primitive. Executable routes use the same portable formatter as authoring validation.

## Composite stream ids

Use a `streamId` block when one stream is keyed by several values, rather than inventing a joined command property. Each child declares `<part> <Type>`. A stream declares either the scalar type or the block, never both.

- Declare at least two parts. Names are exact, case-sensitive and unique.
- Each part uses the scalar stream-id subset above. No optional or collection types, composite `type`s, enums, modifiers or children.
- A command route maps every declared part exactly once as `<part> = <source>` under its own `streamId` header. Authoring accepts nominally compatible nonoptional command property paths or scalar literals. Executable admission accepts direct, required, non-collection, non-generated command properties or literals; property paths remain refused with `PLAY0268`, and generated mappings with `PLAY0273`. Unknown and duplicate parts are refused.
- A [specification route](specifications.md#event-routes) uses the same named mappings with concrete literals only. Empty text parts are refused; whitespace is not empty.
- Mapping order is authored presentation, not identity. Declaration order determines stored identity. Neither form supplies the event source id: `for` remains independent, even when a part has the same name or value.

**Evolution rule:** changing the key schema of a stream with stored events requires a new stored stream identity (a new stream name or its own `id` pin), or an explicit validated migration. Adding, removing or reordering parts, changing a part type, or switching between scalar and composite can split or alias existing ids. Renaming a part preserves stored ids only when its position, type and every mapping keep their meaning. Part-name rename is not supported by the authoring tools.

**Legacy hand-joined ids stay scalar.** An existing text id such as `"p-1:2026-10"` passes through unchanged. Screenplay never splits it or guesses parts; migration is explicit.

## Reference a stream from a command

In this complete [authoring fixture](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/source-streams.play), `Transactions` groups account activity by month. `Ledger` uses an account-and-month composite key. Both routes map existing command values rather than generating a new destination:

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

A scalar keyed stream requires `streamId = <value>`; a composite stream requires the named part block. The forms cannot substitute for each other, and an unkeyed stream accepts neither. Known command paths must match the declared nominal type and be nonoptional scalars. Scalar literals and composite literal parts are checked against known types through the shared stream-id formatter. Text must be nonempty, Unicode NFC and free of lone UTF-16 surrogates; whitespace is accepted. Double-mode integer literals must be within ±9007199254740991. Exact-mode integers have no formatter bound. Invalid values are refused, never normalized or rounded.

In `Deposit`, `stream Account.Transactions` classifies the events and `streamId = month` selects the monthly stream key. In `BookEntry`, the mappings are written month-first, but the stored composite id still follows the declaration's account-then-month order. `for accountId` independently selects the event source instance.

A route does not supply `for`, change plain-production allocation or override inline-production destination rules. A routed command's identifier and each known production destination type must match the source's nominal identifier type; a mismatch reports `PLAY0504`. An allocated destination uses the generated identifier's type when declared, otherwise the source identifier must be UUID-backed. A handler command may author a route even when its returned events are unavailable statically, but it still cannot combine `handler` with `produces`.

## Production route overrides

A command event production may declare one `stream Source.Stream` line. Its scalar or composite `streamId` mappings use the command-route rules. The override replaces the source, stream and stream id together. Other productions keep the command route. An override never supplies `for`, and its destination type must match its source's identifier type.

Plain, conditional and inline event productions accept overrides. A line with `=` remains payload, including a payload named `stream`. Reaction and refusal-branch productions reject routes with `PLAY0663`. Duplicate and invalid routes use `PLAY0504`. An identical override warns with `PLAY0664`; a typed repair removes it.

There is no `no stream` override. An unkeyed override under a keyed command is valid portable semantics. Stage must refuse it until its Arc adapter can prevent sentinel fallback from inheriting the command stream id. Screenplay replaces the whole route and does not inherit that id.

[Invoicing](https://github.com/Cratis/Screenplay/blob/main/Samples/Invoicing/invoicing.play) routes recorded payments to `Payment.Transactions` and cash audit facts to `Payment.Audit`. Overrides select ESM v10. All routes resolve eagerly, even for skipped conditional productions.

## Observer filters

Use `from Source` or `from Source.Stream` on a reaction or reducer when it should observe only facts with matching stored routing names. For example, a reaction filtered to `Payment.Transactions` does not also observe facts routed to `Payment.Audit`. The filter is one leaf line and never filters stream ids; unrouted facts do not match. Omitting it leaves observation unchanged.

A filtered reaction uses only declared-event triggers. These metadata filters do not apply to projections or captures.

`PLAY0665` rejects malformed, duplicate, child-bearing or unresolved filters. The C# compiler warns with `PLAY0666` when every statically known producer lands outside the filter. Command productions use their effective routes. Reaction productions, captures and public publication are unrouted. Handler and foreign origins are unknown and suppress the warning.

Filters select ESM v10. The reference runner applies them to given history and newly appended facts. Reaching a matching opaque reducer still returns Unsupported; routing does not execute its code.

## Canonical stored encoding

Scalar ids use the portable scalar formatting rule without composite escaping. Composite ids use the following stored encoding in authoring validation and reference execution.

1. Format each part: nonempty, well-formed Unicode NFC text unchanged and ordinal (non-NFC text is refused, never normalized), UUID lowercase and hyphenated, integer invariant decimal with no fraction or exponent and `0`, never `-0`.
2. Escape `%` as `%25`, then `|` as `%7C`. Only those two escapes are defined, with uppercase hex.
3. Join with `|` in declaration order. For example, the account/month fixture encodes as `3fa85f64-5717-4562-b3fc-2c963f66afa6|202610`; a text part `a|b%` encodes as `a%7Cb%25`.

Decode by splitting on literal `|` first, checking the declared arity, decoding each component exactly once (only `%25` and `%7C`), then checking canonical scalar spelling. Wrong arity, lowercase or unknown escapes, a trailing `%` and noncanonical scalar spelling are refused, never repaired. Generic URL decoding and decoding before splitting are incorrect. Lone UTF-16 surrogates and non-NFC text are refused in authored literals. Double-mode integer literals are bounded to ±9007199254740991. Runtime formatting follows the route phase described below.

Collision freedom holds within one declared schema, not across schemas or scalar ids. A scalar `"a|b"` can equal the encoding of composite parts `"a"` and `"b"`. Route identity includes source type, stream type and encoded id, with `for` independent; providers must disclose which dimensions they filter and never assume global stream-id uniqueness.

This is stored identity encoding, not transport or log escaping. Parts can contain Unicode, control characters, `/`, `?` or `#`; transport and logging must escape separately while preserving the identity bytes. The encoding is reversible and offers no confidentiality or erasure protection. Use non-sensitive surrogate identifiers. Formatting and decoding diagnostics must not disclose part values; `PLAY0515` rejects `@pii` and `@sensitive` concepts as scalar stream-id types, composite part types, and command route mapping sources, including nested property paths. Use a non-protected surrogate instead of personal data or an operational secret. A mapping using the same protected concept already rejected at its resolved declaration does not repeat that error; a different protected source concept is reported separately. Specification routes state literals, so their declaration check supplies the protection. This authoring rule implements [#525](https://github.com/Cratis/Screenplay/issues/525); it applies independently of executable admission.

## Route phase and failure

A command resolves its command route and all production overrides eagerly, after declarative validation and requirements, before generation and productions. A reaction-invoked command uses the same phase; captures and a reaction's direct productions stay unrouted. Each production uses its override when present, otherwise the command route. Formatting failure in a skipped production rejects the command atomically. Routing does not change allocation, authorization, constraints, projections or queries.

Formatting-only failures, such as empty text or an integer outside the Double bound, reject the failing command as `Contract`: no allocation, facts or response. Authorization denial and validation failure take precedence. Non-NFC or ill-formed direct inputs already fail request type validation, before declarative rules; nothing normalizes them silently. Previously accepted cascade facts remain after a later command fails.

Arc computes stream ids before authorization. That ordering is **not equivalent** to this portable phase: renderers must preserve Screenplay's precedence rather than let a formatting failure mask a denial. Unrouted facts carry no route; Chronicle's `Default`, `All`, `"Default"` triple is never materialized in the executable model.

## Ambiguity and ownership

Resolution uses the complete compilation input, independently of declaration or file order. A duplicate parent source makes **every** child owner ambiguous, even if only one duplicate contains the requested stream. A reference is exactly `Source.Stream`; arbitrary suffixes, foreign module prefixes and fuzzy names do not select an owner.

Properties named `stream`, `eventsource`, `from`, `streamId` and `identifier` remain legal. `stream String`, optional/collection/modifier property forms and `@stream Account.Transactions` are properties. Payload mappings named `stream` remain mappings. If both a unique stream reference and a known imported qualified value type are viable, `PLAY0505` blocks the model and retains both candidates. Choose the intended property with `@stream`. A [typed route selection](ast-authoring.md#source-and-stream-edits) can promote an exact retained route candidate while explicitly resolving the competing qualified type import/declaration in the same canonical proposal. Its source and stream must remain unchanged unique physical owners; unrelated reference removals and structural selection with `PreserveTrivia` are refused. Removing a retained JSON candidate alone cannot change the grammar's ambiguity. Neither interpretation resolving preserves legacy property syntax and its unresolved-type evidence rather than inventing a route.

## Rename a source or stream

Use MCP `propose-rename` with the source or stream's original `read-ast` handle, both expected revisions, `expectedName` and `newName`. Preview with `read-proposal` before `apply`. A source rename repairs command routes, production overrides, observer filters and specification routes. A stream rename repairs only references bound to that stream under its source. Given, `when append` and then routes are included; `no stream` assertions stay unchanged.

Existing `id` pins stay untouched, even when the new name equals the pin. No pins are added automatically. Catalog entries migrate atomically, including all streams owned by a renamed source. An unpinned rename preserves catalog identity but changes the stored name and semantic revision. **Before renaming a source or stream with stored events, add `id "OldName"` with a typed edit** to preserve its stored identity, except for the reserved stream name `All`. `eventNeverPersisted` has no effect on source or stream renames.

**Migrate a stream stored as `All`.** Rename it with `screenplay_propose_rename` (`propose-rename`), then review the proposal before applying. Do not add or keep `id "All"`: the pin preserves the reserved stored name and still reports `PLAY0802`. If a pin already exists, remove or replace it with a typed edit. Chronicle cannot distinguish old events stored under stream type `All` from source-only appends. Treat that history as source-only and read it through a source-only route or as unrouted facts in the consumer; Screenplay does not admit source-only route syntax. Moving that history to the new stream name requires an explicit Chronicle data migration, not a rename pin.

To rename a composite id part, select its `EventStreamIdPartSyntax` handle instead. The proposal updates the declaration and bound part mappings in command routes, specification routes, examples and redelivery locators. Declaration and mapping order, types, values, encoded stored ids and catalog identities remain unchanged. A name already used by another part of that stream or an unproven mapping refuses the proposal.

Colliding names, duplicate physical source declarations, captured route debt and affected opaque references refuse the proposal. The default `PreserveTrivia` patches only proved identifier spans; canonical formatting does not waive reference safety.

## Tooling support

| Surface | Supports | Not available |
| --- | --- | --- |
| C# and TypeScript syntax | Declarations, command and production routes, observer filters, key mappings, strict typed JSON, full-input ambiguity validation | Executable binding is provided by the C# semantic compiler |
| Monaco and VS Code | Typed symbols, contextual tokens, reference/key completion, hover and exact source navigation where the host has authoritative source | Guessed effective routing, automatic source/stream rename or route selection |
| MCP | Paged source/stream inventories, physical AST handles, executable export, source/stream rename, typed redundant-production-route removal | Automatic route selection |
| Board | Command route, each production override, named lookup parts and observer descriptions | Stream event cards, inferred facts or successful routed specification states |
| Reference execution | Canonical routed facts and specification route comparisons | Provider execution without explicit consumer admission |

Source queries expose original source locations, not merged document line numbers. Physical read inventories include retained declarations and route candidates from errorful documents, separately from write eligibility. Parser and whole-assembly diagnostics remain visible; unknown parsed extent reports incomplete ownership rather than a unique survivor. Unresolved or conflicting import placement does not yield confident navigation. [Typed edits](ast-authoring.md#source-and-stream-edits) and [MCP inventories](mcp/authoring-tools.md#event-source-and-stream-authoring) retain revision checks, preview and explicit acceptance.

Routing does not admit direct-production routes on reactions, new concurrency flags, occurrence-time routing, constraint scopes, first-append rules or stream closing. Property-path stream-id mappings remain refused under #574. Existing `concurrency` is separate from routing, and omission keeps its existing meaning.

Continue with [Commands](commands.md) to define destinations and productions, or [Reactions](reactions.md#observer-filters) to act on routed facts. The [grammar](grammar.md) lists the accepted forms.
