---
title: Create a Screenplay model with an AI assistant
description: Create a Screenplay model from a description of your system. Open an empty workspace, review the source, then apply it and add behavior one slice at a time.
---

Create a Screenplay model by describing the system to your AI assistant,
reviewing the source it proposes and applying the changes you accept. Start
with an [installed MCP server](install.md) and an empty `specifications` folder.
Ask the assistant to open that folder with `open-workspace` and confirm the root
before proposing anything. With Docker, use the mounted path `/model`.

The prompts below describe a small lending library, the same domain as the
[Library sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Library).
Paste them into your client, or replace the domain details with your own. The
first proposal establishes the structure; later prompts add behavior.

## Start with the shape, not the syntax

Describe who uses the system and what they need to do. Start with the actions
you want to model rather than trying to specify every declaration at once. The
assistant can read the server's syntax schemas to discover the language.

```text
Use the screenplay MCP server to start a new application called Library.
Librarians add books to a catalog and sign up members. Members borrow and return
books. Create the modules and features you think this needs, with one empty
state-change slice for each thing a person can do. Propose the change and show me
what it would write before you apply anything.
```

The assistant opens the empty workspace, inspects the schemas it needs and
submits a proposal. An accepted proposal returns a `proposalId`, the planned
file changes and readiness information. No model source has been written.
Authoring acceptance means the source passes the authoring checks; it does not
mean every declaration can already run in the reference evaluator or a target
host.

## Review the proposal

Ask to see exactly what is about to be written:

```text
Show me the files in that proposal as they would be written, and tell me whether
any comments would be dropped.
```

The assistant uses `read-proposal` to retrieve the changes and the exact `after`
bytes of each document. Check the proposed names and actions against your
request, not just whether the language checks pass. There are no existing
comments to drop in an empty folder; later proposals may replace source that
contains comments.

This minimal Sales example shows the structure to look for; your Library
proposal should use names from the lending domain. For a new model, ask for
`command reads protected` to opt into
[protected command reads](../commands.md#what-reads-guarantees), and
`reaction batches atomic` so reactions added later use
[atomic production batches](../reactions.md#atomic-production-batches).
These application-root directives select the semantics; they are not implied
by creating the folder:

```screenplay
reaction batches atomic
command reads protected

module Sales

  feature Orders

    slice StateChange Register

      event OrderRegistered
```

## Apply it

```text
That looks right. Apply the proposal.
```

Keep the client's approval prompt enabled for `apply`, and approve only the
proposal you reviewed. The server requires an explicit tool call but does not
ask for human confirmation itself; your client controls that step.

Applying writes the `.play` files and persists stable identities in
`.screenplay/identities.json`. Keep both in source control so reopening the model
preserves those identities. Writes are journaled for recovery, not crash-atomic
across multiple files. If an apply is interrupted, inspect
[identity state and recovery](recovery.md) before trying again.

A proposal is bound to the revision it was created from. If the source changes
before apply, reopen the workspace and request a fresh proposal. Reusing an
already-applied proposal is also rejected.

## Grow the model one slice at a time

Add one action or view at a time so you can judge each proposal against a
specific business need. Use the names from your first proposal; if the assistant
chose a feature name other than `Catalog`, adapt the next prompt to match.

```text
In the Catalog feature, make the AddBook slice real. The command takes a book id,
an ISBN, a title and an author, and only a librarian may run it. It produces a
BookAdded event with the ISBN, title and author. An ISBN has 13 digits and a book
needs a title. Propose it and show me the source it would produce.
```

```text
Add a BorrowBook slice for members. It produces BookBorrowed with the book, the
member and when it was borrowed. Add a specification: given a member, when they
borrow a book, then BookBorrowed is produced.
```

```text
Add a state-view slice that lists the books currently on loan, built from
BookBorrowed and BookReturned, with a query that finds one loan by its book id.
Add a screen for it that a librarian can see.
```

The loan view is a read model: state built from events for a query to read. A
specification that checks one loan needs a key to identify that instance. Mark
the read-model property with trailing `key`, or let one unambiguous keyed query
identify it. If the view has a composite key, declare every part and supply them
all in read-model fixtures. Optional single-instance lookups use a `by` block
with every part; collection queries use equality filters. The projection maps
every part in an untyped key block. A target host must explicitly support the
composite-key executable contract before running it. See
[Read-model keys](../readmodels.md#keys).

For each addition, review the planned source and diagnostics before asking the
assistant to apply it. The server rejects syntax the language does not support
with diagnostic codes and locations rather than writing it to disk. Acceptance
still leaves you to check whether the model says what the business needs.

## Sketch before you commit to it

When you are not sure of the shape, ask the assistant to leave gaps instead of
guessing:

```text
Draft a Reservations feature. I have not decided what a member reserves, so
leave references to things that do not exist yet unresolved. Tell me what is
still missing.
```

Ask the assistant to use the draft reference policy for this proposal. It
allows references to declarations you have not written yet and reports those
gaps as debt to resolve later. Parse errors, identity conflicts and unsafe
bindings are still rejected. Review the gaps before applying the draft; they
are not completed behavior. See
[Authoring with the MCP tools](authoring-tools.md#create-the-first-typed-document).

## Choose how the files are laid out

A model can live in one file or many. Once the behavior has a useful shape,
choose a layout that makes the source easy to navigate:

```text
Recommend a file layout for this model, then reorganize it with one file per
feature. Show me the proposal before applying.
```

The assistant asks the server for a recommendation and proposes the layout
change. Review the file moves and any formatting changes before applying.
Splitting the files should preserve the model's meaning; it is not a reason to
rename its business concepts.

## Check your work

```text
Are there any diagnostics in the model? List each with its file and line, and
which slices have no specification that asserts anything.
```

Use [Edit a model](edit.md) to address diagnostics and missing assertions. To
check the structure visually, [view the model as an event model board](view.md).
A clean diagnostic report is not proof that the scenarios are correct; review
the specifications against the behavior you intended.
