---
id: 0006
title: Reaction triggers declare the views they decide from with reads
status: accepted
stage: implemented
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/ReactionSyntax.cs
  - Source/DotNET/Screenplay/Parsing/ReactionParser.cs
  - Source/DotNET/Screenplay/Parsing/TriggerParser.cs
  - Source/DotNET/Screenplay/Parsing/ReadsParser.cs
  - Source/DotNET/Screenplay/Parsing/ScreenplayValidator.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs
  - Documentation/screenplay/reactions.md
  - Documentation/screenplay/grammar.md
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
---

## Context

An automation decides from state it consults: in Event Modeling, a process watches a to-do view and acts on each item. [#69](https://github.com/Cratis/Screenplay/issues/69) asks for that relationship. A reaction can already start from an event, a declared trigger or the clock, and can `produces` or `invokes`, but it cannot name the views behind its decision. `ReactionTriggerSyntax` has no reads member ([`ReactionSyntax.cs:44-52`](../Source/DotNET/Screenplay/Syntax/ReactionSyntax.cs)), so the inputs of an automation are invisible in the document.

Commands already have `reads <View> [as <alias>] [by <prop>]`. [Decision 0003](0003-decision-consistency-for-command-reads.md) makes a command's reads a protected decision dependency and rejects giving one keyword two meanings ([0003, options](0003-decision-consistency-for-command-reads.md#options-considered)). Reactions do not bind in the executable semantic model (ESM) today: every reaction fails with "requires portable occurrence and effect semantics" ([`SemanticModelBinder.SliceMembers.cs:93`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs)).

Today a line `reads X` under a trigger parses as a trigger value named `reads` of type `X`: the trigger body falls through to `TriggerParser.ParseData`, which accepts any property line ([`ReactionParser.cs:175-178`](../Source/DotNET/Screenplay/Parsing/ReactionParser.cs), [`TriggerParser.cs:87-89`](../Source/DotNET/Screenplay/Parsing/TriggerParser.cs)).

## Decision

A reaction trigger may declare `reads <View> [as <alias>] [by <trigger value>]`, with the single meaning `reads` has on commands: the state the behavior decides from.

1. **Syntax.** `reads` is a trigger body line, parsed by the existing `ReadsParser` into the existing `ReadsSyntax`. The alias rules are the command rules: a second read of the same view needs an alias, and aliases are unique per trigger.
2. **`by`.** On a reaction, `by` names a value the trigger takes. Clock triggers (`every`, `at`) take no values, so they can only read a whole view.
3. **Protection.** A reaction that `invokes` a command decides nothing itself; the command decides, and the command's own reads carry decision 0003's protection. A reaction that `produces` directly is held to decision 0003's rule once reactions bind: its reads are protected only where Chronicle can check them exactly, and otherwise binding fails with a diagnostic that names the reason.
4. **Reserved form.** `for each <View>` is reserved for a later view-driven trigger. It is not admitted by this record.
5. **Compatibility.** A trigger value named `reads` must now be written `@reads`, the escape the parser already applies to other directive names ([`ReactionParser.cs:172-174`](../Source/DotNET/Screenplay/Parsing/ReactionParser.cs)).

## Options considered

- **`reads` on the trigger with one meaning (taken).** It states the automation's inputs with the word commands already use for the same thing, and it does not foreclose a view-driven trigger, which would still want to name what it reads ([#69 sweep comment](https://github.com/Cratis/Screenplay/issues/69)).
- **Report-only `reads` on reactions.** Not taken: `reads` on commands becomes protected under decision 0003, so a report-only reaction `reads` would give one keyword two meanings.
- **A different word such as `uses`.** Not taken: `uses` already attaches UI behaviors to screens ([`ScreenParser.cs:73-76`](../Source/DotNET/Screenplay/Parsing/ScreenParser.cs)), and a second word for the same relationship is what decision 0003 avoids.
- **The view as the trigger (`when <View>`).** Not taken: `when` resolves against events, declared triggers and registered triggers ([`triggers.md`, "How a name resolves"](../Documentation/screenplay/triggers.md#how-a-name-resolves)), so views would be a fourth set with event/view name collisions. It also misstates the runtime: Chronicle reports a read-model change as `Added`, `Modified` or `Removed` ([`ReadModelChangeType.cs:9-25`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/ReadModels/ReadModelChangeType.cs)), not as "the view has rows".
- **Wait for the view-driven trigger.** Not taken: the automation's inputs stay unwritable in the meantime, and nothing in the view-driven design depends on leaving them out.

## Default if unanswered

Reactions keep hiding their inputs. `reads X` under a trigger keeps parsing as a trigger value, so authors who write it get a silent misreading rather than a diagnostic. Studio and Stage cannot draw or realize the view-to-automation edge.

## Timeline and scope

Settle before any reaction grammar work under #69, and keep it until superseded. The grammar is additive apart from the `@reads` escape and can ship first. Protection for `produces` ships only when reactions bind in the ESM, under decision 0003's rule and decision [0004](0004-admission-and-governance-of-portable-executable-semantics.md)'s admission gates.

In scope: the trigger `reads` grammar in the parser, syntax tree, printer, walker, TextMate and Monaco grammars; the alias and unknown-view checks; the `by`-against-trigger-values check; the `@reads` compatibility note in `reactions.md`; the protection rule stated above.

Out of scope: a view-driven trigger (`for each <View>`); business due time (`due at`) and terminal-fact or cancellation semantics, which are deferred because neither Chronicle nor Arc has a scheduling primitive to mirror, Chronicle's read-model reactors are best-effort with no retries or ordering guarantee ([`reacting-to-changes.mdx:56-65`](https://github.com/Cratis/Chronicle/blob/main/Documentation/read-models/reacting-to-changes.mdx)), and the work waits for Stage to decide how view-driven automation is realized and for the logical clock in [#87](https://github.com/Cratis/Screenplay/issues/87); binding reactions in the ESM; `where` over read paths; Stage's rendering of trigger reads.

## Verification

**Done when:** `reads <View> as <alias> by <value>` under an event or named trigger parses into `ReactionTriggerSyntax` with a `ReadsSyntax` entry, and round-trips through the printer. Under a clock trigger, `reads <View>` parses and `reads <View> by <x>` is a diagnostic. A `by` that names no trigger value, an unknown view, a missing alias on a repeated view and a duplicate alias are diagnostics. `@reads X` still parses as a trigger value named `reads`. `reactions.md` and `grammar.md` describe the clause and the escape.

**Verify by:** Parser, validator and printer specs for each case above, and an editor-grammar check that `reads` is highlighted under a trigger. When reactions bind, binder specs show a `produces` reaction's read is either protected under decision 0003 or rejected with the reason.

## Consequences

An automation's inputs become visible to readers, Studio and renderers, using the same word and shape as commands. Protection for reactions comes for free through invoked commands and is never weaker than decision 0003 for direct `produces`. Documents that used a trigger value named `reads` need the `@reads` escape. Due time, terminal facts and a view-driven trigger remain open, and `for each` is kept free for them.

## Status notes

**2026-09-25 — implemented.** Reaction trigger reads now parse, validate, print, walk and appear in editor grammars and guidance. Reactions remain unbound in the ESM; protection for direct `produces` waits for binding. Release version: TBD. It is not yet `verified`.

## Related issues

Screenplay: [#69](https://github.com/Cratis/Screenplay/issues/69), [#87](https://github.com/Cratis/Screenplay/issues/87), [#129](https://github.com/Cratis/Screenplay/issues/129). Decisions: [0003](0003-decision-consistency-for-command-reads.md), [0004](0004-admission-and-governance-of-portable-executable-semantics.md).
