---
title: Getting started
description: Write your first Screenplay .play file and watch the editor highlight it, complete it, and flag mistakes live — in a few minutes.
---

By the end of this page you'll have a `.play` file that models a small feature — a command, the event it produces, and a screen that reads it — open in an editor that **highlights** the language, **completes** it as you type, and **flags mistakes live**. That editor is what this repository ships; running a `.play` as an application is [Stage](./faq.md#what-are-stage-and-studio)'s job, and this tutorial gets you to the point where a Screenplay is ready for it.

## Before you start

You need [Node.js](https://nodejs.org) 20 or newer. Clone the repository and open a terminal in its root:

```bash
git clone https://github.com/Cratis/Screenplay.git
cd Screenplay
```

## Run the editor

Install the workspace and start the standalone editor:

```bash
yarn install
yarn build
yarn dev
```

`yarn dev` serves the editor at [http://localhost:9200](http://localhost:9200). It opens on a complete sample — the invoicing `.play` — so the very first thing you see is the language rendered with full syntax highlighting, including the embedded C#, TypeScript, and React blocks.

> [!TIP]
> Prefer your own editor? The same language support ships as a VS Code extension. Press **F5** in VS Code with this repository open and an Extension Development Host launches with `.play` highlighting, IntelliSense, hover, and diagnostics.

## Model a slice

Clear the editor and start a bounded context. A `.play` file is indentation-based — structure follows the offside rule, and a construct owns everything indented beneath it. Type the outline of a module with one feature and one slice:

```screenplay
module Library

  feature Lending

    slice StateChange BorrowBook
```

As you type `slice`, the completions offer the four slice types — `StateChange`, `StateView`, `Automation`, `Translate`. `StateChange` is the one that accepts a command and records what happened.

## Add the command and the event

Inside the slice, declare the command, the rule that guards it, and the fact it produces:

```screenplay
    slice StateChange BorrowBook
      command BorrowBook
        bookId     BookId
        borrowedBy MemberId
        authorize  IsMember
        validate
          bookId not empty  message "A book is required"
        produces BookBorrowed
          bookId       = bookId
          borrowedBy   = borrowedBy
          borrowedAt   = $context.occurred

      event BookBorrowed
        bookId     BookId
        borrowedBy MemberId
        borrowedAt DateTime
```

Notice what the file is saying in one read: who may call it (`authorize`), what has to be true (`validate`), and the past-tense fact it appends (`produces` → `event`). The event never carries an identity of its own — `$context.occurred` and the event source come from the surrounding context, not the payload.

## Watch a mistake get caught

That slice references a policy, `IsMember`, that you haven't declared. The language service knows it: `IsMember` is underlined with a diagnostic — *reference to an undeclared policy*. This is the live feedback the editor gives you for the whole language — unknown slice types, unknown primitive types, references to undeclared policies or events, and tab indentation are all flagged as you type.

Declare the policy at the top of the file, above the `module`, and the diagnostic clears:

```screenplay
policy IsMember
  require authenticated
```

> [!NOTE]
> Concepts like `BookId` and `MemberId` are strongly-typed value types. Declare them once — `concept BookId : Uuid` — and every construct that uses them is typed end to end. See [Concepts](concepts.md).

## Read it back on a screen

A command that records facts is only half a feature. Add a second slice — a `StateView` — that projects the event into a read model and puts it on screen:

```screenplay
    slice StateView OnLoan
      query BooksOnLoan => OnLoanReadModel[]

      projection OnLoan => OnLoanReadModel
        from BookBorrowed key bookId
          borrowedBy = borrowedBy
          borrowedAt = borrowedAt

      screen OnLoan
        data OnLoanReadModel[] via query BooksOnLoan
```

The `projection` uses the embedded [Projection Declaration Language](projections/index.md) to fold events into a read model; the `query` exposes it; the `screen` renders it. No update code, no proxy to regenerate by hand — the whole read path is declared in six lines.

## What you've built

You have a `.play` file that models a complete slice of behavior — command, event, projection, query, and screen — with authorization and validation, all in one file the editor understands. That file is a Screenplay ready to hand to Stage.

Where to go next:

- **[Language overview](overview.md)** — the design principles and the full map of constructs.
- **[Modules, features, and slices](slices.md)** — the four slice types and what goes in each.
- **[Why Screenplay](./why-screenplay.md)** — the reasoning behind modeling a bounded context in one file, and when not to.
- **[Glossary](./glossary.md)** — the vocabulary, defined once.
