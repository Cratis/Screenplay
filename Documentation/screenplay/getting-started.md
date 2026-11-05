---
title: Write your first Cratis Screenplay model
description: This Cratis Screenplay tutorial models borrowing a library book in a .play file and uses editor diagnostics to find and fix a missing policy.
---

Write a Cratis Screenplay model for borrowing a library book, from the command that requests it to the event and the screen showing books on loan. You will use editor completions and diagnostics as you connect the declarations, including finding and fixing a missing authorization policy.

This tutorial ends with a readable `.play` model. It does not run or deploy an application.

## Before you start

You need [Node.js](https://nodejs.org) 20 or newer and Yarn available in your terminal. Clone the repository, then work from its root for the commands below:

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

Open [http://localhost:9200](http://localhost:9200) after `yarn dev` starts the editor. It loads an invoicing sample. You can inspect its highlighting, including embedded C#, TypeScript, and React blocks, before replacing it with your own model.

> [!TIP]
> To work in VS Code instead, open this repository and press **F5**. The Extension Development Host opens with `.play` highlighting, completions, hover documentation, and diagnostics. Use a new `.play` file there for the model below.

## Model a slice

Clear the standalone editor's sample, or start a new file in VS Code. The `Library` module will contain a `Lending` feature, with a slice for borrowing a book. A slice groups declarations for one behavior.

Begin the root file with two execution choices: `command reads protected` selects [protected command reads](commands.md#what-reads-guarantees), and `reaction batches atomic` selects [atomic production batches](reactions.md#atomic-production-batches). These determine how reads and reaction effects are handled. They require executable semantic model (ESM) v25 or later; any target you later choose must admit that version. Existing models keep their mode unless you opt in.

Indentation defines the structure: each declaration owns what is indented beneath it. Type this outline:

```screenplay
reaction batches atomic
command reads protected

concept BookId : Uuid
concept MemberId : Uuid

module Library

  feature Lending

    slice StateChange BorrowBook
```

After `slice`, completions offer `StateChange`, `StateView`, `Automation`, and `Translate`. Choose `StateChange` here because borrowing a book accepts a command and records what happened. The `BookId` and `MemberId` concepts give those identifiers distinct types based on `Uuid`.

## Add the command and the event

Replace the empty `BorrowBook` slice with the declarations below, keeping the module and feature above it. The command takes a book and member identifier; the event records the borrowing and when it happened.

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

Read the command from top to bottom: `authorize` names who may call it, `validate` states the input rule, and `produces` maps values into the event. `$context.occurred` supplies the occurrence time. The event-source identity belongs to the surrounding context rather than being an identity declared by the event payload.

## Watch a mistake get caught

The editor underlines `IsMember` because the model refers to a policy that does not exist yet. Diagnostics also flag unknown slice or primitive types, undeclared event references, and tab indentation.

Add the policy above `module Library` to clear the undeclared-policy diagnostic:

```screenplay
policy IsMember
  require authenticated
```

Despite its name, this policy requires authentication, not a library-membership check. [Policies](policies.md) explains how to express authorization rules. Reuse `BookId` and `MemberId` elsewhere in the model when you need those value types; see [Concepts](concepts.md).

## Read it back on a screen

To describe what a reader sees after borrowing a book, add a `StateView` slice under `feature Lending`, alongside `BorrowBook`. It builds a read model, a queryable view of the recorded events, and connects it to a screen:

```screenplay
    slice StateView OnLoan
      readmodel OnLoanReadModel
        bookId     BookId key
        borrowedBy MemberId
        borrowedAt DateTime

      query BooksOnLoan => OnLoanReadModel[]

      projection OnLoan => OnLoanReadModel
        from BookBorrowed key bookId
          borrowedBy = borrowedBy
          borrowedAt = borrowedAt

      screen OnLoan
        data OnLoanReadModel[] via query BooksOnLoan
```

The `readmodel` declares the view's fields, with `bookId` identifying each instance. The `projection` describes how `BookBorrowed` events fill those fields. It uses the embedded [Projection Declaration Language](projections/index.md); the same-named `bookId` is mapped automatically. The query exposes the collection, and the screen binds to that query. Keep the `readmodel` declaration: without it, the projection has no declared target.

## What you've built

You can now follow the borrowing behavior through two connected slices: a command records an event, and a projection describes the view a screen reads. Authorization and validation sit beside the command. The model is readable by people and AI agents, but editor feedback alone does not establish executable binding or target support.

Use [Modules, features, and slices](slices.md) to decide where the next behavior belongs, then [Specifications](specifications.md) to describe its expected outcomes. The [language overview](overview.md) is the reference map. For the modeling trade-offs, read [Why Screenplay](./why-screenplay.mdx); use the [Glossary](./glossary.md) for unfamiliar terms.

To see a supported model run, follow the separate [Cratis Stage sandbox walkthrough](/stage/docker/), which supplies its own model and prerequisites. Stage is not a production host: state is held in memory and lost on container restart. Check admission diagnostics and bind published ports to `127.0.0.1`.
