---
id: 0003
title: Decision consistency for command reads
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/CommandSyntax.cs
  - Source/DotNET/Screenplay/Parsing/ReadsParser.cs
  - Source/DotNET/Screenplay/Parsing/CommandConsistencyValidator.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Commands.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Conditions.cs
  - Source/DotNET/Screenplay/Semantics/Versions.cs
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
---

## Context

[#129](https://github.com/Cratis/Screenplay/issues/129) promises that a decision commits only while every state input it decided from is still current. It names three open choices: what a `reads` dependency maps to at runtime, how existing `reads` and `concurrency` migrate, and how to alias two reads of the same view. `require` over declared reads in [#209](https://github.com/Cratis/Screenplay/issues/209) waits on the first and third. Today it fails binding with "read-model paths require decision-consistent reads (#129)" ([`SemanticModelBinder.Conditions.cs:31`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Conditions.cs)).

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines the runtime meaning, and Screenplay may not invent a check Chronicle lacks. Chronicle's `ConcurrencyScope` is shaped by event streams, not read models. It narrows by sequence number, event source, stream type, stream id, source type and event types ([`ConcurrencyScope.cs:17-23`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/ConcurrencyScope.cs)). A scope that expects no matching event represents absence (`ConcurrencyScope.cs:44`). A read-model lookup returns the sequence number it last handled ([`GetInstanceByKeyResponse.cs:28`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Contracts/ReadModels/GetInstanceByKeyResponse.cs)).

In Screenplay, both `reads` and `concurrency` already fail binding with `PreservedLegacySemanticSyntax` ([`SemanticModelBinder.Commands.cs:24-37`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Commands.cs)), so no bound model depends on their meaning yet. `concurrency` is documented as a mirror of Chronicle's `ConcurrencyScope` and is in use ([`commands.md:472`](../Documentation/screenplay/commands.md)). `ReadsSyntax` has no alias member ([`CommandSyntax.cs:170`](../Source/DotNET/Screenplay/Syntax/CommandSyntax.cs)).

## Decision

A declared `reads` becomes a protected decision dependency, admitted in a first version only where Chronicle can check it exactly.

1. **Runtime mapping.** The first version admits only reads of views keyed on the event source (`SemanticProjectionKey.EventSourceIdentity`, [`SemanticModelBinder.ProjectionValues.cs:22`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.ProjectionValues.cs)) with no joins. Each read maps to one Chronicle concurrency scope:
   - The label is the read key value.
   - The event types are the events the view projects from.
   - The sequence number is the watermark read with the view.
   - An absent view maps to "expects no matching event".

   Views keyed any other way, joins and child scopes fail binding with a diagnostic that names the reason.
2. **Precondition.** Before the runtime mapping is implemented, confirm that Chronicle/Arc can read that watermark at command time and accept a scope on an event source the command does not append to. This confirmation is in progress. It gates the runtime mapping only, not the grammar.
3. **Migration.** Protected reads apply only under a new language version. Existing `concurrency` is never silently promoted. A command that declares both protected `reads` and `concurrency` gets a blocking ambiguity diagnostic.
4. **Alias grammar.** The grammar is `reads <View> [as <alias>] [by <prop>]`. The alias is required when a command reads the same view twice.

## Options considered

- **Derive a scope for any view from the events it projects from.** Not taken: for a view not keyed on the event source, the scope cannot narrow to the instance that was read. It would either reject irrelevant changes or miss relevant ones.
- **Define a read-model revision check.** Not taken: Chronicle has no such check, and decision 0001 rules out inventing one in Screenplay.
- **Event-source-keyed views only (taken).** The mapping is exact, and a lagging projection makes the check fail instead of letting a stale decision through.
- **Explicit opt-in per read.** Not taken: a document would then mix two meanings of one keyword.
- **Silently promote `concurrency` or legacy `reads`.** Not taken: it changes append behavior in models already using `concurrency`.
- **Positional aliases or no aliases.** Not taken: two reads of one view (a source account and a destination account) would be ambiguous.

## Default if unanswered

`reads` and `concurrency` stay unbound legacy syntax, `require` over reads stays rejected, and commands that decide from a read model have no portable consistency guarantee. A stale read can authorize a decision in any rendered target.

## Timeline and scope

Settle this before any #129 or #209 `require`-over-reads work, and keep it until superseded. The alias grammar can ship first because it is additive. The runtime mapping and migration ship only after the precondition is confirmed.

In scope: the alias grammar in the parser, syntax tree, TextMate and Monaco grammars; admission and rejection diagnostics; the language-version gate; the both-declared ambiguity; the concurrency-scope mapping; and `require` over reads for admitted views.

Out of scope: views keyed otherwise, joins, child scopes, a read-model revision check, changes to Chronicle's runtime, and the #129 non-goals (stream positions, locks, DCB tags, framework concurrency attributes).

## Verification

**Done when:** `reads <View> as <alias> by <prop>` parses into `ReadsSyntax` with an alias, and a second read of the same view without an alias is a diagnostic. Under the new language version, a read of an event-source-keyed view without joins binds to a dependency that carries its key, event types and watermark. Other views fail binding with the reason stated. A command that declares both protected `reads` and `concurrency` gets a blocking diagnostic. Documents under the current language version keep today's `PreservedLegacySemanticSyntax` behavior. The runtime mapping part is done only once the precondition in item 2 is confirmed.

**Verify by:** Parser and binder specs for each of those cases, plus reference-execution vectors that check three things: a changed declared dependency rejects the decision with no facts, an irrelevant change does not, and absence races fail the check. Check grammar changes against Chronicle's pinned `Cratis.Screenplay` version, as decision 0001 requires.

## Consequences

Decision consistency gets a first version that Chronicle can check exactly, and #209's `require` over reads can follow. Commands that read views keyed any other way stay unprotected, and those reads fail binding rather than being silently weakened. The language-version gate keeps existing documents unchanged, at the cost of a version bump and a migration path for authors. The mapping depends on a Chronicle/Arc capability that is not yet confirmed, so part of this record may need a follow-up record if the confirmation fails.

## Related issues

Screenplay: [#129](https://github.com/Cratis/Screenplay/issues/129), [#209](https://github.com/Cratis/Screenplay/issues/209), [#214](https://github.com/Cratis/Screenplay/issues/214).
