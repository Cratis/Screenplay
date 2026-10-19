# Commands

Commands are input definitions — imperative intents. A command declares its properties, its authorization, its validation rules, and what events it produces.

## Syntax

````screenplay
command <Name>
  [description "<text>"]

  <property> <Type> [optional] [generated] [identifier]
  ...

  [reads <ReadModel> [as <alias>] [by <property>]]   ← state the command decides against
  ...

  [authorize <PolicyName> [<PolicyName>]*]

  [validate
    <rule> [severity information|warning|error] [message "<message>"]
    require <condition>              ← a rule about the command as a whole
      [severity information|warning|error]
      [message "<message>"|$strings.<key>]
    ...]

  [validate <inline csharp block yielding messages for broken rules>]

  [stream <EventSource>.<Stream>   ← event classification
    [streamId = <value>]]

  [produces ...]                  ← declarative — repeatable

  [handler                        ← file/code fallback, or pending intent beside produces
    file <Path>
    | <inline csharp block returning events to append>]

  [concurrency                    ← optional concurrency scope
    [eventSource]
    [sourceType <Name>]
    [streamType <Name>]
    [streamId <Name>]
    [events <EventType>[, <EventType>]*]]
````

## Description

An optional `description` is the first body line of a command — a human-readable summary consumers such as Prologue surface when presenting the model. At most one per command. Use the quoted form for a single line:

```screenplay
command RegisterInvoice
  description "Registers a new invoice with its lines and payment terms"
```

When one line is not enough, use a fenced block — the same ``` convention as inline code blocks. The fenced text is kept verbatim:

````screenplay
command RegisterInvoice
  description
    ```text
    Registers a new invoice with its lines and payment terms.
    The invoice starts out as a draft.
    ```
````

Commands also accept one nonempty fenced `markdown` `documentation` block for reasoning and assumptions. It is report-only (`PLAY0270`), with no effect on executable bytes. See [Descriptions and documentation](slices.md#descriptions-and-documentation).

Command descriptions use `text` fences, not `markdown`; Markdown description fences are available only on events. Descriptions work the same on modules, features, slices, and personas — see [Descriptions](slices.md#descriptions).

## Declare an event inline

Use `produces event <Name>` to declare a new event where the command produces it. Each payload property declares its type and mapping together:

```screenplay
command Rename
  projectId Uuid identifier
  name String
  produces event Renamed
    description "The project acquired another name"
    tag audit
    name String = name
```

Here the omitted `for` means `for projectId`; the identifier is **not** copied into the payload. The declaration belongs to the slice, so extracting it into a standalone `event Renamed` plus `produces Renamed` with explicit `for projectId` preserves its identity and canonical ESM bytes. Declarations remain order-independent. A plain `produces Renamed` still references a declared event rather than creating one.

Each inline payload property is declared once; repeating its name reports `PLAY0168`. Inline events are generation 1 only, local to the command, and cannot declare `generation` or `origin`. Reactions cannot declare inline events. Names must not collide with another standalone, imported, or inline event. `tag` inside this block is an **event-type** tag, unlike a tag on plain `produces`. [Event metadata](events.md#authoring-metadata) covers descriptions, documentation, and rename-only identity pins.

### Appending to several event sources

A command can append events to more than one event source. A production's `for` may name the command identifier or any other required, non-collection command property, and the events of one accepted command are appended together:

```screenplay
module Sales
  feature Orders
    slice StateChange PlaceOrder
      command PlaceOrder
        orderId String identifier
        productId String
        quantity Int
        produces OrderPlaced
          for orderId
          orderId = orderId
          productId = productId
        produces StockReserved
          for productId
          productId = productId
          quantity = quantity
      event OrderPlaced
        orderId String
        productId String
      event StockReserved
        productId String
        quantity Int
```

`OrderPlaced` is appended to the order's event source and `StockReserved` to the product's. A command specification states each destination with `for` on each `then` event. Optional and collection properties still cannot name a destination. A command that uses a destination other than its identifier selects language/semantic `30.0`; models using none keep their existing version and bytes. Chronicle appends each event to its own event source; this is what the reference evaluator proves, and a consumer must not assume cross-source atomicity beyond what its event store provides.

If any production targets a source other than the command identifier, **every production must state `for` explicitly** (`PLAY0470`). The same rule applies when an inline production and a plain production both omit `for`: their defaults differ. The diagnostic names the production that needs a destination; adding an inline event never silently retargets a plain sibling. The compiler compares authored paths, not runtime values. Commands without a generated identifier still cannot bind a destination other than their identifier. In ESM v7 a command with a generated identifier may explicitly route a production to another required scalar command property; its response may still return the generated identifier. This does not admit named sources or streams. An inline omission without a required scalar identifier cannot bind. The [explicit-intent workspace/MCP repair](mcp/authoring-tools.md#establish-production-destinations) requires a destination selection for every occurrence and confirmation of unchanged named routes. It fills missing `for` expressions only, binds the whole candidate and retains a routing report for individual review before explicit apply. These invalid mixed-destination models have no executable before-baseline, so the repair establishes the author's routing rather than proving preservation. There is no automatic editor action or fix-all; never assume an omitted plain production meant the identifier.

Plain `produces <Name>` retains its legacy omission behavior; it does not acquire the inline default. Copying an identifier into a same-source payload reports `PLAY0469`: warning for inline declarations, information for plain productions with explicit `for`. Review the persisted contract before removing a payload field. The inline-only typed repair removes the property and mapping together, retires the property's semantic address, and is labeled “changes the event contract”. It is excluded from fix-all. The narrow repair refuses other consumers of that event, opaque implementation impact, routing changes and comment loss; standalone/plain contracts receive guidance only, not an automatic shape change. To keep the stored contract instead, remove the copy with `propose-property` (`action: "remove"`, `newGeneration: true`, `CanonicalizeTouchedDocuments`), which drafts generation N+1 without the copy and extracts an inline event in the same proposal. See [event generations](events.md).

The unescaped directives `namespace`, `sequence`, `correlation`, `causation`, `causedBy`, and `occurred` are reserved system-assigned metadata in production bodies. Escape a genuine payload field, for example `@sequence String = name`; `occurred at` is not supported yet.

## Command stream routing

Use `stream Source.Stream` to select a declared [event source and stream](event-sources.md). Map a scalar keyed stream with nested `streamId = <value>`. For a composite stream, nest a bare `streamId` header and one `<part> = <source>` mapping for every declared part, exactly once. Mapping order is free; the printer retains it. Each source is a nominally compatible nonoptional command path or scalar literal. Literals are checked like any stream id: text must be non-empty, well-formed and NFC, and an integer must lie within ±(2^53−1) in the default Double numeric mode. The command's identifier and each event's destination must have the source's identifier type, otherwise `PLAY0504` is an error. Scalar and composite mapping forms cannot substitute for one another. This route never supplies the event's `for` destination and does not change plain-production allocation or inline defaults. Handler commands may author routing without statically declared events; `handler` with `produces` is still prohibited.

Both viable stream/property interpretations remain blocking `PLAY0505` candidates, never a guessed route. Source/stream declarations and routed commands select executable semantic model **v8**. Executable mappings read direct, required, non-collection, non-generated command properties or literals. Paths remain refused with `PLAY0268`; generated mappings are refused with `PLAY0273`. A command event production may override the whole route with its own `stream Source.Stream` and scalar or composite `streamId` mappings. Other productions keep the command route. Overrides and reaction/reducer filters select ESM v10. New concurrency flags remain unadmitted. The board lists each authored production override in command details.

The command route and every production override resolve eagerly after declarative validation and requirements, before generation and productions. This includes conditional productions that will not run. Formatting failure rejects the failing command as `Contract` with no allocation, append or response; authorization and validation take precedence. Invalid non-NFC direct inputs still fail earlier at request type validation. Arc computes the stream id before authorization, so its native ordering is not equivalent; renderers must preserve this portable precedence. See [route phase and failure](event-sources.md#route-phase-and-failure).

## Operations and external systems (syntax-only)

A command can describe external effects through inline `produces operation <Name>` declarations or plain references to standalone operations. [Operations and external systems](operations.md) covers typed inputs, `uses`, execution/compensation intent and manual promotion. Event and operation productions stay in one ordered sequence, but operations do not participate in event destinations or payload identity diagnostics. **Operations are not admitted by any supported executable model (ESM) version yet**; binding reports `PLAY0268` without an executable model.

## Generated values and responses

Generated command properties, responses, generated fixtures and return expectations select at least **ESM v7**. A serialized concept generation strategy selects **ESM v15**, including fixture shape validation and Guarded freshness; see [Generated values](generated-values.md). Commands generate values after authorization and validation, then use complete values in productions and responses. A response exists only on acceptance; rejection, denial or an unsupported outcome has no response. A later reaction or scenario-query failure retains already accepted facts but clears the response. Syntax validation alone still does not prove execution or downstream rendering.

```screenplay
command RegisterProject
  projectId ProjectId generated identifier
  receiptId ReceiptId generated
  name ProjectName
  returns
    projectId = projectId
    receiptId ReceiptId = receiptId
