---
title: Explore a Cratis Screenplay model with MCP
description: Inspect a Cratis Screenplay model through MCP to find declarations, trace dependencies and check specification gaps without changing files.
---

Use the Cratis Screenplay MCP server to find what a model contains, trace what a
change would affect, and locate missing specifications without changing files.
Everything on this page is read-only. The server returns compact pages of results,
so you can start with the application outline and ask for detail where you need it.

[Connect the server](install.md) to your model folder before using these prompts.
The examples use the lending library from [Create a model](create.md).

## Get oriented

```text
Use the screenplay server to give me an overview of this application: its
modules and features, how many slices, events and read models it has, and whether
it compiles cleanly.
```

The overview gives you counts and a `sourceRevision`, which identifies the source
snapshot behind the answer. Ask about a module next to see its features and slices
without loading the whole model:

```text
Walk me through the Lending module: each feature, and for each slice what the
command does and which events it produces.
```

## Find a declaration

```text
Where is BookBorrowed declared? Show me its properties and every file that
contributes to it.
```

```text
Search for anything with "Loan" in its name and tell me what kind of declaration
each match is.
```

Use the declared spelling: names are case-sensitive. A module or feature split
across files appears as one logical declaration with every location listed, so you
can find all of its source before planning an edit.

## Follow the connections

```text
Which slices and read models use the BookBorrowed event? Tell me whether each one
produces it or consumes it.
```

```text
What does the BorrowBook command depend on, directly? And what would be affected if
I changed it?
```

Read the coverage limits alongside the references. The server reports resolved
references and places it cannot see into, such as code blocks and expressions.
Those gaps tell you what a rename or removal might miss; an empty list alone is
not proof that nothing else uses a name.

## See how modules and features depend on each other

```text
Show the dependency graph between modules, with the slices and source references
behind each edge. Then show feature cycles and suggest a story order. Do not edit
anything.
```

The assistant calls `dependency-graph` with its default module → module view,
then `view: "cycles", from: "feature", to: "feature"` and `view: "order"`.
For a narrower question:

```text
Which features depend on the Lending.Loans feature? Show incoming dependencies
and their evidence, excluding specifications.
```

Use feature → feature levels, `scope: "Lending.Loans"` and `direction: "incoming"`.
You can also compare mixed levels, such as feature → module, or use `to: "context"`
for imported facts. Follow `nextOffset` with the returned `sourceRevision` as
`expectedSourceRevision`; raise `evidenceLimit` only for edges you need to inspect.

Cycles and suggested order include read-model decisions, not just event flow.
Library's Catalog and Loans form a cycle because Catalog uses loan events and
BorrowBook reads CatalogEntry. This does not add a `PLAY0517` diagnostic. An order
is a suggestion only; ambiguity and unresolved references remain visible. Set
`includeTestOnly: true` when you want specification dependencies too. See the
[dependency-graph reference](reference.md#dependency-graph) for exact options and
coverage.

## Check the specifications

```text
Which slices have no specification that asserts anything? Group them by feature.
```

```text
Show me the specifications that seed a BookAdded event as their starting state,
with the values they use.
```

A specification counts as asserting something when it declares a `then`, including
`then denied`. Use this list to find missing expectations. It does not run the
specifications or tell you whether they pass.

## Read what is on disk

```text
Show me the exact source of the file that declares AddBook, including comments.
```

The assistant reads the original file bytes, so comments and line endings are
preserved. Ask for the merged view when you want one canonical document for the
whole application instead.

## Look for problems

```text
List the diagnostics for the whole application, errors first, with file and line.
```

```text
Are there any problems in just the Catalog feature?
```

An invalid file produces a partial index, never a silently complete one. The
assistant reports parse errors and what could and could not be indexed.

## Keep big questions bounded

On a large model, scope the question before asking for detail:

```text
Without reading every file, tell me which feature has the most slices and show me
only the slices of that feature.
```

Large answers are refused with a smaller alternative rather than truncated. If an
assistant reports that a page was rejected, ask it to narrow the scope or page
through the results.

To inspect these relationships visually, [view the event model board](view.md) in
a client that supports it. When you have identified a change, [propose and review
an edit](edit.md) before applying it.
