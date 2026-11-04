---
title: "Getting started"
description: "Write a complete library model, check three reference specifications with the released tool, and recover from a deliberate binding error before optionally viewing the board."
---

A librarian may add a book with a valid ISBN; a member may not. Model that rule, the request, and the resulting fact in one file. Then run three specifications without generating an application or starting an event store.

This is the Cratis Platform's optional model-first path. You can use Arc and Chronicle directly without Screenplay. Here you will get a **reference specification result**, not a deployed app. Allow about 15 minutes; no AI client, Stage, Studio, or Cratis CLI is required.

## 1. Install the released tool

You need the **.NET 10 SDK**, a text editor, and access to NuGet. The commands below use a POSIX shell on macOS or Linux. Start in an empty folder so the sample cannot be confused with an existing model:

```bash
mkdir library-model
cd library-model
dotnet tool install Cratis.Screenplay.Tool --tool-path .tools --version 4.259.0
.tools/screenplay --version
```

The version output should identify **4.259.0**. This walkthrough pins that release so the syntax and reference runner stay aligned. If the tool is already in `.tools`, inspect its version rather than installing over it; use `dotnet tool update Cratis.Screenplay.Tool --tool-path .tools --version 4.259.0` to select this version deliberately.

Use `.tools/screenplay` throughout, not an unrelated global `screenplay` or the separate `cratis` CLI. If installation or startup fails, check the SDK with `dotnet --list-sdks` and resolve that prerequisite before proceeding.

## 2. Save the complete model

Create **`library.play`** in `library-model` and copy the entire block below. It is a complete single-file application with no imports or implementation attachments, adapted from the maintained [Library sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Library).

```screenplay
domain Library

reaction batches atomic
command reads protected

concept BookId     : Uuid
concept Isbn       : String
concept BookTitle  : String
concept AuthorName : String

policy IsLibrarian
  require role "Librarian"

module Lending
  feature Catalog
    slice StateChange AddBook
      command AddBook
        bookId BookId identifier
        isbn   Isbn
        title  BookTitle
        author AuthorName
        authorize IsLibrarian
        validate
          isbn matches "^[0-9]{13}$"  message "An ISBN has 13 digits"
          title not empty             message "A book needs a title"
        produces BookAdded
          for bookId
          isbn   = isbn
          title  = title
          author = author

      event BookAdded
        isbn   Isbn
        title  BookTitle
        author AuthorName

      specification AddingABook
        given caller
          authenticated
          role "Librarian"
        when AddBook
          bookId = "0f8fad5b-d9cb-469f-a165-70867728950e"
          isbn   = "9780262033848"
          title  = "Introduction to Algorithms"
          author = "Cormen"
        then BookAdded
          for "0f8fad5b-d9cb-469f-a165-70867728950e"
          isbn   = "9780262033848"
          title  = "Introduction to Algorithms"
          author = "Cormen"

      specification RejectingAMalformedIsbn
        given caller
          authenticated
          role "Librarian"
        when AddBook
          bookId = "0f8fad5b-d9cb-469f-a165-70867728950e"
          isbn   = "12345"
          title  = "Introduction to Algorithms"
          author = "Cormen"
        then error "An ISBN has 13 digits"

      specification RefusingAMemberAddingBooks
        given caller
          authenticated
          role "Member"
        when AddBook
          bookId = "0f8fad5b-d9cb-469f-a165-70867728950e"
          isbn   = "9780262033848"
          title  = "Introduction to Algorithms"
          author = "Cormen"
        then denied
```

Read the file from the business boundary down:

- `domain Library` names the business area; it does not provision infrastructure. `Lending.Catalog.AddBook` is the module, feature, and slice address.
- The two application modes select atomic reaction batches and protected command reads. This small example has no reactions or reads, but retains those modes as its behavior grows. Atomic batches require ESM v22; protected reads require v25. A consumer must admit those capabilities rather than silently relax them.
- Concepts name domain values. `BookId` is not just an arbitrary UUID in the design; `Isbn` has a business meaning even though its underlying type is `String`.
- `AddBook` is an intent. Authorization requires the librarian role; validation checks 13 digits and a nonempty title. This ISBN rule is deliberately small: it does **not** verify the ISBN checksum or catalog uniqueness.
- `BookAdded` is the fact. `for bookId` routes it to the book's event source; the payload contains ISBN, title, and author, not a duplicate routing identifier.
- Each specification supplies a caller and a request, then states the expected outcome. No `given` events are needed for this command.

