---
title: "Cratis Screenplay FAQ: modeling and execution"
description: "Cratis Screenplay describes application behavior in .play files. Learn where code fits, how execution is checked and how Stage and Studio use the model."
---

Cratis Screenplay describes application contracts and behavior in `.play` files, so you can review what callers send, which facts follow and what state they can read. These answers help you choose what to model, where implementation code belongs and what to verify before relying on an execution tool.

## Why indentation instead of braces?

Structure follows the offside rule: a construct owns everything indented beneath it, the way Python and YAML work. A `.play` file describes a nested model — modules contain features contain slices contain constructs — and indentation shows that nesting directly, without the visual noise of matching braces. The trade-off is that indentation is significant, so the language service flags tab indentation as a diagnostic; use spaces.

## What does the `.play` extension mean?

A Screenplay is a script for a production, so its file is a `.play`. The name is deliberate: a runtime or renderer performs the script without becoming the authority for what it means. Editors recognize `.play` files and give them the Screenplay icon and language support.

## Do I still write C#, TypeScript, or React?

Yes, where the behavior needs an implementation rather than a declarative rule. A command handler, query performer, rule predicate, reducer rule, reaction, constraint, or screen can carry an inline block or `file` reference. Keep the contract and its specifications in Screenplay; attach the code needed to realize it. Built-in inline tags are `csharp`, `java`, `typescript`, `react`, `html`, and `sql`. A recognized tag does not mean every target can execute it: Java is for targets that render Java, such as Axon, and the Cratis target refuses Java bodies. See [Sub-languages and inline code](sub-languages.md) for the attachment boundary.

## What are Stage and Studio?

Cratis Studio is an optional commercial workspace for collaboratively designing and editing Screenplay event models. Cratis Stage verifies modeled specifications, renders application code and can perform supported behavior in a disposable sandbox, letting you try the application you described. Screenplay remains the source of the model's meaning; sharing a `.play` file does not prove that every consumer supports the same behavior.

## How does Screenplay relate to Arc and Chronicle?

Screenplay describes commands, facts, views, queries, policies and specifications without depending on a runtime framework. A renderer maps that model to an application; the Cratis target uses [Arc](/arc/) and [Chronicle](/chronicle/). You can also use Arc and Chronicle directly without adopting Screenplay.

## What are PDL and CDL?

They are the two built-in first-class sub-languages. **PDL**, the Projection Declaration Language, defines projection bodies. **CDL**, the Change Data Capture Language, defines capture transformations. Both have standalone compiler entry points as well as their host-language constructs. See [Projections](projections/index.md) and [Captures](captures.md).

## Can I add my own sub-language?

Not as a new Screenplay construct today. Host-language construct keywords are closed so the compiler cannot silently discard unknown behavior. You can register additional inline language tags, which the compiler carries as opaque text for the owning tool to interpret. Monaco can also register editor-only highlighting and completions, but that does not make the construct valid Screenplay syntax. See [Sub-languages and inline code](sub-languages.md).

## Where do highlighting and IntelliSense come from?

The [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) package provides the Monaco language service: highlighting, context-aware completions, hover documentation and diagnostics, including embedded code and the PDL/CDL sub-languages. The standalone editor and VS Code extension host that service; their surrounding editor features need not be identical.

## Is Screenplay production-ready?

Check the capabilities your application needs rather than treating accepted syntax as an execution guarantee. Compilation checks the source; semantic binding checks whether its behavior fits a supported executable semantic model (ESM). [Specifications](specifications.md) then run scenarios and compare outcomes. Unsupported behavior cannot count as a passing scenario. Verify your chosen runtime or renderer's support separately, including any code attachments it must execute.
