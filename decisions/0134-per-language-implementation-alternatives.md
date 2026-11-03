---
id: 0134
title: A member carries at most one implementation body per language, written as sibling payloads
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-01
decider: woksin (direction accepted on #1086; syntax decided under delegated orchestration)
amends: 0002, 0013, 0117
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/grammar.md
  - Documentation/screenplay/commands.md
  - Documentation/screenplay/queries.md
---

## Context

[#1086](https://github.com/Cratis/Screenplay/issues/1086) asks for one model to target both the Cratis (C#) and Axon (Java) render targets with real logic. Today a member carries exactly one implementation body, so a command handler, query performer, reaction or reducer is written in `csharp` *or* `java`, never both. Evidence is at `14478619` (`origin/main`):

- `handler` admits exactly one child: a `file`, one fence, or one `implementation` wrapper ([`CommandParser.cs:409-440`][cmd-handler]; grammar [`HandlerDecl`](../Documentation/screenplay/grammar.md), lines 1187-1196). A second child is either a mixing error ([`CommandParser.cs:438`][cmd-handler]) or falls through to the command body as `PLAY0034`; a second fence under a query performer surfaces as `PLAY0048` ([`QueryParser.cs:116-128`][query]).
- The wrapper takes no operand and admits "at most one file or inline payload" ([`ImplementationParser.cs:30-87`][impl-parser]); the TypeScript compiler mirrors both rules ([`ImplementationParser.ts:95-121`][impl-parser-ts], [`SyntaxInvariants.ts:88`][invariants-ts]).
- Every fence already names a registered language ([`CodeBlockParser.cs:23-28`][code-block]). A `file` names one only for the CommandHandler role, by extension ([`DescribedCommandAdmission.cs:12-27,79`][admission]; decision [0117](0117-admit-constrained-attached-command-handlers.md) item 2).
- A RequirementId hashes owner, role and member, not language or content ([`ImplementationRequirementIdentity.cs:12-13`][identity]). The binder gives a *repeated* identical member a `#N` ordinal ([`SemanticModelBinder.Implementations.cs:140-145`][binder]) and flags two CommandHandler entries with one RequirementId as ambiguous ([`:23-27`][binder]); 0117 item 3 makes ambiguous attachments fail closed. So `#N` produces a second *requirement*, not an alternative body for the same requirement, which is what the issue reports.
- The ESM carries only the RequirementId and result version for a handler ([`EsmBaselineShapes.cs:50`][esm-shapes]). Language, file, content hash and body span live in the sidecar ([`SemanticImplementationRequirement.cs:45-74`][requirement]); 0117 item 6 already relies on this to attach bodies without changing canonical bytes.

On 2026-10-29 woksin accepted the per-language alternatives model as the direction for rendering to other targets, sequenced after inline C# bodies work end to end in the running Stage (`cratis run` and Studio's Play). Downstream selection is Cratis/Stage#869 (C#) and Cratis/Stage.Axon#52 (Java). The issue leaves the syntax and three questions open: duplicate languages, a member without a body for a given target, and the relation to the implementation lifecycle ([#307](https://github.com/Cratis/Screenplay/issues/307)).

## Decision

A member's implementation is a **set of language alternatives**: one or more payloads, at most one per language, behind one RequirementId.

1. **Syntax: sibling payloads, no new keyword.** Wherever a member admits exactly one payload today (directly under the member, or inside its `implementation` wrapper), it admits one or more payloads. Each payload is a tagged fence or a `file` directive, and fences and files can be mixed. A payload's language is its fence tag or its file extension, using the one extension table from 0117 item 2, which now applies to every implementation role. `implementation` keeps no operand. Hints stay on the wrapper and belong to the member, not to a language.

   ````
   handler
     implementation
       hint "Reject registrations for closed accounts"
       ```csharp
       ...
       ```
       file ./Register.java
   ````

2. **Language rules.** Two payloads with the same language on one member are an **error**, because a target could not choose between them deterministically. A `file` whose extension has no language is an error when it is one of two or more payloads. A single payload keeps every current rule and diagnostic. Mixing direct and wrapped payloads stays an error (`ConflictingImplementationSources`). The duplicate and language-less errors are new codes, allocated in both compilers in the same slice.
3. **One requirement, several realizations.** Alternatives share the member's RequirementId and do not use the `#N` ordinal. Identity encoding is unchanged. In the sidecar the requirement gains an ordered, init-only `Alternatives` list. Each entry holds a language, file, content hash, body span and body lines, ordered by language (ordinal). For a single payload, `Alternatives` is empty and the existing top-level `Language`, `File` and `ContentHash` keep their current values and bytes. For two or more, the top-level fields are empty. A consumer that predates the member therefore sees a language-less attachment and fails closed (0117 item 3) instead of running the wrong language. Only payloads with the same language count as ambiguous.
4. **Executable model unchanged.** The ESM gains no member, enum value or version. Canonical bytes and revision stay identical, as in 0117 item 6. Decision [0004](0004-admission-and-governance-of-portable-executable-semantics.md) needs no version because no canonical bytes change.
5. **Syntax tree.** Each syntax node that carries a payload (`HandlerSyntax`, `ImplementationSyntax`, performer, reaction, reducer and rule payload owners) gains an ordered, init-only `Alternatives` collection in authored order, omitted from syntax JSON when empty. `File` and `Code` are set only for a single payload, following the init-only growth rule in [AST compatibility](../Documentation/screenplay/ast-compatibility.md). A syntax-transport reader that predates the member rejects it instead of reading a bodyless member.
6. **Round trip and canonical form.** The printer keeps authored order, so text round-trips. Semantic equivalence ([0013](0013-equivalence-for-screenplay-code-round-trips.md) item 2) compares a requirement's alternatives as a set of `(language, content hash)` keyed by RequirementId, so order does not matter. A realization that recovers only some languages reports the others as a classified loss, not a mismatch.
7. **Selection belongs to the target.** Each render target and each opt-in reference-run provider ([0121](0121-reaction-effect-and-snapshot-performer-provider-contracts.md)) uses the alternative in its own language. It neither warns about nor refuses the others. Screenplay does not diagnose a member that lacks a body for some target, because it does not know the target set. The target reports a blocking gap, as the provider-side capability rule in [0002](0002-implementation-attachments-envelope-and-reducer-role.md) item 4 already requires. MCP declaration details list each member's languages so tools can report coverage on request.
8. **Lifecycle (#307).** Intent and hints belong to the member and are language-neutral ([0082](0082-admit-a-described-command-handler-as-a-pending-implementation-requirement.md)). Each alternative is one realization with its own content hash, and drift is measured per realization. Zero payloads is still the pending state of 0082. One or more payloads is attached, for the languages present.
9. **Sequencing.** Slices 2 onwards start after the C# inline path works end to end in Stage, as the owner set on #1086.

## Options considered

- **Sibling payloads, language from tag or extension (chosen).** This combines the issue's options 1 and 3. It reuses the existing source of truth for language (the fence tag that `CodeBlockParser` already requires, and 0117's extension table), adds no keyword and keeps the wrapper contract (hints, no operand). Its costs are a parser rule change in both compilers and a sidecar member.
- **Repeated `implementation <language>` wrappers (issue option 2).** Rejected. The operand restates the fence tag, so two sources of truth can disagree (`implementation java` around a `csharp` fence) and need their own diagnostic. Hints would be repeated or split across wrappers, which breaks 0082's member-level intent. The grammar's no-operand rule ([`ImplementationParser.cs:38`][impl-parser]) and every printer would also change.
- **Per-language files only (issue option 3 alone).** Rejected as the only form. Inline bodies are the path the owner prioritised for C#, so forbidding inline alternatives would make authors move to files just to add a second language.
- **Separate requirements per language (reuse `#N`).** Rejected. RequirementIds would differ by language, which breaks 0013's identity-based equivalence, and targets would see a second, unrelated requirement.
- **Duplicate language as a warning, last one wins.** Rejected. Authored order would decide behaviour silently, which conflicts with 0117 item 3 (ambiguous attachments fail closed).
- **Screenplay diagnoses a missing target language.** Rejected. Screenplay has no target set, and the provider-side capability rule (0002) already gives the target this responsibility.

## Default if unanswered

A model stays single-language, so one model cannot carry working logic for both Stage and Axon. Authors keep two diverging models or give up one target, and Stage#869 and Stage.Axon#52 have nothing to select from.

## Timeline and scope

In scope: every implementation payload owner (command handlers, query performers, reactions, reducers, operation phases and named rules), in both compilers, the printer, the binder sidecar, MCP and documentation. Out of scope: rendering selection in Stage and Stage.Axon (their own issues), the language list itself, capability profiles, and AI realization or drift tooling (#307).

Implementation slices:

1. This record.
2. Both parsers (C# and TypeScript), syntax nodes and transport schema, `SyntaxInvariants`, printer, the duplicate and language-less diagnostics, and parser-parity fixtures. Grammar and the command and query pages are updated in this slice, when the syntax becomes real.
3. Binder and sidecar `Alternatives`, extension inference for all roles, the same-language-only ambiguity rule, the 0013 equivalence comparator, and per-language selection in the reference-run provider seam.
4. MCP (declaration details, attachment manifest, readiness text), editor hover and completion, and documentation.
5. Downstream: Stage#869 selects `csharp` and Stage.Axon#52 selects `java`, each without diagnostics for the other languages.

Performance (large models): parsing stays linear in payload lines. Binding creates one requirement per member whatever the number of languages, so requirement count does not grow. Alternatives must not go through the per-requirement ordinal scan at [`SemanticModelBinder.Implementations.cs:142`][binder]: that scan is already linear per allocation (quadratic over a model), and slice 3 must not add entries to it. A keyed counter is the expected fix if slice 3 touches it. Hashing stays proportional to body bytes, as today. Slice 3 adds a compile benchmark on a generated model with thousands of members at one and at two languages. It must show linear growth with total body size, and no change for the single-language model.

## Verification

Done when slices 2 to 4 are merged and Stage#869 and Stage.Axon#52 consume them.

Verify by:

- Parser-parity fixtures show one `csharp` fence and one `java` file under one handler parse identically in C# and TypeScript, and that two `csharp` payloads produce the duplicate-language error in both.
- A binder spec shows one RequirementId with two alternatives, no `#N` ordinal and no ambiguity flag.
- An ESM golden test shows canonical bytes and revision are byte-identical whether the member has one alternative or two.
- Round-trip print preserves authored order.
- The performance benchmark meets the criterion above.

## Consequences

One model can carry real logic for every target, and targets select by language with no new keyword. Sidecar consumers must learn `Alternatives`; older ones fail closed. Authors must keep alternatives behaviourally equivalent by hand, because Screenplay cannot check that across languages: specifications prove the behaviour per target. This rules out per-language hints and per-language identities.

[cmd-handler]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Parsing/CommandParser.cs#L396-L440
[query]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Parsing/QueryParser.cs#L116-L128
[impl-parser]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Parsing/ImplementationParser.cs#L13-L87
[impl-parser-ts]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/Screenplay/Compiler/Parsing/ImplementationParser.ts#L95-L121
[invariants-ts]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/Screenplay/Compiler/Syntax/SyntaxInvariants.ts#L88
[code-block]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Parsing/CodeBlockParser.cs#L23-L28
[admission]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/DescribedCommandAdmission.cs#L12-L79
[identity]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/ImplementationRequirementIdentity.cs#L12-L13
[binder]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Implementations.cs#L23-L145
[esm-shapes]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/Serialization/EsmBaselineShapes.cs#L50
[requirement]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/SemanticImplementationRequirement.cs#L45-L74
