---
title: Model notes
description: Keep questions, tasks and review notes anchored to a model without changing its executable behavior.
---

A workshop asks, “Can a member cancel after check-in?” The answer is not settled yet,
and burying the question in a chat thread makes it easy to lose. Model notes keep
that question beside the model element, with an explicit resolution when the team
answers it. A rename or move does not detach the question.

## Choose the right home

Use a **question** for an unresolved modeling choice, a **task** for work to do,
and a **note** for review context you want to retain. All three kinds can be open
or resolved. They live in the committed `.screenplay/notes.json` sidecar, not in
`.play` syntax.

A `description` states what an element means. A `documentation` block explains
reasoning without tracking open work. A decision record preserves a consequential
choice and its owner/date. Model notes link to those artifacts; they do not replace
them. A source comment is not a durable work item: authoring proposals can drop it.

Notes do not change source, compiler verdicts, executable semantic model bytes,
semantic revision, workspace revision or catalog revision. Nothing in either
compiler reads the sidecar. There is no PLAY diagnostic for an open question.

## Anchors and orphans

A note anchors to a stable `semanticId`, or to `{ "application": true }`.
The server resolves that identity against the current catalog, so renamed and moved
elements retain their notes without rewriting the file. `lastAddress` is only a
last-known display hint; the server refreshes it when writing notes and never uses
it to resolve an anchor.

Select syntax-only constructs by their source handle or dotted address and kind.
When a construct has no semantic identity, the server substitutes its nearest
identified owning slice, feature, module or application and reports that substitution.
Name the particular construct in the text. Document and authoring-key anchors are
not supported.

Removing an identified element leaves its note **orphaned**. The state is computed
on read, never stored. Its current address is null and `orphanReason` is `unknown`
when the catalog no longer contains the identity. Source proposals report the IDs
of newly orphaned notes in `orphanedNotes` before apply. Notes are never silently
deleted; reanchor or remove one explicitly after reviewing it.

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

## Review and commit notes

Use [the MCP notes tools](mcp/reference.md#model-notes) to list notes, propose an
atomic batch, inspect `read-proposal view=notes`, then apply. The server supplies
random `note_` IDs and UTC instants. `author` is free text, not authentication or an
authorization claim. A batch can add, edit, resolve, reopen, reanchor or remove notes.

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

The published [notes v1 JSON Schema](notes-v1.schema.json) describes the wire file.
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

After [installing the CLI](tool.md#install-the-cli), report the model's questions:

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