The first scenario expects a routed `BookAdded` fact with exactly the supplied values. The second expects the ISBN validation message. The third expects denial because a member lacks the librarian role.

## 3. Compile the exact file

From `library-model`, run:

```bash
.tools/screenplay --warnaserror library.play
```

Expect exit **0** and no source errors or warnings. Compilation checks the declarations and references; it does not run the scenarios. If this fails, fix the first reported diagnostic before continuing. Check indentation, spelling, and that you copied the whole file rather than only the slice.

## 4. Run the reference specifications

```bash
.tools/screenplay test library.play --format json
```

The command evaluates the supported declarative behavior deterministically **in memory**. Look for exit **0**, `outcome: "passed"`, and these report fields:

| Field | Expected value |
| --- | --- |
| `discovered` | `3` |
| `selected` | `3` |
| `executed` | `3` |
| `passed` | `3` |
| `failed` | `0` |
| `unsupported` | `0` |

In `results`, find `Lending.Catalog.AddBook.AddingABook`, `Lending.Catalog.AddBook.RejectingAMalformedIsbn`, and `Lending.Catalog.AddBook.RefusingAMemberAddingBooks`, each with `outcome: "passed"`. Read any `diagnostics` and `planIssues` too; an empty selection is not scenario coverage.

For these three requests, the evaluator checked the authorization and validation outcomes and compared the successful production's route and payload with the expected fact. It did **not** deploy a service, commit events to Chronicle, execute attached code, contact external services, or test a renderer. Source validity, reference execution, and consumer conformance are different verdicts.

## 5. See a binding error, then recover

In **`AddingABook` only**, find the `then BookAdded` block. Change its expected `title` from `"Introduction to Algorithms"` to `"A Different Title"`. Leave the `when AddBook` input and the command's `title = title` production unchanged. This intentionally asks the model to produce a fact different from the requested title.

Run the same command again:

```bash
.tools/screenplay test library.play --format json
```

Expect exit **3** and `outcome: "unbound"`. In `diagnostics`, find **`PLAY0285`**: the expected `BookAdded` cannot be produced from the stated `when` values under the declared mappings. The command copies the requested title unchanged, so the compiler rejects the contradictory expectation before reference execution.

The report has `discovered: 3`, but `selected`, `executed`, `passed`, `failed`, and `unsupported` are all **0**, and `results` is empty. This is a binding error, not a failed runtime assertion or a successful reference run. None of the three scenarios executed; zero failures here does not mean they passed.

Restore the expected title to `"Introduction to Algorithms"` and rerun the command. Expect exit **0**, with `discovered`, `selected`, `executed`, and `passed` all **3**, and `failed` and `unsupported` both **0**. Continue only when all three pass again. If they do not, compare the `when` and `then` blocks with the complete file above.

Other outcomes need different action:

- Exit **2** means invalid arguments, selection, or input, or that the command could not run. Check the path and tool version.
- Exit **3** means the model did not bind, or nothing failed but reference execution was unsupported. Inspect diagnostics and capability reasons; do not treat unsupported scenarios as passes.
- Zero selected or executed scenarios is not evidence that these business rules were checked.

The [CLI reference](tool.md#run-the-models-specifications) defines the complete report and exit contract, including mixed failed/unsupported selections.

## 6. Optionally view the same model

The file is already useful as a reviewed, reference-tested description. To see its structure, install the Screenplay VS Code extension and open this folder:

```bash
code --install-extension cratis.screenplay
code .
```

Open `library.play`; choose **Show Source** to keep the text beside the board. Find `AddBook`, `BookAdded`, and the specifications. The board shows declarations and authored expectations, not a new execution result. A released extension may bundle a different compiler from the pinned tool; keep using the tool for this walkthrough's acceptance check. See [See your event model](see-your-event-model.md) for other viewing paths and sharing limits.

## Continue with one concern

- Add a read side: [Projections](projections/index.md) builds a read model from events; [Queries](queries.md) exposes it.
- Describe interaction: [Screens](screens.md) names what people see and which commands they invoke.
- Organize a larger model: [Modules, features, and slices](slices.md), [Folders](folders.md), and [Imports](imports.md).
- Add review checks: [Completeness checks](completeness.md) finds structural gaps beyond ordinary compilation.
- Choose a consumer: [Interoperability](interoperability.md) explains the ESM version and capability boundary. A reference pass alone does not establish renderer or runtime support.
