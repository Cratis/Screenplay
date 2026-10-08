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
and repairs qualified typed references, including `depends on`. Event names and
`id` pins remain unchanged. It refuses collisions, capture, moves into descendants,
and changes to inherited authorization or screen interaction bindings. Literal
placing imports need one unambiguous destination; placing globs are refused.

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
