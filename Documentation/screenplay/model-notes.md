---
title: "Screenplay model notes: track workshop questions"
description: "Keep workshop questions, tasks and review context anchored to a Screenplay model, then record the evidence that resolves each question."
---

Cratis Screenplay model notes keep unresolved questions, tasks and review context
beside the model elements they concern, without changing executable behavior. Use
them to carry workshop questions into the next modeling session and record what
settled each one.

For example, “Can a member cancel after check-in?” is a question to resolve, not
behavior to guess. Anchor it to the relevant slice so reviewers can find it while
working on cancellation. The stable identity keeps the note attached when that
element is renamed or moved through the authoring tools.

## Choose the right home

Use a question for an unresolved modeling choice, a task for work to do,
a note for review context you want to retain, and an issue to link an element to
the work item that tracks it in GitHub, Jira, Linear or another tracker. All four
kinds can be open or resolved. They live in `.screenplay/notes.json`, a sidecar file kept alongside
the model rather than inside `.play` syntax.

A `description` states what an element means. A `documentation` block explains
reasoning without tracking open work. A decision record preserves a consequential
choice and its owner/date. Model notes link to those artifacts; they do not replace
them. A source comment is not a durable work item: authoring proposals can drop it.

Editing notes does not itself change source, compiler verdicts, executable semantic
model bytes or their revisions. Notes have their own revision, separate from the
workspace and identity catalog. Neither compiler reads the sidecar, and an open
question produces no PLAY diagnostic. To require answers before proceeding, use
the [unresolved-question gate](#gate-on-unresolved-questions).

## Anchors and orphans

An anchor identifies what a note concerns: a stable `semanticId` for a model
element, or `{ "application": true }` for the application as a whole.
The server looks up that identity in the current identity catalog, so renamed and
moved elements retain their notes without rewriting the file. `lastAddress` is only a
last-known display hint; the server refreshes it when writing notes and never uses
it to resolve an anchor.

Modules, nested features and slices have stable identities even while the model
does not bind, provided every document parses and has resolved import placement.
Select them by dotted address and kind, typed address or current source handle:
the note anchors to that container, without substitution. Fixing binding through
a Model Context Protocol (MCP) proposal preserves its ID and origin. Renaming or moving it before first
binding carries the ID with it, just as for a bound model. Opening computes new
container assignments in memory; only apply persists them in `identities.json`,
including when applying a notes-only proposal.

Commands, events, read models and other in-slice declarations still need binding
to gain their own identities. Select syntax-only constructs by their source handle or dotted address and kind.
When a construct has no semantic identity, the server substitutes its nearest
identified owning slice, feature, module or application and reports that substitution.
Name the particular construct in the text. Document and authoring-key anchors are
not supported.

If a document does not parse or place, no new container assignments are made
anywhere in the model. `open-workspace` and `workspace-state` report those documents
and the unidentified containers. Duplicate slices report `ambiguous`; an ID already
held by another assignment reports `idCollision`. New targeting refuses rather
than substituting around these problems, including targeting descendants of an
ambiguous container. Existing assignments and notes are retained: resolved notes
on an ambiguous container or its descendants carry `ambiguous: true`, not an orphan
state. Read them without a target filter, or by their existing semantic ID.

Removing an identified element leaves its note orphaned: the question is retained,
but its target is no longer in the catalog. The state is computed
on read, never stored. Its current address is null and `orphanReason` is `unknown`
when the catalog no longer contains the identity. Source proposals report the IDs
of newly orphaned notes in `orphanedNotes` before apply. Notes are never silently
deleted; reanchor or remove one explicitly after reviewing it.

Re-adding an address recreates its deterministic identity when source is complete
and unambiguous. A note resolves again only if that is its exact former ID. An ID
carried to the removed address by a rename or assigned externally is different;
use an explicit reanchor for those notes. Manual AST removal still needs
`retiredSemanticAddresses`, and manual renames need `semanticRenames`.

## References and resolutions

References are identifiers with optional absolute HTTP(S) URLs. For example,
`{ "id": "D123", "url": "https://example.org/decisions/D123" }` can identify your
project's external decision register. The server never fetches the URL.
`list-model-notes` with `ref: "D123"` finds both notes citing the record and notes
resolved by it.

A null `resolution` means open. A resolution records `by`, explanation `text`,
unverified `author`, and server UTC `at`. The supported evidence is:

| `by` | Evidence |
| --- | --- |
| `specification` | Required `semanticId` of the model evidence |
| `description` | Required `semanticId` of the described element |
| `decision` | A recorded modeling decision; optional `semanticId` or `ref` |
| `external-decision` | Required `ref`, such as D123 in your decision register |
| `dismissed` | Text explaining why the question or task no longer applies |

Resolving a question does not implement its answer. Once cancellation behavior is
agreed, change the model and add the relevant specification; then resolve the note
with that specification's ID. If the answer only clarifies terminology, update the
description and resolve against that element. If D123 settles policy outside the
model, resolve with `by: "external-decision"` and its reference. Reopen the note
if the answer stops being adequate.

## Link elements to issues

An `issue` note links the element it is anchored to with one or more issues in an
external tracker, so the work that builds or changes a slice, a command or a screen
is one click away from the model. Every reference of an issue note names its tracker:

```json
{
  "kind": "issue",
  "text": "Build the cancellation flow",
  "anchor": { "semanticId": "..." },
  "refs": [
    { "tracker": "github", "id": "Cratis/Shop#412", "url": "https://github.com/Cratis/Shop/issues/412" },
    { "tracker": "jira", "id": "SHOP-77" }
  ]
}
```

`tracker` is `github`, `jira`, `linear` or `other`, `id` is the issue key as the
tracker shows it, and `url` is optional. An issue note needs at least one reference,
and a reference without a tracker is refused. Because the note is anchored by stable
identity, the link survives renames and moves. `list-model-notes` with `kind: "issue"`
lists every link and `ref: "SHOP-77"` finds the elements an issue covers. The server
never calls the tracker: keeping the issue's own status current is the tracker's job,
and resolving the note records that the link no longer needs attention.

## Review and commit notes

Use [the MCP notes tools](mcp/reference.md#model-notes) to
read the current notes and propose an atomic batch. Inspect the candidate with
`read-proposal view=notes` before applying it. A batch can add, edit, resolve,
reopen, reanchor or remove notes.

Proposals check the workspace, catalog and notes revisions you read. If the notes
change before apply, read them again and make a new proposal rather than
reusing the stale one. This keeps one reviewer's update from overwriting another's.
The server supplies random `note_` IDs and UTC instants. `author` is free text,
not authentication or an authorization claim.

The sidecar is committed content. **Do not put secrets or personal data in note
text, author names, references or resolutions.** Prefer team handles or roles
where needed. Commit the file with the model so other reviewers see the same work.

Canonical JSON uses stable key order, LF, a trailing newline, notes sorted by ID,
and one note object per line. Concurrent additions usually merge on separate
lines; edits to the same note still require a deliberate merge. Duplicate IDs,
conflict markers, invalid notes, an unsupported schema/version or another
application's identity make the file unreadable. Opening the model still works;
note tools refuse with `UnreadableNotes` and never repair or rewrite the file.
Resolve the conflict or restore known-good content, then read the notes again.

The published [notes v1 JSON Schema](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/notes-v1.schema.json) describes the wire file.
Its header is `schema: "cratis.screenplay.notes"`, `schemaVersion: 1`, and
`applicationIdentity` in the identity catalog's representation. The independent
`notesRevision` is `nrev1:` plus lowercase SHA-256 of canonical bytes;
`nrev1:absent` denotes an absent file, not an empty notes document.

These are shared conventions for future `.screenplay/` sidecars: a named schema
and integer version with a published JSON Schema, canonical JSON, and writes under
one `pending.json` journal. Nonsemantic state never blocks opening the model.
State governing model meaning must specify its own blocking rules. See
[identity state and recovery](mcp/recovery.md) for interrupted writes.

## Gate on unresolved questions

Use `report` to review what is still unanswered before the next workshop. Use
`fail` when your workflow requires the selected scope's questions to be resolved.
After [installing the CLI](tool.md#install-the-cli), run:

```bash
screenplay path/to/model --open-questions report
screenplay path/to/model --scope Library.Loans.CheckIn --open-questions fail
```

The report emits one `info` finding with code `model-note.open-question` per open
question. Findings on declarations group under their owning slice or container.
The scope includes its descendants and ancestor notes, including application notes;
an unscoped report also includes orphaned open questions. If identity state is
absent, the CLI explains that notes cannot be scoped and reports all open questions
unscoped as orphaned with reason `identity-state-absent`, rather than hiding them.

`report` does not fail because questions remain. `fail` exits `1` when the selected
set contains an open question. Unreadable notes exit `2` only when the flag is given.
With no flag, notes are ignored and ordinary compilation and `--warnaserror` keep
their existing behavior. Tasks and ordinary notes do not fail the question gate.

When the report identifies a question, return to its model element, make the agreed
change and resolve the note with its evidence. For source changes that also move
or rename elements, follow the [revision-bound workspace contract](folders.md#propose-revision-safe-workspace-changes)
to preserve their identities.
