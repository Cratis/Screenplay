---
title: Edit a model
description: Prompts for changing, renaming, moving and repairing parts of a Screenplay model through reviewed proposals, then applying or recovering them.
---

Every edit follows the same loop: ask for the change, review the proposal, apply it.
This page gives you prompts for each kind of change, then for the review and apply
step they all share. The examples use the lending library from
[Create a model](create.md).

## Change an element

```text
Change the description of the AddBook slice to "The librarian adds a newly bought
book to the catalog". Propose the change and show me the difference. Do not apply
it yet.
```

```text
Add a "publishedYear" property to the BookAdded event, and map it from the AddBook
command. Show me the source that changes.
```

The assistant reads the element, edits its typed form and proposes a replacement.
Edits to a single value keep every comment and blank line in place.

## Add or remove parts

```text
In the same feature as BorrowBook, add a ReturnBook slice that produces a
BookReturned event, with a specification that returning a borrowed book produces
BookReturned.
```

```text
Remove the unused BookReserved event. First tell me what references it. Only
propose the removal if nothing does.
```

Asking for references first matters: a removal that leaves other declarations
pointing at something that no longer exists is rejected under the default `Safe`
reference policy, and the assistant can tell you what depends on it before it
proposes anything.

## Rename without a find and replace

```text
Rename the event BookBorrowed to BookCheckedOut everywhere it is used, keeping its
identity. Propose it and tell me how many files change.
```

The server renames the declaration, repairs the references it can prove, and keeps
comments, line endings and unrelated text untouched. It refuses a rename that would
collide with another name, one it cannot prove is safe, and one whose old or new
name appears in opaque text such as a code block. A refusal names the file, line
and column. Treat it as information, not as a cue to run a text replacement:

```text
You said the rename is blocked. Show me exactly where, and propose the smallest
change that removes the conflict. Do not do a global replace.
```

Event renames also add `id "<previous name>"` by default to preserve stored identity.
An existing pin stays unchanged; renaming back to that identity removes it. Only
set `eventNeverPersisted: true` on `propose-rename` when you know no events have
been stored. That option omits a new pin and removes a redundant pin equal to the current
name; a pin naming an earlier identity is kept.

## Change a property everywhere it flows

```text
Rename the AddBook command's title input to bookTitle. Use propose-property,
keep the connected event fields unchanged, and show me the mappings and fixtures
that change. Do not apply yet.
```

Select the target property from `read-ast`. Add `alsoRename` or `alsoRemove` handles
only for other properties you intend to change on its resolved mapping chain.
Being connected is not permission to rename or remove a field. Unrelated
same-named fields are listed in `property.notChanged`, never edited.

Rename repairs proven mappings and fixture assignments, including seed event values.
Seed values refer to the event's current generation; removing them reports
`fixtureRemoved` with `weakensAssertion: false`, not an assertion change.
If only one end of an
AutoMap link is renamed, the proposal inserts an explicit mapping to preserve the
flow. Remove refuses while readers remain; removed expected values are disclosed
with `weakensAssertion: true`. If the proposal cannot tell which `then` steps
inherit a removed example value, it refuses rather than report the assertion as
unaffected; fix the errors in that example or its specifications and retry.
Unselected upstream inputs remain follow-up
candidates rather than being deleted.

For selected event fields, choose `eventNeverPersisted: true` only when the current
generation has never been stored, or choose `newGeneration: true` to keep the old
shape and append one new generation. Historical generations cannot be edited;
extract inline events first. New generations require a transformation before
Chronicle deployment, which Screenplay does not yet express.

Review the required `property.generations` array in the proposal and in
`read-proposal`, including `view: "semantic-diff"`. It reports each selected event's
own `from`, `to` and `mode`, once per event in canonical address order. An in-place
edit has equal revisions. `[]` means no accepted planned event-contract edits,
not that the proposal succeeded. There is no singular `property.generation` field.