```

This fragment assumes `ProjectId` and `ReceiptId` are concepts backed by `Uuid`, and `ProjectName` is a concept backed by `String`. The [complete example](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/generated-responses.play) includes their declarations and passing specifications asserting both emitted events and responses. [Invoicing](https://github.com/Cratis/Screenplay/blob/main/Samples/Invoicing/invoicing.play) shows scalar and record responses in its wider language showcase.

- `generated` is command-only and requires a required scalar generatable concept: Uuid has implicit uuid, while String needs an admitted `generate` strategy. Bare primitives, optional values and collections are invalid. Generated values are not request inputs, form fields, invocation arguments or ordinary specification inputs.
- Modifier order is `Type optional generated identifier`. This order does not permit an optional generated value or optional identifier. `generated String` still declares a property named `generated`.
- `returns <property>` declares one scalar response. Bare `returns` opens an unnamed record, even when it has just one field. Declare at most one unconditional response, as a command sibling, not inside a production.
- Each record field is `<name> [<Type>] = <property>`. Its type is inferred from a direct command property, or explicitly annotated with exactly the same type identity and optionality. Fields are unique and stay in authored order. Arithmetic, read aliases, collections and whole read-model responses are not supported.
- A two-token `returns name` is a response when `name` identifies another property of **this command**, regardless of declaration order; otherwise it remains a property named `returns` whose type is `name`. This is a local property lookup, not a type-inventory or capitalization rule. Use `returns @name` to force a source reference, including an unknown source that needs a diagnostic; use `@returns Type` to force a property declaration. Returning a property named `returns` uses `returns @returns`.

Authorization policies (including inherited/composed policies and the implicit subject of a generated identifier), property rules and requirements cannot reference generated values: binding reports `PLAY0273`, because those values do not exist before generation. Rules on a generated property's concept select ESM V20, including implicit UUID generation. Proven rules bind silently; Refuted rules fail with `PLAY0688` and a quoted witness; Unproven rules warn with `PLAY0689` and a reason. Generated fixtures must satisfy shape and rules after generation; failures are `Rejected(Contract)`, never client validation errors or silent redraws. Opaque named/code predicates remain Unsupported in the reference evaluator. Explicit generation with compliance markers remains refused; legacy implicit-UUID non-identifier compliance keeps its existing admission. Opaque pre-generation contexts expose input properties only and keep their existing `Unsupported` execution behavior.

ESM v21 adds pattern, random and implementation generation. Numeric fixtures and their return expectations use integral numbers within the inclusive Int64 strategy range, not quoted strings. Pattern date segments compare against the same UTC `given clock` instant; without it, only digit widths are checked. Checksum arithmetic is enforced only by an authored String-concept checksum rule. Every supplied fixture is checked before missing-fixture completeness: invalid entries reject Contract; a missing reached fixture is Unsupported(IdentityAllocation). Generation follows authorization and validation; there is no reference RNG, seed, body execution or redraw. See [patterns and checksums](generated-values/patterns.md).

A generated identifier supplies an inline production's implicit destination. Plain `produces` without `for` keeps its separate legacy allocation channel; a generation fixture never satisfies that allocation. A response-only command records no facts. No response differs from a scalar response whose optional source is `Null`. Record fields preserve authored order and have external contract names, not independent semantic identities.

Responses also parse beside a handler. ESM v23 admits described command handlers with unconditional event productions; response-only pending handlers are refused (`PLAY0696`). File/inline handlers, operations and exact numeric mode remain unadmitted independently. The compiler does not emit a renderer's `<Command>Response` type. The compilers and editors resolve returned names in form `on submit` and command `on success` continuations; renderers bind the actual response values. Using a returned name under that command's `on failure` reports `PLAY0685`, unless an existing local binding or an outer successful response supplies it. See [continuation values](interactions.md#returned-values-in-continuations). This authoring rule adds no ESM fields and does not deliver end-to-end navigation. Generated values provide no idempotency, retry or deduplication guarantee.

The board excludes generated properties from the request schema and shows generated/response details in the existing command description. It creates no response event or response identity. See [specification fixtures and return assertions](specifications.md#generated-fixtures-and-return-expectations).

## The identifier

Every command that changes state changes the state of *something*. `identifier` marks which property names it:

```screenplay
command RegisterInvoice
  invoiceId     InvoiceId identifier
  invoiceNumber InvoiceNumber
```

The identifier names the command's event source; a `produces` that states [`for <identifier>`](#where-an-event-lands) appends to it. The event source id is never event payload. Leave it out and the code Stage renders for Arc lets Arc allocate a fresh `Uuid` instead — which is right for a command that creates something whose identity the caller does not supply. The executable model does not allocate one: it needs an allocated identity from the execution request, or an explicit `for`, and reports `IdentityAllocation` otherwise ([see below](#where-an-event-lands)):

```screenplay
command ArchiveOldInvoices
  olderThan Date
```

The trailing `subject` role belongs only to [event properties](events.md#data-subject), including inline events before `=`. Commands and response fields refuse it (decision 0008); it is report-only metadata and does not promise provider output.

**At most one property per command** may be the identifier; a second one is a compile error, because there is no sensible way to choose between them. The modifier belongs to commands only — an event never carries its own event source id (it is implicit in the event context), so `identifier` on an event property is an error too.

## What the command reads

A command that changes state usually has to consult state first — whether the month is already started, which phase an engagement is in, who the consultant on a scope is. `reads` declares that dependency:

```screenplay
command StartMonth
  engagementId EngagementId identifier
  year         TimesheetYear
  month        TimesheetMonth
  reads EngagementScope by engagementId

  produces TimesheetStarted
    for engagementId
    consultantId = EngagementScope.consultantId
```

This is the read-model-to-command arrow of Event Modeling — one of the four the method is built on, and the only one a document could not draw. Without it, a command that decides against state shows its inputs and its events but not what it consulted in between, and the mapping fed from that state has nowhere to come from.

- `<ReadModel>` names a read model some [projection](projections/index.md) produces. Reading something no projection produces is a warning — the document says it depends on state nothing in it explains.
- A `reads` declaration may have one child `by` header with at least two named `part = source` mappings. It cannot combine with inline `by`. Every part must name a key property, and every source must be a required, noncollection property path of its compatible nominal type. Literals are refused. Other children remain errors.
- A single-instance read supplies every declared key part exactly once. Missing parts use `PLAY0661`. Its typed repair covers command reads only when every missing part has one same-named compatible input. Queries and trigger reads are not repaired. See [Read-model keys](readmodels.md#keys).
- `by <property>` names the command property the read model is looked up by, and must be one of the command's own properties. Under `command reads protected`, leaving it out uses the command's declared identifier. Singleton/unkeyed views remain authoring-only and are refused by protected admission. The name starts with a lower-case letter or underscore, followed by letters, digits or underscores.
- `as <alias>` distinguishes instances of the same view. If a command reads a view more than once, **every** instance needs an alias. Aliases start with a lower-case letter or underscore, followed by letters, digits or underscores; `as`, `by` and `reads` are reserved. Aliases must be unique in the command and must not match any of its property names. A single read may also be named by an alias.

For example, a transfer can name both accounts without conflating their state:

```screenplay
command TransferFunds
  sourceId Uuid
  destinationId Uuid
  reads Account as source by sourceId
  reads Account as destination by destinationId
  validate
    require source.balance > 0
      message "Source account must have funds"
