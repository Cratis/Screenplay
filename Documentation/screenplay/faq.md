---
title: "FAQ"
description: "Decide whether to adopt Screenplay, understand reference-test results, and choose a next step without confusing a model with a deployed app."
---

## Do I need Screenplay to use the Cratis Platform?

No. [Arc](/arc/) and [Chronicle](/chronicle/) are independently usable. Screenplay is an optional model-first path: write the business behavior, check it, then choose a consumer that can preserve it. You do not need Stage, Studio, Scene, or Prologue to keep building a handwritten application with Arc or Chronicle.

## When is a model worth keeping?

When commands, facts, rules, read models, and specifications need to stay understandable together. A `.play` model gives the team an artifact to review before those decisions are spread across implementation layers. It is less useful when nearly all behavior would be opaque code attachments, or when the task is a small script. See [Why Screenplay](why-screenplay.md) for the trade-offs.

## Is Screenplay another programming language?

It is a business-oriented specification language for event-sourced applications. It describes intent declaratively; it is not a general-purpose language. Inline code and `file` references can attach implementation at selected points, but the model must remain meaningful without them. See [Commands](commands.md), [Queries](queries.md), and [Sub-languages](sub-languages.md).

## Does a `.play` file replace every file in my application?

No. It can describe the whole system's intended behavior, but source may be split across many `.play` files. Implementation attachments, build configuration, secrets, generated artifacts, and application tests are separate concerns. Compiling a folder treats its discovered files as one application; compiling a root file follows its imports. See [Folders](folders.md).

## What can I verify before I have an application?

You can compile source, request [structural completeness checks](completeness.md), and run supported specifications with the deterministic in-memory reference evaluator. The [first-model walkthrough](getting-started.md) checks a successful book addition, a malformed ISBN, and a caller without the required role. It also changes one expected title deliberately to show a binding error before any scenarios execute, then restores the title to recover the three passing reference results.

## Does a passing specification mean my app works?

It means that selected, supported reference scenarios matched their expectations. `screenplay test` does not run attached code, contact external services, commit to Chronicle, or deploy an application. Unsupported behavior is reported as `unsupported`, not as a pass. Check positive selected/executed counts as well as failures, unsupported results, and exit status. See [Compiler and CLI](tool.md#run-the-models-specifications).

## Does a clean compile mean a renderer can build my model?

No. Parsing and source validation, executable binding, reference execution, and consumer conformance are separate verdicts. The executable semantic model (ESM) is versioned; a renderer or runtime must admit its version and capabilities. It must reject what it cannot preserve, not silently omit it. See [Interoperability](interoperability.md).

## Do I need Stage or Studio for the first example?

Neither. The walkthrough needs the released Screenplay .NET tool only. Stage is a consumer intended to run models; Studio provides visualization, editing, and generation surfaces. Their support must be checked for the model you want to use. A [board](see-your-event-model.md) helps review structure, but drawing cards is not execution or complete validation.

## What if I already have code?

Keep it. You can [generate a model from recognized declarations](see-your-event-model.md#get-a-model-from-existing-code), inspect diagnostics, and use the result for discussion. Generation does not reconstruct arbitrary handler logic or prove runtime behavior. Adopting the generated source as an authored model is a separate decision; it is not required to inspect it.

## Can I inspect or share the model without running the app?

Yes. VS Code, MCP Apps, the CLI explorer, and Studio offer different [viewing paths](see-your-event-model.md). Public share links expose model structure; never include secrets or private customer data. The anonymous viewer currently needs a self-contained single-file model and does not follow file imports.

## Why indentation instead of braces?

Structure follows the offside rule: a construct owns everything indented beneath it, as in Python and YAML. Modules contain features, features contain slices, and slices contain constructs; indentation shows that nesting without matching braces. Indentation is significant. Use spaces: the language service diagnoses tab indentation.

## What does the `.play` extension mean?

A screenplay is a script for a production, so its file is a `.play`. A runtime or renderer performs that script without becoming the authority for its meaning. Editors recognize the extension for Screenplay language support.

## Do I still write C#, TypeScript, or React?

At implementation points that admit code, yes. A command handler, query performer, rule predicate, reducer rule, reaction, constraint, or screen can carry an inline block or `file` reference where declarative intent needs a realization. Built-in inline tags are `csharp`, `java`, `typescript`, `react`, `html`, and `sql`. Java is for targets that render Java, such as Axon; the Cratis target refuses Java bodies. Attached code remains distinct from the business model and is not executed by the reference runner.

## What are PDL and CDL?

They are the two built-in first-class sub-languages. **PDL**, the Projection Declaration Language, defines projection bodies. **CDL**, the Change Data Capture Language, defines capture transformations. Both have standalone compiler entry points as well as host-language constructs. See [Projections](projections/index.md) and [Captures](captures.md).

## Can I add my own sub-language?

Not as a new Screenplay construct today. Host-language construct keywords are closed so the compiler cannot silently discard unknown behavior. You can register additional inline language tags, carried as opaque text for the owning tool to interpret. Monaco can register editor-only highlighting and completions; that does not make a construct valid Screenplay syntax. See [Sub-languages and inline code](sub-languages.md).

## Where do highlighting and IntelliSense come from?

The [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) Monaco language service provides highlighting, embedded-language support, completions, hover documentation, and diagnostics. The standalone editor and VS Code extension host this shared service.

## Is Screenplay production-ready?

Screenplay is young and evolving. The compiler and authoring tools are usable; portable execution and rendering are delivered capability by capability. Treat the reference as the description of accepted syntax, and require a runtime or renderer to declare support before relying on it to perform that syntax. Arc and Chronicle are the first realization target, not dependencies of the language or the authority for its portable meaning.

## Where are the grammar, value types, and inline-code rules?

Use the [complete EBNF grammar](grammar.md), [Concepts](concepts.md) for value types and their inherited attributes, and [Sub-languages and inline code](sub-languages.md) for embedded bodies. Start with the [language overview](overview.md) when you need a map rather than a single rule.