Structural changes need explicit `CanonicalizeTouchedDocuments` formatting consent;
trivia preservation never falls back to printing. Inspect exact bytes and identity
changes, then apply the reviewed proposal. See the [property planner contract](reference.md#property-change-planner).

## Add a property along a mapping chain

```text
Add a required String priority input to RegisterProject, map it into
ProjectRegistered and add it to ProjectSummary through AutoMap. Use "Normal"
in every existing fixture and query-result assertion. The current event
has never been persisted. Propose the change and show me the report before applying.
```

Read declaration and mapping-block handles at the open revision. For `action: "add"`,
select `owner`, `expectedOwnerName`, `name` and the authored `type` text. Each `hops`
entry selects another declaration with `{at, name}`, or a `produces`, `from` or
capture `append` block with `{at, value}`. A declaration hop uses the same type;
a mapping hop supplies an expression, not an inferred business rule. AutoMap
satisfies a same-named projection hop without an explicit `from` value.

Supply `fixtureValue` as literal source text (for example `"\"Normal\""` in JSON)
for a required property. The planner adds it to every existing command, event and
read-model fixture, example, result assertion and seed entry for the selected declarations,
including seed entries with no existing assignments.
`fixtureValue` is written to every fixture and assertion occurrence as-is. It does
not evaluate your mapping expressions: a non-identity `value` hop can make the
specifications fail even when the candidate binds. Run `run-specifications` with
the returned `proposalId` before applying it. This checks the retained candidate,
not the current model on disk.

An optional property (`optional: true`) needs no fixture edits. Missing values
refuse with `FixtureValueRequired`; missing producer or consumer decisions refuse
with `SourcesRequired`. Off-chain hops and existing property names refuse atomically.
Unsupported expressions or shared parameter spans are not guessed.

Even an optional event addition requires one event mode. `eventNeverPersisted`
edits the highest generation in place; `newGeneration` appends the current shape
plus the property as N+1. For the RegisterProject v2 model, these mean editing
generation 2 or appending generation 3. Both keep ESM v4; a new generation reports
`deployableWithoutTransformation: false` and `esmVersionChange: null`.

Use `CanonicalizeTouchedDocuments` for structural additions. Review `changed`
(`added` and `fixtureAdded`), `chainCandidates` and `generations` with `read-proposal`,
then inspect `view: "semantic-diff"` and approve `apply` only for the reviewed plan.

## Edit a specification

```text
Change the expected priority in RegisterProject's existing specification from
"Normal" to "High". Preserve comments and formatting, show the expectation diff,
and run the candidate before applying it.
```

Read the specification with `read-ast`, `kind: "SpecificationSyntax"`, its `name`
and `includeContent: true` at the opened revision. Use typed `propose-ast` edits;
for one assignment, replace its `PropertyMappingSyntax` with
`formatting: "PreserveTrivia"`. Review `read-proposal` with
`view: "semantic-diff"` for changed expectations, then call `run-specifications`
with the same `proposalId`. Pin the returned candidate `sourceRevision` as
`expectedSourceRevision` for subsequent pages. Failed, unsupported or unbound
results are not passes; correct the proposal before applying it.

Apply only the reviewed proposal, using its before workspace/catalog revisions.
For the typed members, case/example edits and specification retirement, follow
[Edit a specification](authoring-tools.md#edit-a-specification).

## Move things between files

```text
Move the Catalog feature into its own file under lending/catalog.play. Show me
the proposal; nothing should change except where the feature is written.
```

```text
Recommend a layout for this model, then reorganize it with one file per slice.
```

A layout change keeps declaration identities and keeps annotations such as
`// @public` beside the declaration they describe. A module or feature that is
written in several files is one logical declaration, so renaming its header changes
every fragment in a single proposal.

## Move a slice or feature to another parent

```text
Move the AddBook slice from Catalog to Acquisitions, keeping every identity and
its behavior. Use propose-move and show me the migrations before applying.
```

`propose-move` takes the target and destination parent as logical addresses, so a
feature written in several files moves every fragment together. Slices can move
to features; features can move to modules or other features. Declaration moves
between slices are deferred.

The proposal computes every assigned semantic and event-contract identity migration
and repairs qualified typed references, including `depends on` and interaction
navigation, command execution and query refresh targets. Named-behavior parameters
are not declaration names: their `uses` argument values are repaired instead.
Event names and `id` pins remain unchanged. It refuses collisions, capture, moves into descendants,
and changes to inherited authorization or resolved screen interaction targets,
even if the attachment text stays the same. An argument used for multiple target
domains also refuses. Literal placing imports need one unambiguous destination;
placing globs are refused. Restated headers inside an imported file stay authored
outside the file's actual placement scope.

Files stay where they are. Use `expand-layout` afterwards if you want to realign
the folders. Inspect `moveReport` in the proposal and `read-proposal`, including
`identityMigrations`, `retired` (always empty), `referenceRepairs` and
`fragmentsMoved`, then approve the existing `apply` tool. Trivia preservation is
the default and never falls back silently to canonical formatting.

## Fix a diagnostic

```text
List the diagnostics. For each one that has a repair, propose it. Show me what each
repair would change.
```

Some repairs reprint a whole file in canonical form, so whitespace can change. A
repair that would drop a comment is refused.

## Review and apply

Make review part of every prompt that changes something:

```text
Before applying, tell me: how many files change, whether any comments would be
dropped, and whether the model compiles after the change.
```

For each proposal the assistant can check:

- the changes and the before and after bytes of every document
- the number of dropped comments, and each dropped comment with its file, line and text
- whether the application compiles and whether it is ready to execute
- what changes in `.screenplay/identities.json`

Then approve it:

```text
Apply the proposal.
```

Your client asks for approval of `apply`. After a successful apply, every earlier
proposal and node handle is stale. If you want another change, the assistant opens
the model again at the new revision. A proposal made against an old revision is
rejected, never merged.

If you decide against a proposal:

```text
Discard that proposal.
```

## When something goes wrong

If an apply is interrupted, the server keeps a pending marker and refuses to edit
until the state is understood:

```text
The last apply was interrupted. Show me the workspace state and tell me whether
rolling back is safe. Do not recover anything yet.
```

Recovery is explicit and approved by you, and it refuses to overwrite bytes that
changed outside the server. See [identity state and recovery](recovery.md).

## Understand what the assistant did

The tool-level calls behind these prompts, with complete arguments, are in
[Authoring with the MCP tools](authoring-tools.md).
