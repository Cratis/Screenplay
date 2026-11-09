---
title: "Storylines"
description: "Name an ordered path through a Screenplay model, such as a user journey across modules, features and slices, to review it, show it on a board and walk it."
---

# Storylines

A user journey, such as "a customer returns an item", usually crosses several modules, features and slices. A
`storyline` gives that path a name and keeps it with the model, so it can be reviewed, shown on a board and walked
step by step. Each step names one element by its kind and its name.

A storyline is authoring metadata. It never changes what the model executes: the executable model, the semantic
revision and every generated artifact are the same with or without it.

## Syntax

```screenplay
storyline <Name> ["<title>"]
  [description "<text>"]
  <kind> <reference> ["<note>"]*
```

- `<Name>` is unique among the application's storylines. The optional title is what a board shows; the name
  stands in when there is none.
- Each step is one line. `<kind>` is `module`, `feature`, `slice`, `command`, `event`, `readmodel`, `query`,
  `screen` or `reaction`. The order of the lines is the order of the path.
- `<reference>` is the element's name. When the name alone matches several elements of that kind, qualify it
  with the module, feature or slice it sits in, such as `Requests.RequestReturn`.
- The optional note says what happens at that step.

Storylines are declared at the top level of a document. In a [folder model](folders.md) any file can declare one,
and each storyline is declared in exactly one file.

## Example

```screenplay
concept ItemId : Uuid

module Returns
  feature Requests
    slice StateChange RequestReturn
      command RequestReturn
        itemId ItemId
        produces ReturnRequested
          itemId = itemId

      event ReturnRequested
        itemId ItemId

    slice StateView PendingReturns
      readmodel PendingReturn
        itemId ItemId

      projection PendingReturn
        from ReturnRequested
          key itemId

      query AllPending => PendingReturn[]

storyline ReturnAnItem "A customer returns an item"
  description "From the request to the list the warehouse works from"
  feature Returns.Requests
  command RequestReturn "The customer asks to return the item"
  event ReturnRequested
  readmodel PendingReturn
  query AllPending "The warehouse sees the return"
```

## Renames, moves and missing elements

A step is a reference like any other, so renaming or moving the element it names rewrites the step. Renaming
the command `RequestReturn` through `propose-rename` or `screenplay rename` rewrites `command RequestReturn`, and
moving the feature `Requests` to another module rewrites `feature Returns.Requests` to the new path.

A step that names nothing, for example after the element was deleted, is warning `PLAY0942`. It does not stop the
model from compiling: remove the step or point it at the element that replaced it. A step whose name matches
several elements of its kind is warning `PLAY0198`; qualify it. See [diagnostics](diagnostics.md#storylines).

## Tools

- `list-storylines` (MCP) lists every storyline with its steps in order. Each step reports its kind, its
  reference, its note, and the element it resolves to: kind, address, location and semantic identity. A step
  that names nothing or is ambiguous reports `missing` or `ambiguous` instead.
- Storylines are AST nodes (`StorylineSyntax` and `StorylineStepSyntax`), so `read-ast` reads them and
  `propose-ast` creates, reorders, edits or removes them. `propose-source` edits them as text.
- `search-declarations` and `declaration-details` find a storyline by name with kind `Storyline`.
- `export-workspace` and every printed document keep storylines, so they survive export and import.
