# Events

Events are immutable, past-tense facts — the record of something that happened. An event declaration defines the event type's name and properties. Properties use [concepts](concepts.md), composite [types](types.md), or primitives.

## Syntax

```screenplay
event <Name> [generation <N>]
  [file <path>]
  [tag <value>]*
  <property> <Type>
  ...
```

Without `generation`, an event is generation 1 and prints exactly as before. To show an evolved event, declare **each full shape** with its own number, beginning at 1 and continuing without gaps or duplicates. Declarations with the same name in the same owning slice (module, feature path and slice) identify one event contract: its address and `EventContractId` do not include the generation number. A same-named event in another slice has a different address, not another generation of this contract. The current ESM binder treats same-named events across slices as ambiguous, and the identity catalog rejects colliding legacy IDs; do not use cross-slice declarations to express lineage. Generations use numbers from 1 through 4294967294; Chronicle reserves 4294967295 for an unspecified generation. The previous generation of N is always N−1. References to an event name resolve to its current (highest) generation in that slice; property checks use that shape, not the historical ones. The executable model stores one contract with current revision N and complete prior revisions 1..N−1, each with its predecessor (generation 1 has none). Each revision owns separate property identities, even for same-named properties. A lone `generation 1` marker behaves like an unmarked event and retains its previous ESM version. A multi-generation contract selects ESM v4. Bare references use the current revision only; a `produces` mapping, `then` assertion, projection mapping, or constraint naming a removed property fails binding with `PLAY0273`, and `given` historical-shape consumption reports `PLAY0449` with the event and revision. No historical fact is silently converted or replayed. A target without explicit migration support must reject an evolved contract; Chronicle registration needs migrations, which Screenplay does not yet express. Complete historical declarations alone do not prove schema immutability across edits to referenced concepts or types (see #71).

```screenplay
event ProjectRegistered generation 1
  projectId Uuid
  name String

event ProjectRegistered generation 2
  name String
```

The optional `file` line names the repository relative file this declaration is realized by, so a document can be navigated back to the code it describes. It is additive - it never stands in for any part of the declaration. See [File references](file-references.md).

For an existing catalog, advance a contract only with `SemanticIdentityCatalog.PlanEventRevisionAdvancement(previous, expectedRevision, documentKeys, semanticAddresses, eventAddresses, advancements)`. Supply the complete current addresses and an explicit `(event address, new revision)` advancement. The plan checks the exact previous catalog revision, retains the contract id, carries generation-1 property IDs to revision-qualified addresses, and refuses backward or stale advancement. A fresh compile can bootstrap all property identities for each declared revision and record revision N in its returned `SemanticCompilation.Documents.IdentityCatalog`; persist that catalog before subsequent edits. Workspace transactions can supply the same explicit advancement through `WorkspaceTransactionRequest.EventRevisionAdvancements`, checked against `ExpectedCatalogRevision`. Newly introduced events receive their declared revision without an advancement. Declaring fewer generations than a persisted catalog records fails binding.

## Example

```screenplay
event InvoiceRegistered
  invoiceId     InvoiceId
  customerId    CustomerId
  invoiceNumber InvoiceNumber
  lines         InvoiceLine[]
  currency      CurrencyCode
  registeredAt  DateTime
  status        InvoiceStatus
```

## Type modifiers

| Modifier | Syntax | Example |
| --- | --- | --- |
| Collection | `<Type>[]` | `lines InvoiceLine[]` |
| Optional | `<Type>?` | `note String?` |

## Tags

`tag` lines attach tags to every append of the event — Chronicle stores them alongside the event so consumers can filter and group on them. A tag value is a bare identifier or string literal for static tags, or a `$context.` expression for tags resolved from context at append time.

```screenplay
event InvoiceRegistered
  tag invoicing
  tag "billing"
  tag $context.identity.id
  invoiceId InvoiceId
```

Each historical revision retains its declared literal tags; new appends use the current revision's event tags.

Tags can also be declared per production site — on a [`produces` block](commands.md#the-produces-block) and on a capture [`append` block](captures.md) — where they apply to that specific append rather than every occurrence of the event type. ESM v1 admits literal event-level and production-level tags as append metadata, in that order. Chronicle's `IEventSequence.Append` accepts tags and persists them in `EventContext.Tags` (Decision 0001); tags do not change the event payload. `$context`-computed tags remain syntax-only; ESM v2 admits selected scalar `$context` **produces mappings**, not computed tags.

## Guidance

- **Name events in the past tense** and make them self-describing: `InvoiceRegistered`, never `Created`.
- **One purpose per event.** If an event needs a nullable property to cover two situations, model the second situation as its own event.
- **Compliance is inherited.** A property typed with a `@pii` concept is PII — nothing extra to declare on the event.
- The event-source identity is not an event property; it travels in typed event context in ESM v2. A command's `produces … for <identifier>` binds the destination from a required scalar command identifier. A projection can use `$eventSourceId` without copying that identity into every event payload, and a specification can name it with `for <value>` on its `given` and `then` events. Marking an event property `identifier` is an error.
- **Declare the shapes you reference.** A property typed `InvoiceLine[]` needs a [`type InvoiceLine`](types.md); the compiler warns when it resolves against nothing the document declares.