```

An unaliased single read uses its view name in a `require` path. If you declare an alias, use that alias instead; repeated reads must use their aliases, never the ambiguous view name. The same single-level paths are addressable in produced-event mappings, as above.

These paths bind under the application-root `command reads protected` preamble when every referenced read is admitted. `PLAY0784` names an unknown read/property path; `PLAY0785` requires an alias for an ambiguous view name. With an alias, use that alias as the read name.

### What `reads` guarantees

Application-root `command reads protected` opts the composed application into protected command reads (ESM v25), under [decision 0085](https://github.com/Cratis/Screenplay/blob/main/decisions/0085-admit-protected-command-reads-through-an-explicit-preamble.md), amending 0017. Declare it once, in the root document; imported commands inherit it. Duplicate, conflicting or non-root declarations report `PLAY0780`. Without it, legacy command reads retain `PLAY0271`, read paths retain `PLAY0268`, and unchanged models keep their bytes and version. A later version alone does not opt a model in.

A protected read must meet all of these conditions:

- Its view is built from a finite, non-empty set of event-log event types, with removal handling, keyed only by event-source identity.
- Its lookup is a required, scalar, non-generated command input of type String, a String concept or canonical GUID. Omitted `by` uses the command's declared identifier; no identifier reports `PLAY0782`.
- Repeated reads use distinct aliases. A command cannot combine reads and `concurrency` (`PLAY0783`).

`PLAY0781` refuses reducers, joins and removal joins, children/nested scopes, open subscriptions (`every`/`all`), payload keys, composite keys (`ByParts`), missing removal handlers, empty handled event sets, named sources/streams, singleton views and unsupported, generated, optional or collection keys. The diagnostic carries the closed reason; see [protected-read diagnostics](diagnostics.md#protected-command-reads). `optional`, `advisory` and `via query` remain refused. There is no unguarded fallback. Aliases qualify single-level `read.property` paths in requirements and produced-event mappings only; deeper paths and other positions remain `PLAY0268`.

Execution orders authorization → property rules → required reads → derive/provide → requirements → productions/responses. Missing required state rejects with `Validation` and message `<ReadModel> '<key value>' does not exist.` before any requirement or mapping is evaluated. A property-rule rejection therefore takes precedence over absence. Optional view properties follow the existing optional comparison and mapping rules.

Decide records the unfiltered fact-log boundary and each read's key plus the view's handled event types; reads of the same key merge. A matching fact after the boundary makes commit return `Conflict` / `DecisionStateChanged`, with no facts from the command. Every intervening fact is considered, including publications. An irrelevant fact does not conflict: commit uses the latest world for constraints, projections and publications and preserves intervening state. Even a command producing no facts is checked. Race cases are [published vectors](interoperability.md#protected-command-read-race-vectors) run through the public host API, not a new specification construct.

#### Keys the event store refuses

A protected read decides over a key that arrives at run time, so the key itself can be one the event store cannot decide over. The shapes are: empty, whitespace-only, exactly `*`, containing `#` anywhere, and leading or trailing whitespace. Such a key, or any part of a composite key, behaves as a missing view: `Validation` with `<ReadModel> '<key>' does not exist.`, decided before the world is consulted. It rejects even when the world holds an instance stored at that key. Only protected reads change; see [decision 0091](https://github.com/Cratis/Screenplay/blob/main/decisions/0091-protected-reads-treat-keys-the-event-store-refuses-as-missing.md). Hosts map their store's refusal to this outcome, and the [published vectors](interoperability.md#protected-reads-of-refused-keys) (`ProtectedCommandReadsCorpus.V25`) pin it.

### Deciding and committing from a host

