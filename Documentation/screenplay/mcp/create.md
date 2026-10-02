---
title: Create a model
description: Prompts that take an assistant from a description of a system to a reviewed, applied Screenplay model, then grow it slice by slice.
---

This guide takes you from an empty folder to a model on disk. Every prompt below is
meant to be pasted as written; change the domain details to yours. It assumes you
have [installed the server](install.md) with its root at an empty `specifications`
folder.

The examples describe a small lending library, the same domain as the
[Library sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Library).

## Start with the shape, not the syntax

Describe who uses the system and what they need to do. The assistant reads the
server's syntax schemas, so you do not have to explain the language.

```text
Use the screenplay MCP server to start a new application called Library.
Librarians add books to a catalog and sign up members. Members borrow and return
books. Create the modules and features you think this needs, with one empty
state-change slice for each thing a person can do. Propose the change and show me
what it would write before you apply anything.
```

The assistant opens the workspace for the empty root, inspects the schemas it
needs and sends a proposal. The server answers with a `proposalId`, whether the
result compiles and how many files it would write. Nothing is on disk yet.

## Review the proposal

Ask to see exactly what is about to be written:

```text
Show me the files in that proposal as they would be written, and tell me whether
any comments would be dropped.
```

A first proposal drops no comments because there are none yet. The assistant reads
the proposal's changes and the `after` bytes of each document. For a brand-new
model it can look like this:

```screenplay
module Sales

  feature Orders

    slice StateChange Register

      event OrderRegistered
```

## Apply it

```text
That looks right. Apply the proposal.
```

Your client asks for approval of `apply`; accept it. The server writes the `.play`
files and `.screenplay/identities.json` together. Commit both. Because `apply` is
bound to the model's revision, a second `apply` of the same proposal is rejected;
ask for a fresh proposal instead.

## Grow the model one slice at a time

Prefer several small prompts over one large one. Each becomes its own reviewed
proposal.

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
BookBorrowed and BookReturned. Add a screen for it that a librarian can see.
```

The server validates every proposal against the language's syntax schemas, so a
construct the language does not have is rejected with a reason instead of being
written to disk.

## Sketch before you commit to it

When you are not sure of the shape, ask the assistant to leave gaps instead of
guessing:

```text
Draft a Reservations feature. I have not decided what a member reserves, so
leave references to things that do not exist yet unresolved. Tell me what is
still missing.
```

This uses the draft reference policy: unresolved references are allowed and
reported as debt, while parse errors, identity conflicts and unsafe bindings are
still rejected. See [Authoring with the MCP tools](authoring-tools.md#create-the-first-typed-document).

## Choose how the files are laid out

By default a model can be one file or many. Say which you want:

```text
Recommend a file layout for this model, then reorganize it with one file per
feature. Show me the proposal before applying.
```

The assistant asks the server for a recommendation and then expands the layout.
Reading the model afterward gives the same answers whichever layout you choose.

## Check your work

```text
Are there any diagnostics in the model? List each with its file and line, and
which slices have no specification that asserts anything.
```

Fix what it finds with [Edit a model](edit.md), and look at the result with
[View a model](view.md).