`SemanticEvaluator.Execute` decides and commits at one world. A host that appends to a real store has a window between the two, in which another writer can append, so it calls the two halves itself, under [decision 0089](https://github.com/Cratis/Screenplay/blob/main/decisions/0089-expose-the-reference-evaluators-decide-commit-split-to-hosts.md):

```csharp
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;

static SemanticExecutionResult DecideAndCommit(
    SemanticEvaluator evaluator,
    SemanticExecutionPlan plan,
    SemanticWorld decidedWorld,
    SemanticExecutionRequest request,
    ImmutableArray<SemanticFact> factsAppendedMeanwhile)
{
    var decision = evaluator.Decide(plan, decidedWorld, request);
    var extended = evaluator.ExtendWorld(plan, decidedWorld, factsAppendedMeanwhile);

    return extended is SemanticAccepted ? decision.Commit(extended.World) : extended;
}
```

- `Decide` returns a `SemanticDecision`. Its `Boundary` is the count of facts in the decided world and its `Dependencies` list, per protected key, the event contracts that change the views behind it (`Entries[].Key` and `Entries[].EventContracts`). `Dependencies.Matches(fact)` applies the conflict rule to one fact. A command that reads nothing, or whose decision was refused, has `Dependencies.Empty`. A read-only request is refused with a `Contract` rejection; run it through `Execute`.
- `Commit(world)` takes a world that holds the decided facts as its prefix and settles the decision against it: `Conflict` / `DecisionStateChanged` with no facts when a later fact matches a dependency, otherwise constraints, projections and publications run against that world. A world that is shorter than, or rewrites, the decided history is a `Contract` rejection.
- `Commit` is pure. It changes neither the decision nor the world, so the same decision can be committed against several worlds in turn: accepted against one, in conflict against a longer one.
- [`ExtendWorld`](projections/semantic-model.md#extending-a-world-with-facts-from-outside) turns the facts another writer appended into the later world. It is how a host reaches `Commit` without reconstructing history.
- `ISemanticEvaluator` has no `Decide`. Reactions and cascades keep calling `Execute`: inside settlement the decide and commit worlds are the same, so no window exists.

The [host loop](reactions.md#append-a-settled-command-in-a-live-host) covers settlement, the guarded write and when a lost guard may be retried.

A target must explicitly admit and realize the version or refuse it; successful reference execution is not proof of Stage or Arc realization. Plain read-model injection is still not a protected decision. Use a `unique` [constraint](constraints.md) for uniqueness and see [keeping decisions correct in Arc](/arc/backend/csharp/chronicle/consistency/) for hand-written code.

Eligible legacy models receive informational `PLAY0786` at the first command read. The typed workspace/MCP repair previews the preamble insertion, checks workspace/catalog revisions, preserves comments and requires individual review before explicit apply. Refused reads list their compiler reasons; the repair never silently upgrades a model.

## Validation rules

Declarative validation covers the common cases without code:

| Rule | Example |
| --- | --- |
| `not empty` | `name not empty` |
| `max <n>` | `reason max 500` |
| `min <n>` | `quantity min 1` |
| `> <value>` | `quantity > 0` |
| `>= <value>` | `discountPct >= 0` |
| `< <value>` | `dateOfBirth < today` |
| `<= <value>` | `discountPct <= 100` |
| `== <value>` | `currency == "NOK"` |
| `!= <value>` | `status != "draft"` |
| `length == <n>` | `currency length == 3` |
| `decimals <= <n>` | `price decimals <= 2` |
| `contains "<text>"` | `reference contains "INV"` |
| `not contains "<text>"` | `reference not contains " "` |
| `starts with "<text>"` | `reference starts with "INV-"` |
| `ends with "<text>"` | `reference ends with "-NOK"` |
| `matches email` | `email matches email` |
| `matches "<regex>"` | `invoiceNumber matches "^INV-[0-9]{6}$"` |
| `all > <value>` (on collection) | `lines.quantity all > 0` |
| `all >= <value>` (on collection) | `lines.unitPrice all >= 0` |
| `rule <Name>` | `orgNumber rule BeAValidOrganizationNumber` |

`max` and `min` take their meaning from the property's type: on text they bound its length (`reason max 500` — at most 500 characters), and on a number they bound its value (`quantity min 1`). There is one rule for each, not a separate length and value form.

`matches "<regex>"` uses the [portable match dialect](#portable-match-dialect): a restricted, flagless JavaScript regular expression. Like JavaScript `RegExp.test`, it succeeds if **any part** of the text matches; use `^` and `$` to require a whole-value match. Patterns are checked at authoring. The reference evaluator runs Screenplay's own Thompson automaton, not a platform regular-expression engine. Each input code unit visits at most the automaton's states; there is no timeout or host-specific engine limit. It applies only to a single text value (not an enum or collection). `matches email` expands to `^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$`: exactly one `@`, a non-empty local part without whitespace or `@`, and a domain with at least two non-empty dot-separated parts without whitespace or `@`. This is a conservative pattern, **not** RFC 5322 email validation. Other bare names have no definition and are rejected; quote a pattern to use a custom regular expression.

The four literal text rules `contains`, `not contains`, `starts with` and `ends with` compare UTF-16 code units ordinally: case, accents, whitespace and Unicode normalization are not changed. They require one non-enumerated text value and a quoted text operand, not a collection, property reference or regular expression. An absent optional value passes; add `not empty` to require presence. Empty operands are admitted: `contains ""`, `starts with ""` and `ends with ""` always pass, while `not contains ""` always fails for present text. First-class text rules select their own executable-model version; models using none keep their existing version and bytes.

`decimals <= <n>` limits how many places a `Decimal` value has after the decimal point. The operand is a whole number from 0 to 28, and the subject is one `Decimal` property or a `Decimal` concept, not a collection, a whole number or text. Trailing zeros do not count, so `19.990` has two places. An absent optional value passes. A rule that fails is a validation rejection with the rule's message. The rule selects language/semantic `29.0`; models using none keep their existing version and bytes. It is checked by the reference evaluator, Stage and every generated application the same way, so a price with three decimals is refused where it enters rather than rounded where it is stored.

### Portable match dialect

Patterns operate on UTF-16 code units, without flags. They admit literals, `.`, `^`, `$`, alternation, ordinary groups, `(?:...)`, and `(?<name>...)` with names matching `[A-Za-z_][A-Za-z0-9_]*`. Captures cannot be observed: backreferences are not admitted. Duplicate names are admitted only in disjoint alternatives, as in `(?<x>a)|(?<x>b)`; duplicate names that can participate together report `PLAY0367`. JavaScript-valid names outside the ASCII spelling (such as `$x`, `é` or `\u0078`) report `PLAY0699`; malformed names report `PLAY0367`. Quantifiers are `*`, `+`, `?`, `{n}`, `{n,}`, `{n,m}` and their lazy forms. Stacked quantifiers such as `a**`, `a+?+` and `a{2}{3}` report `PLAY0367`.

Character classes contain literals, ranges with single-character endpoints, and `\d \D \w \W \s \S`. Negation and unions use set semantics: `[\w\W]` matches any code unit and `[^\w\W]` matches none. `\d` is `[0-9]`; `\w` is `[A-Za-z0-9_]`. `\s` is the fixed JavaScript set: U+0009–U+000D, U+0020, U+00A0, U+1680, U+2000–U+200A, U+2028, U+2029, U+202F, U+205F, U+3000 and U+FEFF. Dot excludes LF, CR, U+2028 and U+2029. `$` means the very end of input, not before a final newline.

Both contexts admit `\t \n \r \f \v`, `\0` not followed by a digit, `\xHH`, `\uHHHH`, and escaped syntax characters `\^ \$ \\ \. \* \+ \? \( \) \[ \] \{ \} \| \/`. Inside a class, `\-` is also admitted and unescaped `{` and `}` are literal. Outside a class, an unescaped `{` must start a valid quantifier; escape a literal brace.

`PLAY0699` names a refused construct and suggests a replacement where one exists:

| Refused form | Replacement or limitation |
| --- | --- |
| Lookahead and lookbehind | Except for the exact deprecated Studio text-rule shapes below, no general equivalent. Replace a length-only assertion such as `^(?=.{21}$).*$` with `^.{21}$`. |
| Backreferences; `\b` and `\B` word boundaries | No equivalent assertion in this dialect. |
| `\p` and `\P` Unicode properties | An explicit character class when its required set is known; no general equivalent. |
| `\u{...}`, `\cX`, class `\b` | UTF-16 `\uHHHH` escapes, hexadecimal control escapes such as `\x01`, and `\x08`, respectively. |
| `\A`, `\z`, `\Z` | `^`, `$`, `$`, respectively. |
| Inline options such as `(?i)` | Explicit case classes such as `[aA]`. Other group extensions and .NET-only syntax are not admitted. |
| `[]`, `[^]`, a leading or lone `]` | Use `[^\s\S]` for no code unit, `[\s\S]` for any code unit, or escaped `\]` for a literal bracket. |
| Class subtraction; class escapes as range endpoints, such as `[\d-a]` | Write explicit ranges or a union of classes. |
| Octal and other identity escapes | Use `\xHH`, `\uHHHH`, or write the literal without a backslash. |

A pattern has at most 1,000 UTF-16 code units and 3,000 expanded nodes. Each literal, class and anchor counts one, as does each optional, loop or alternative choice point. A counted repeat multiplies its body by its upper bound; without an upper bound, by the lower bound plus one. It adds one choice point for each optional copy (one loop for an unbounded repeat). `*`, `+` and `?` count their body once plus one choice point. Thus `x{1500}` counts 1,500, `a{0,1500}` counts 3,000 and `(a*){1500}` counts 3,000. Exceeding either limit reports `PLAY0699` before automaton construction. The automaton has at most the expanded size plus one accepting state, including epsilon choice states; an empty expression needs only that accepting state.

These meanings fix earlier C#/JavaScript differences: `\s` now includes Unicode spaces, so `matches email` rejects `a<U+00A0>@b.co`; dot rejects CR and U+2028/U+2029; `\w` rejects U+0130. Older ESM documents keep their authored pattern bytes and load even when the current parser classifies the pattern as invalid or outside the dialect. Except for the exact Studio text-rule shapes below, either case produces plan-time `UnsupportedValidation` and deterministic generation Unproven, naming the construct. Re-authoring reports `PLAY0367` for invalid syntax or `PLAY0699` for a construct outside the dialect. The matcher has the same behavior on every supported target framework; platform regex settings cannot change a verdict.

### Deprecated Studio text-rule patterns

Studio's exact escaped-literal lookaround shapes report warning `PLAY0787`, not `PLAY0699`. The syntax tree keeps the original `matches` text; executable binding stores the equivalent ordinal rule. The editor quick fix and MCP `propose-repair` migrate the source while preserving the rule's message and severity.

| Decoded pattern (`L` is an escaped literal) | Replacement |
| --- | --- |
| `^(?![\s\S]*L)` | `not contains "literal"` |
| `L(?![\s\S])` | `ends with "literal"` |
| `^L(?![\s\S])` | `== "literal"` |
| `^(?!L$)` or `^(?!L(?![\s\S]))` | `!= "literal"` |

Recognition requires a lossless escape/decode round trip. Studio v1 uses .NET `Regex.Escape`; v2 escapes exactly `\ ^ $ . | ? * + ( ) [ ] { } /`. The not-contains wrapper accepts either escape set; the strict-end lookaround forms use v2 escaping, and the `$` inequality uses v1. Different assertion bodies, reversed `[\S\s]` sets, unescaped metacharacters and trailing regex syntax are not recognized and still report `PLAY0699`. Already-admitted non-lookaround forms, including `L`, `^L`, `^L$` and `L$`, are unchanged. No general lookaround support is added.

Older serialized ESM `matches` rules with these exact shapes evaluate and participate in generation proofs as the replacement, without changing their stored bytes. New `not contains` and `ends with` rules select the text-rule ESM version; equality replacements keep the existing equality contract. The deprecation remains a warning until a later major release.

### Checking patterns before authoring

C# callers use `Cratis.Screenplay.Semantics.PortableMatchPattern.Check(pattern)`; TypeScript callers import `checkPortableMatchPattern` from `@cratis/screenplay-compiler`. Both return `PortableMatchCheck`: status, diagnostic code, construct, UTF-16 offset, suggestion, description and optional replacement. Status is Admitted, Deprecated, Refused or Invalid (lowercase strings in TypeScript). Deprecated has code `PLAY0787` and a replacement `{ kind, value }`; the semantic kind vocabulary is Equal, NotEqual, Contains, NotContains, StartsWith and EndsWith. Admitted has no diagnostic details; invalid syntax uses `PLAY0367` and other refused constructs use `PLAY0699`. Checking never executes the pattern or constructs a platform regex. This is authoring admission, not a claim that an application has rendered or run.

Browser checks remain advisory: admitted patterns can still backtrack badly in JavaScript. Both compilers report warning `PLAY0803` for exponential ambiguity, naming the repeated subexpression and suggesting a simple equivalent where available: `(a+)+` becomes `a+`, and `(a|a)*` or `(a*)*` becomes `a*`. The check preserves parallel derivations in a position automaton and respects anchors and overlapping UTF-16 character classes. It reports the smallest ambiguous repetition (earliest offset on a tie). To bound authoring time, analysis silently skips patterns exceeding 256 expanded positions (including anchors) or 500,000 graph-construction and synchronized edge-pair steps. Polynomial ambiguity is not checked; absence of a warning is not a browser performance guarantee. The warning changes neither admission nor execution. The server-side linear check is authoritative.

### Rule messages and severity

A rule may have a `message` shown when it fails. Add `severity information`, `severity warning` or `severity error` before the optional end-of-line `message`. The default is `error`; the printer omits it. Message remains last so its quoted text or `$strings` key is unambiguous:

```screenplay
validate
  invoiceNumber not empty                  message "Invoice number is required"
  invoiceNumber matches "^INV-[0-9]{6}$"  severity warning message "Invoice number must match INV-000000"
  dueDate > today                          message "Due date must be in the future"
```

### Rules about the command as a whole

Every rule above says something about one property. The rules that actually guard a domain usually do not — "the month is already started", "the engagement must be in its contract phase" — they are about the command as a whole, and most often about state it [reads](#what-the-command-reads). `require` states one:

```screenplay
command StartMonth
  engagementId EngagementId identifier
  reads EngagementScope by engagementId

  validate
    require EngagementScope.isStarted == false
      message "The month is already started"
    require EngagementScope.phase == "Contract"
      message "The engagement must be in its contract phase"
```

The condition is the language's [one condition grammar](grammar.md) — the same one a [policy](policies.md) `require` carries, so `and` and `or` mean the same thing here, `and` binds tighter than `or`, and parentheses group:

```screenplay
validate
  require EngagementScope.isStarted == false and EngagementScope.phase == "Contract"
    message "The month cannot be started yet"
```

The message and optional severity go in the body rather than on the end of the line. A condition is as long as the rule it states, and metadata pushed out past it is the part nobody reads. Use a quoted literal or an unquoted `$strings.<key>` for the message. Write `severity warning` or `severity information` as a sibling of `message` (in either order); omitting severity means `error`:

```screenplay
command ConfirmOrder
  total Decimal
  validate
    require total > 0
      severity warning
      message $strings.orders.totalMustBePositive
```

Every failed rule and requirement **rejects** the command, even at warning or information severity. A rejection carries each failed rule's message and severity for UI presentation; severity is not a pass/fail threshold.

An operand is either a property of the command or a path into state the command declares it reads (qualified by its alias, or by its view name if unambiguous). Anything else is a warning — a requirement a reader cannot resolve says less than it appears to. A rule whose logic is not a comparison at all still belongs in a named `rule` or an inline block, as below; `require` is for the rules that *can* be stated.

#### A rule that only applies sometimes

A rule that holds only under a condition — "an extension has to move the end date out, but a renewal need not" — is a requirement like any other. State it as an implication rather than reaching for a second construct:

```screenplay
command ExtendEngagement
  engagementId EngagementId identifier
  isExtension  Bool
  endDate      Date
  newEndDate   Date

  validate
    require isExtension == false or newEndDate > endDate
      message "An extension must move the end date out"
```

`or` is what makes the rule conditional: when `isExtension` is false the requirement is already satisfied and the comparison never decides anything. This is why the language has no separate `when` clause on a rule — the condition grammar already says it, and a second way to write the same rule would be a second thing to keep consistent.

### Rules whose logic is not expressible

Not every rule is a comparison. A predicate — "is this a valid organization number", "is this still available" — has logic that lives in code, and the declarative shapes above cannot express it.

Leaving it out is the worst option, because it makes the document lie: a property with two declarative rules and three predicates reads as a property with two rules, and a reader cannot tell "nothing further constrains this" from "the rest could not be written down". Name the rule instead:

```screenplay
validate
  orgNumber not empty                       message "Organization number is required"
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
  orgNumber rule BeUnique
```

The name is a reference into the implementation, not a declared construct — nothing resolves it, and the compiler does not check that anything called `BeAValidOrganizationNumber` exists. It is there so the document is honest about how constrained a value is, and so a reader has something more useful than "a rule was omitted".

### Giving a named rule a body

A bare `rule <Name>` states that a constraint exists without saying what it computes — sometimes that is genuinely all a document can say, because the logic lives somewhere the compiler cannot see. When the logic *can* live in the document, give the rule a body the same way every other construct that needs exact details does: a `file` reference or an inline code block, indented under the rule:

```screenplay
validate
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
    file Validations/BeAValidOrganizationNumber.cs
```

````screenplay
validate
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
    ```csharp
      string orgNumber = context.Value;
      return orgNumber.Length == 9 && orgNumber.All(char.IsDigit);
      ```
````

Both forms are optional and mutually exclusive with each other — a rule with neither stays the bare, undetermined-location form from above. The `file`/fenced-code shapes and their compiled representation (`FileReferenceSyntax` / `CodeBlockSyntax`) are exactly the ones used by [`handler`](#the-handler-block) and [reactions](reactions.md), so a reader who knows one already knows the other. The same body is available on a concept's own `rule <Name>` (see [Concepts](concepts.md#validation)) — the implementation travels with the value everywhere it appears.

Cross-field or complex rules drop into C#. The block yields the message of every rule the command breaks, and yields nothing when the command is valid:

````screenplay
validate
  ```csharp
  if (context.Artifact.PaymentTerms == "immediate" && context.Artifact.Total > 1_000_000)
  {
      yield return "Invoices over 1,000,000 cannot require immediate payment";
  }
  ```
````

Declarative `validate` and fenced `validate` can coexist on the same command.

### Named-rule implementation intent (commands only)

Add guidance without changing an existing command predicate's source:

```screenplay
command Submit
  label String
  validate
    label rule CheckLabel severity warning message "Invalid label"
      implementation
        hint "Preserve the existing acceptance criteria"
        file Rules/CheckLabel.cs
```

This is a command fragment; the file link selects ordinary team-owned predicate source. A wrapper accepts ordered nonblank quoted `hint` lines and at most one `file` or registered-language tagged fence. Direct forms remain valid. The rule owns its File/Code; metadata owns only hints. Do not mix direct and wrapped sources or repeat the wrapper.

An empty or hints-only wrapper is pending intent: it is valid authoring syntax but executable binding fails with `PLAY0268`. Attached wrappers bind to the existing `RulePredicate` contract, exactly like direct source. Adding, editing or reordering hints does not change canonical ESM bytes, requirement identity, context/result versions, capability or source content hash. Source coordinates move with the text. Reached opaque predicates remain unsupported by the reference runner, never a passing test or confirmation.

Concept predicates, builtin property rules and whole-command `require`/`validate` bodies do not accept this wrapper. Handler and operation-phase wrappers have their own [support boundaries](context.md#implementation-wrapper-support). Realization, committed locks, drift checks, confirmation and AI actions remain deferred under [#307](https://github.com/Cratis/Screenplay/issues/307); builds never invoke AI.

### What a rule can see

Inside a `rule` body and inside a `validate` block with a ` ```csharp ` fence, `context` is the `RuleContext`:

| Member | Value |
| --- | --- |
| `context.Artifact` | The whole thing under validation — the command here, the concept's own value on a concept rule. |
| `context.Value` | The value the rule is declared on. Equal to `Artifact` for a fenced `validate` block. |
| `context.Property` | Where that value sits in the artifact — `orgNumber` above, empty for a whole-command block. |
| `context.Tenant` | The tenant the command is executing for. |
| `context.CausedBy` | The identity that caused the command, so a rule such as "you may not approve your own request" is expressible. |
| `context.Occurred` | When the command was received. |

A rule can see **who** is calling but not **what they are allowed to do** — there are no roles and no claims in a `RuleContext`. A rule that inspects those is an authorization decision wearing a validation hat, and belongs in a [policy](policies.md). See [Contexts](context.md) for all four shapes.

### What the executable model admits

Every rule above parses, prints and round-trips. The executable semantic model (ESM) — what the [reference execution](specifications.md#reference-execution) runs and what Stage renders — admits the rules that have one portable meaning in v1, and binds code validation as opaque implementation attachments in v3. It reports blocking diagnostic `PLAY0268` for rules it still cannot represent, with a message that says why.

| Rule | Admitted on | Operand |
| --- | --- | --- |
| `not empty` | text, or a collection | none |
| `max`, `min` | text (bounds its length) or a whole or decimal number (bounds its value) | a non-negative whole number on text; a number of the property's type otherwise |
| `>`, `>=`, `<`, `<=` | a whole or decimal number | a number of the property's type |
| `==`, `!=` | text, an enum member, a whole or decimal number, or a boolean | a literal of the property's type, or a bare member name of an enum concept |
| `==`, `!=`, `>`, `>=`, `<`, `<=` (ESM v26) | a scalar Date or DateTime command property | a date literal of the same primitive; DateTime needs an offset or Z |
| `==`, `!=` in `require` and `produces when` only (ESM v28) | a required scalar Uuid command property or protected read path | see [Uuid equality](#uuid-equality-in-command-conditions); not admitted as a validation rule |
| `length ==` | text that is not an enum | a non-negative whole number |
| `all >`, `all >=` | a collection of whole or decimal numbers | a number of the element type |

```screenplay
command RegisterInvoice
  invoiceId InvoiceId identifier
  currency String
  amount Decimal
  lineAmounts Decimal[]
  status InvoiceStatus

  validate
    currency length == 3        message "A currency code has three letters"
    amount > 0                  message "An invoice for nothing is not an invoice"
    lineAmounts all >= 0        message "No line can be negative"
    status != cancelled         message "A cancelled invoice cannot be registered"
```

The meaning is fixed so every target agrees:

- An absent value of an optional property satisfies every rule except `not empty`.
- `not empty` on text requires at least one non-whitespace character: null, `""` and whitespace-only text fail. Whitespace is the Unicode `White_Space` set: U+0009–U+000D, U+0020, U+0085, U+00A0, U+1680, U+2000–U+200A, U+2028, U+2029, U+202F, U+205F and U+3000 — exactly .NET `char.IsWhiteSpace`. A lone U+FEFF passes; JavaScript targets must use this set, not `trim()`, which differs on U+0085 and U+FEFF. Values are not trimmed or normalized by the rule. This meaning applies to concept and command property rules in every ESM version.
- `not empty` on a collection requires at least one element, regardless of that element's value. An empty collection satisfies `all >` and `all >=`.
- Identity text is separate: [event-source and stream-id parts](event-sources.md) still treat whitespace as a value; only the exact empty string is empty.
- Text length counts UTF-16 code units, the length .NET and JavaScript both report.
- An operand is a literal. A property reference is not admitted, and an operand that does not fit the property's type — `quantity min 1.5` on an `Int`, `status == "pending"` on an enum without that member — is a binding error (`PLAY0273`) on the rule.
- A failed rule rejects the command at every severity, with its `message` (or a generated description) and its severity.

Still rejected, and why:

| Rule | Why |
| --- | --- |
| date comparisons in concept rules, reaction `where`, or with read paths, `$context` or `today` | These positions and operands are not admitted by date-comparison ESM v26. |
| Uuid comparisons in validation rules, concept rules or reaction `where`, ordering on Uuid, and two different Uuid concepts | ESM v28 admits only `==` and `!=` in command `require` and `produces when`; concept rules also run on query arguments and in client validators. |
| a named `matches` pattern other than `email` | Only `email` has a portable definition; other names are rejected (`PLAY0366`). Invalid pattern syntax reports `PLAY0367`; constructs outside the portable dialect report `PLAY0699` with a suggested replacement where one exists. |
| `require` over a read-model path without `command reads protected` | Protected-read admission requires explicit opt-in; with it, admitted single-level paths are executable. |
| a bare `rule <Name>` | Its logic lives outside the document, so it has no portable meaning. |
| a rule on a nested path such as `lines.quantity` | ESM v1 validates command properties; put the rule on the nested value's [concept](concepts.md#validation) instead. |

A bodied `rule <Name>` on a command property or concept value carries its name, property (or concept value), message, severity and stable implementation requirement id in ESM v3. Fenced `validate` blocks on commands and concepts also bind: both yield zero or more rejection messages, with the concept's own value guarded by a concept block. Each yielded message rejects at error severity. A rule predicate receives the guarded value (with the command as its artifact for a command rule) and returns accept or reject. The context follows the documented `RuleContext` above; Screenplay neither executes the C# nor enforces a provider's API allowlist. Requirements use context/result contract version 1 and capability `pure`. Files need host-supplied contents to acquire a content hash; the path is never a code revision. The reference executor reports `SemanticUnsupported` naming the opaque rule for any command carrying it, including concept values nested in a composite or collection. It cannot establish a portable ordering with other validation rules, so it never treats an earlier declarative rejection as proof that the predicate was run. Other commands and read-only specifications remain executable.

### Chronological command date comparisons

ESM v26 adds `==`, `!=`, `>`, `>=`, `<` and `<=` for `Date` and `DateTime` in command `require`, `produces when` and command/property validation rules. Conditions compare a required scalar command property with another required scalar command property of the same primitive, or with a literal. Validation rules still require a literal right operand; optional absent values continue to pass. A Date concept can type a command property, but date rules on the concept itself remain refused.

```screenplay
command KeepWindow
  from DateTime
  to DateTime
  validate
    require to >= from
    to >= "2026-01-01T00:00:00Z"
```

`Date` compares calendar dates without a time zone. `DateTime` compares UTC instants at tick precision (seven fractional digits), including equality between different offsets naming the same instant. Literals bind to canonical text: `yyyy-MM-dd` or UTC with seven fractional digits and `Z`. An invalid date literal or a DateTime literal without an offset reports `PLAY0273`. A refused comparison reports only `PLAY0268`, without checking its literals.

Every DateTime input property used by a comparison must carry an offset or `Z`. Input validation rejects a missing offset with a Validation outcome before any rule runs, even when a condition short-circuits or a production is skipped. Targets apply this before deserialization can attach a local offset. A target that cannot preserve tick precision refuses the comparison; JavaScript `Date` alone is insufficient. Client validators omit date ordering rules. Specification `then` matching, read-model keys, constraints and freshness keep ordinal equality. `$context`, protected read paths, `today`, mixed Date/DateTime operands and reaction `where` remain refused.

### Uuid equality in command conditions

ESM v28 admits `==` and `!=` between Uuid values (the Uuid primitive, or a concept or identity over it) in command `require` and `produces when`. A `require` may also use a protected read path on either side; `produces when` uses command properties and literals. Both operands must be required scalars. When both sides are concepts they must be the same concept, a bare Uuid compares with any Uuid concept, and a literal takes the type of the other side. A literal must be canonical (lowercase, hyphenated, 36 characters); any other spelling reports `PLAY0273`. A refused whole comparison reports only `PLAY0268`: ordering, optional or collection operands, two different concepts, Uuid against text, `$context`, reaction `where`, and Uuid in validation rules or concept rules.

```screenplay
command RegisterInvoice
  customerId CustomerId
  approvedBy UserId
  submittedBy UserId
  validate
    require customerId != "00000000-0000-0000-0000-000000000000"
    require approvedBy != submittedBy
```

The comparison is ordinal equality of the canonical spelling. Every Uuid reaching a condition is already canonical, so this is value equality; specification `then` matching, read-model keys, constraints and freshness are unchanged. When a protected read finds no instance, [protected read absence](#what-the-executable-model-admits) applies unchanged. Commands invoked by a reaction evaluate these conditions the same way.

Command `require` conditions over command properties and constants are admitted in ESM v1: equality on scalar text, enumeration, number or Boolean; ordering on numbers only. ESM v26 adds the date comparisons above and ESM v28 adds the Uuid equality above. Both property operands must have compatible types. Under `command reads protected`, either operand may instead be a declared read's single-level property path. Requirements run after property validation and required-read presence checks and before production; a false requirement rejects with its message (or a default message). Concept `require` is not admitted: declare a concept validation rule instead.

## Authorization

```screenplay
authorize <requirement>
```

Two policies written next to each other mean both must pass, and `and` says the same thing out loud. `or` makes them alternatives. Repeated `authorize` lines on a command or query combine with AND in authored order; printing writes them as one `authorize A and B` line. A requirement may continue on the next line at deeper indentation:

```screenplay
authorize CanManageInvoice and IsAdultCustomer

authorize IsAccountant
          or IsCustomerSelf
```

This is the language's [one condition grammar](grammar.md) again, over policies instead of comparisons — so `and` binds tighter than `or`, and parentheses group:

```screenplay
authorize IsAccountant or IsFinance and OwnsInvoice     // IsAccountant, or both of the others
authorize (IsAccountant or IsFinance) and OwnsInvoice   // one of the first two, and OwnsInvoice
```

Those two admit different callers, and the parentheses are the only thing that distinguishes them. Printing writes them back wherever the grouping is not the one precedence gives, so a document always says which one it means.

Policies are declared at the top of the file — see [Policies](policies.md). Declarative policies bind to ESM v1 and are evaluated before validation using an explicitly supplied caller; a failed gate returns the typed `Unauthorized` rejection without producing events. Module and feature gates are composed with the command's gate by AND. Inline `csharp` and file policies bind as opaque ESM v3 predicates: the reference evaluator reports `SemanticUnsupported` if evaluation reaches one, and a target provider must supply its implementation.

## The `produces` block

Declares what events a command emits. Supports single, multiple, and conditional forms. Beside a pending [handler](#the-handler-block), unconditional `produces` doubles as the body's result contract, not a payload implementation. A file/code handler remains mutually exclusive with `produces`.

With application-root `command reads protected`, a produced-event mapping may source a single-level property from an admitted read, such as `amount = balance.outstanding`. Without the preamble, read-path mappings retain `PLAY0268` and point to the opt-in. This does not admit read paths in `produces when`, response fields, `for`, stream routes or derive/provide expressions.

### Single event with property mapping

```screenplay
produces InvoiceRegistered
  for invoiceId                          // event source, never payload
  invoiceNumber = invoiceNumber          // from command property
  registeredAt  = $context.occurred      // event occurrence time
  registeredBy  = $context.identity.id  // caller identity
  source        = $env.SERVICE_NAME      // environment variable
  status        = "draft"                // string constant
  lineCount     = 0                      // numeric constant
```

### Tags

`tag` lines before the mappings attach [tags](events.md#tags) to the event appended by this specific production. ESM carries literal tags as ordered append metadata, not event payload; computed `$context` tag values are not yet admitted:

```screenplay
produces InvoiceRegistered
  tag audit
  for invoiceId
  invoiceNumber = invoiceNumber
```

### Mapping one concept to another

A mapping may assign a value of one concept to a property of a different concept, or between a concept and its primitive, when both sit over the same primitive. This is how an identifier crosses a context boundary and keeps its own concept on each side:

```screenplay
concept OrderId : Uuid
concept MovementId : Uuid

module Stock
  feature Moves
    slice StateChange MoveStock
      command MoveStock
        id String identifier
        orderId OrderId
        produces StockMoved
          for id
          movementId = orderId    // OrderId becomes MovementId
      event StockMoved
        movementId MovementId
```

The mapping is admitted when all of these hold: both concepts are over the same primitive, neither is an enumeration, the source is not optional when the target is required, and the target concept declares no rule the source concept does not also declare, so the value is already proved. A personal-data or secret classification on the source must be repeated on the target. Anything else is still the type mismatch error. The check applies to every property mapping: `produces`, reaction productions and invocations, and projections. A model that needs the conversion selects language/semantic `29.0`; models using none keep their existing version and bytes.

### Mapping sources

| Source | Syntax | Description |
| --- | --- | --- |
| Command property | `= <propertyName>` | Direct copy from command |
| Command context | `= $context.occurred` | Timestamp of the command |
| Tenant | `= $context.tenant` | The tenant the command executes for |
| Calling identity | `= $context.causedBy.subject` | Subject of the identity that caused the command |
| Causation | `= $context.causation.type` | What caused the command — a command, reactor, schedule |
| Caller identity | `= $context.identity.id` | The caller's identifier from the auth token |
| Caller claim | `= $context.identity.claims.<name>` | The value of a claim the caller carries |
| Environment | `= $env.<VAR_NAME>` | Environment variable |
| String constant | `= "value"` | Literal string |
| Numeric constant | `= 0` | Literal number |
| Expression | `= lines.sum(l => l.quantity * l.unitPrice)` | Computed value |

Every `$context.` path names a member of the `CommandContext` an inline handler compiles against — see [Contexts](context.md). The ESM v2 `produces` subset is narrower: occurrence time and the three audit identity fields (`identity.id`/`causedBy.subject`, `name`, `userName`). Other entries above remain syntax-only for portable produces mappings and report `PLAY0268`. Explicit `for <command-identifier>` binds a typed state-change destination separately from event properties. When the event does not duplicate that identifier as a payload property, it selects ESM v2; historical v1 models that copy it into both places retain their canonical v1 bytes. Produced v2 facts carry the identity in event context even when the payload has no ID.

### Where an event lands

A plain `produces <Event>` without `for` does not say where the event lands, and it does not mean the command's [identifier](#the-identifier). What happens next depends on which runtime handles the command:

- **Executable model** (reference execution and Stage's semantic host). A plain `produces` is appended to an identity the execution request allocates for the command. When none is supplied, the command is reported as unsupported with `IdentityAllocation`, so name the destination with `for` or supply an allocated identity.
- **Rendered Arc commands** (code generated by Stage's default renderer). The renderer does not read `for`: the command's identifier is used as the event source for every append, whatever `for` says, and a command with no identifier leaves Arc to allocate the id. This is tracked in [Cratis/Stage#187](https://github.com/Cratis/Stage/issues/187); until it is fixed, treat `for` as binding only in the executable model.

A plain omission with an available identifier reports information diagnostic
[`PLAY0478`](diagnostics.md#production-destination-advice-and-repairs). Its typed
[MCP repair](mcp/authoring-tools.md#fix-a-diagnostic) inserts `for <identifier>` after
review; it does not apply automatically. Accept it when the identifier is the
intended destination, not when you deliberately want an allocated identity.

The rest of this section describes the executable model. In ESM v2, a plain `produces` in a command where another production states `for` uses that destination. State `for` on every production that belongs to the command's event source; the event source id is never payload. An explicit `for` can name the command's required scalar identifier on an indented line:

```screenplay
command Activate
  requestId  Uuid identifier
  contractId Uuid

  produces RequestActivated
    for requestId

  produces ContractPolicyActivated
    for requestId
    contractId = contractId
```

Both facts address the command's `requestId` event source; the contract ID remains an event payload property. The executable model cannot bind `for contractId` here: explicit destinations must resolve to the command's required scalar identifier. Use an imperative handler for fan-out to other event sources.

`for` is an indented line rather than an argument on the header, so the target sits beside the mappings that fill the event instead of out past the end of the line. Only one `for` per `produces`: an event is appended to one event source.

### Multiple unconditional events

Repeat `produces` for each event; all are emitted. This excerpt illustrates the syntax, not event naming or a recommended model. `InvoiceRunningTotalUpdated` is a generic, redundant event: normally the projection derives the total from `InvoiceLineItemAdded`. A separate adjustment event is warranted only for an independent business decision, named for that decision rather than for updating a total:

```screenplay
produces InvoiceLineItemAdded
  for invoiceId
  addedAt    = $context.occurred

produces InvoiceRunningTotalUpdated
  for invoiceId
  adjustment = lines.sum(l => l.quantity * l.unitPrice * (1 - l.discountPct / 100))
```

### Conditional produces

`produces when <condition>` emits the indented event only when the condition holds. In ESM v1, comparisons use declared command properties and constants with `==`, `!=`, `>`, `>=`, `<`, `<=`, combined with `and`/`or` (`and` binds tighter; parentheses group). Ordering is numeric; equality admits scalar text, enumeration, number and Boolean. `$env` conditions remain syntax-only because environment values vary across realizations. the supported scalar `$context` mappings select v2 ([#226](https://github.com/Cratis/Screenplay/issues/226)), and paths into reads wait on [#129](https://github.com/Cratis/Screenplay/issues/129):

```screenplay
produces when isProForma == true
  ProFormaInvoiceIssued
    for invoiceId
    issuedAt   = $context.occurred

produces when paymentTerms == "net30" or paymentTerms == "net60"
  DeferredPaymentInvoiceRegistered
    for invoiceId
    paymentTerms = paymentTerms

produces when $env.WELCOME_EMAILS_ENABLED == "true"
  CustomerWelcomeEmailRequested
    for invoiceId
    customerId  = customerId
```

Multiple `produces when` blocks form mutually exclusive or overlapping branches — each condition is evaluated independently. When all conditions are false, execution accepts the command and appends no facts. ESM v1 also rejects arithmetic in mappings: Chronicle's projection language has no binary operators (Decision 0001).

## The `handler` block

Declares a fully imperative implementation of the command, in C#, as either an inline block or a reference to an external file. Use it when the declarative `produces` forms cannot express the logic — batch processing, imperative branching, or anything that needs more than property mappings and conditions.

Delegating to a file:

```screenplay
handler
  file Commands/ProcessInvoiceBatchHandler.cs
```

Inline C#:

````screenplay
handler
  ```csharp
    var events = new List<object>();
    events.Add(new InvoiceBatchProcessingStarted(
        BatchId: BatchId,
        StartedAt: DateTimeOffset.UtcNow
    ));
    foreach (var invoiceId in InvoiceIds)
        events.Add(new InvoiceSent(invoiceId, DateTimeOffset.UtcNow, context.Identity.Id, null));
    return events;
    ```
````

A command uses either `produces` blocks or a file/code `handler` — not both. A pending handler intent may sit beside `produces`. Keep handler logic small; anything substantial belongs in a `file` reference where it can be tested on its own.

Inside either form, `context` is the [`CommandContext`](context.md) — the command itself, the tenant, the caller, the identity recorded as having caused it, the causation, and when the command was received. An inline block and a `file` reference compile against exactly the same type.

### Implementation intent (handlers only)

You can keep implementation guidance beside the handler's existing attachment:

```screenplay
handler
  implementation
    hint "Archive only invoices dated before the requested cutoff"
    file Handlers/ArchiveOldInvoicesHandler.cs
```

This is the handler from [Invoicing](https://github.com/Cratis/Screenplay/blob/main/Samples/Invoicing/invoicing.play). The wrapper also accepts an existing tagged fence instead of `file`. Omit the payload to record **pending** intent; bare `implementation` is valid too. Hints are ordered, nonblank quoted strings, with the usual string escaping. Their decoded text is retained without trimming. A handler allows one wrapper and at most one payload. Unknown children, duplicate wrappers, multiple payloads and mixed direct/wrapped sources are errors.

A pending handler (an `implementation` wrapper with no `file` or code, with optional ordered hints) may coexist with `produces`. It cannot coexist with another `handler` occurrence, even another pending intent (`PLAY0698`), in either order.

A pending command handler with unconditional `produces` is a **described command**, admitted by ESM **v23**. Under a pending intent, `produces` doubles as **result contract version 1**:

| Part | Owner in result contract v1 |
| --- | --- |
| Events, count and order | Declarative: each `produces` line yields exactly one event, in declared order |
| Destination (`for`, routes and allocation) | Declarative, as for ordinary productions |
| Tags and other production metadata | Declarative |
| Generated values | Declarative; generated before the body and available to it |
| Response | Declarative: the declared command property |
| Authorization and validation | Declarative; run before the body, which denial or refusal never reaches |
| Event property values | **The body**; mappings are non-normative hints and required properties may be unmapped |

Neither renderers nor reference execution evaluate these event-property mappings. The exemption is limited to payload values; mapping type checks still apply. Binding reports information `PLAY0695` naming the command and RequirementId. No productions (including response-only pending commands) fails binding with `PLAY0696`; conditional `produces when` fails with `PLAY0697`. Result v1 does not admit repeated/occurrence productions or body-decided cardinality, and the body cannot reject. Source remains valid when binding refuses it. Legacy command `reads` remains outside admission.

Reference execution evaluates authorization, validation, routing and generated values in their ordinary order, then returns **Unsupported (`ImplementationBody`)** when it reaches the body, directly or through an invoking reaction. The reason names the command and RequirementId; no production mapping runs or event is emitted. Earlier denial/validation specifications still execute; unsupported specifications are counted separately and an all-unsupported run is never green. Historical `given` events remain ordinary facts.

**Realization is deferred.** The pending sidecar requirement carries ordered hints, flags and a CommandHandler typed-context descriptor with context/result versions 1. An admitting target supplies the body, not Screenplay. Moving to a file keeps the RequirementId, but attached handlers cannot keep `produces` today and require a constrained implementation attachment (`PLAY0800`). Screenplay defines no filled-body reuse across renders until a later decision defines the attached result contract and body ABI.

| Owner | `implementation` wrapper |
| --- | --- |
| Command handler | Supported; direct file/fence forms remain supported |
| Query performer, validation rule, reducer rule, policy, reaction trigger | Deferred; existing direct forms only |
| Operation execute/compensate phases | New syntax-only authoring wrapper; not admitted by any supported executable model (ESM) version yet |
| Provisioning | Deferred |

`implementation` and `hint` are contextual, not globally reserved property names. Intent authoring does not execute, confirm or regenerate code. File/inline handlers require a constrained implementation attachment (`PLAY0800`); described command admission requires consumers to explicitly admit v23. There is no lock, drift checker, confirmation or AI action in this slice. See [AST authoring](ast-authoring.md#handler-intent-edits) and [MCP inventory](mcp/reference.md#handler-intent-inventory).

## Concurrency

An optional `concurrency` block declares the concurrency scope enforced when the command's events are appended — mirroring Chronicle's `ConcurrencyScope`. When two commands race, the append fails for the loser instead of silently letting both win.

```screenplay
command RegisterInvoice
  ...
  concurrency
    eventSource
    sourceType Account
    streamType Onboarding
    streamId Monthly
    events InvoiceRegistered, InvoiceCancelled
```

| Dimension | Meaning |
| --- | --- |
| `eventSource` | Scope the check to the command's event source id |
| `sourceType <Name>` | Scope the check to an event source type |
| `streamType <Name>` | Scope the check to an event stream type |
| `streamId <Name>` | Scope the check to an event stream id |
| `events <EventType>[, ...]` | Scope the check to the listed event types |

All dimensions are optional and each appears at most once, but the block must declare at least one — an empty `concurrency` block is a compile error. A command without a `concurrency` block does not append unchecked: Chronicle's default optimistic concurrency strategy applies to every append, narrowed by the append's routing metadata. It skips only the first append into a scope, unless `CheckFirstAppendIntoAScope` is turned on or the append uses `ExpectingNoMatchingEvent()`. The executable model does not bind the `concurrency` block yet. Under `command reads protected`, combining it with command `reads` reports blocking `PLAY0783`; the compiler never guesses which consistency boundary you meant. See [Chronicle concurrency](https://github.com/Cratis/Chronicle/blob/main/Documentation/events/concurrency.mdx).
