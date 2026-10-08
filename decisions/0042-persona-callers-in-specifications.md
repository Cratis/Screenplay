---
id: 0042
title: Expand persona callers in specifications and check persona coverage on request
status: accepted
stage: none
decided: 2026-10-08
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/SpecificationParser*.cs
  - Source/DotNET/Screenplay/Syntax/Specifications/**
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Specifications.cs
  - Source/DotNET/Screenplay/Completeness/**
  - Source/DotNET/Screenplay.Mcp/McpDeclarationDetails.cs
  - Source/Screenplay/Compiler/Parsing/SpecificationParser.ts
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/personas.md
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/completeness.md
---

## Context

A persona lists the policies a role satisfies (`personas.md`). A specification states its caller in a separate, hand-written `given caller` block. Nothing connects the two, so they drift, and denial coverage per persona cannot be checked. `given caller as Accountant` fails today with PLAY0386, and a persona whose policies gate nothing compiles silently. [#382](https://github.com/Cratis/Screenplay/issues/382) asks for a persona caller and a coverage check.

Personas are report-only authoring metadata: a valid persona does not block binding, and an unresolved persona policy is an error ([#254](https://github.com/Cratis/Screenplay/issues/254), v4.39.0). [0032](0032-expand-typed-specification-examples-in-the-front-end.md) established front-end expansion that yields the same bytes as the hand-written spelling. [0027](0027-policy-negation-joins-esm-v7.md) added `not` to policies. [0004](0004-admission-and-governance-of-portable-executable-semantics.md) and [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) govern any ESM change.

## Decision

### `given caller as <Persona>`

`given caller as <Persona>` is an alternative to the explicit `given caller` block, not an addition to it. It has no body, so the zero-or-one-caller rule (PLAY0387) is unchanged, and it satisfies the explicit-caller requirement for authorized scenarios (PLAY0389). The name resolves among top-level personas. It supplies no actor to reaction invocations ([0030](0030-reaction-refusals-and-redelivery.md)).

The persona is expanded into an ordinary caller before binding, using the origin mechanism of 0032. The ESM is unchanged: canonical bytes and revision are identical to a hand-written `given caller` listing the synthesized values in canonical order. No ESM member, version or persona identity is introduced.

### Synthesis (a published contract)

The caller satisfies all of the persona's policies.

1. **Always authenticated.** A persona caller is authenticated. A role-only caller that is not authenticated would be allowed by the reference evaluator but refused by Stage (STAGE-ESM-011) and denied by Arc. An anonymous visitor stays an explicit empty `given caller`.
2. **Refused personas.** Synthesis is refused for a persona with zero policies (refusal reason `noPolicies`), for any policy containing `not` (including `not authenticated`), and for inline or `file` implementation policies.
3. **Required atoms.** Collect every atom reachable from each policy's root through `and` and grouping only: `authenticated`, `role "<R>"`, literal `claim "<T>" matches "<V>"`.
4. **`or` resolution.** Visit the policies in declared order, depth first, left to right. An `or` already satisfied by the collected atoms adds nothing; otherwise take its leftmost alternative that has a witness made only of the three atom kinds. The order of `or` operands in a policy used by a persona is therefore significant for specification outcomes, and the documentation states it.
5. **Non-literal claims** (subject, path, `$` expressions) block synthesis only when the caller would need one, meaning it is required or every alternative needs one.
6. **No role-claim-type claims** are synthesized; Stage refuses them.
7. **Result.** Roles are de-duplicated and sorted ordinally; claims are de-duplicated by type (ordinal, ignoring case) and value (ordinal) and sorted the same way. The binder verifies the result against each persona policy with the reference evaluator; anything but allow is a binding error.

A specification through a persona proves "this deterministic minimal witness of the persona produces outcome O", not that every caller fitting the persona does.

### Visibility and checks

- The chosen alternative is visible: Monaco hover, and MCP `declaration-details` for kind `Persona` with a `caller` view carrying the caller, the `contributions` (kind, value, policy) and a `refusal` (`policy`, `reason`: `negation`, `nonLiteralClaim`, `opaqueImplementation`, `unresolvedPolicy` or `noPolicies`, location).
- The effective-syntax API returns the expanded caller with a new `Persona` value origin naming persona and policy; `find-fixtures` reports it.
- `find-references` and rename index `given caller as` as a persona reference.
- The opt-in completeness check `personas` (not run by ordinary compilation; #448 can acknowledge findings) reports: a persona that gates nothing (warning); a gated command or query whose effective gate is a definite deny for every persona's synthesized caller, evaluated three-valued so unsynthesizable personas count as unknown (warning); and an **ambiguous persona** whose depended-on `or` is not decided by required atoms and has more than one buildable alternative, naming the unchosen alternative and suggesting "add a policy that pins it" (information severity). The check is silent when no personas are declared.

### Diagnostics and syntax tree

- A new error in both compilers for malformed `given caller as`, body lines, or an unknown persona (listing declared personas).
- A new error in the C# binder when a referenced persona cannot be synthesized, naming persona, policy, the blocking construct or `noPolicies`, and the remedy (an explicit `given caller`). This is decided at compile time, never as runtime `SemanticUnsupported`; it **replaces** #382's acceptance criterion "`SemanticUnsupported` naming the policy".
- `SpecificationSyntax` gains an init-only `GivenCallerPersona` (name, location), not an empty `SpecificationCallerSyntax`: a consumer predating the member would otherwise read an explicit unauthenticated caller and pass denial scenarios silently.

### #254

The remaining scope of [#254](https://github.com/Cratis/Screenplay/issues/254) is closed: personas and providers stay out of the ESM. Reopen only if a persona form of `runs as` is chosen (see [0043](0043-reaction-identity-runs-as.md), which rejects it for v1), or if [#335](https://github.com/Cratis/Screenplay/issues/335) puts screen availability into the ESM.

## Options considered

- **Front-end expansion with a binding error (chosen).** Decidable at compile time; ESM bytes untouched.
- **Union of all `or` alternatives.** Rejected: over-grants (`InvoiceManager` would also hold `Accountant`), defeating `then denied` coverage.
- **One specification per minimal alternative.** Stronger proof but breaks byte equivalence with a hand-written caller and multiplies runner work. Deferred.
- **Error on any unresolved `or`.** Rejected: the documented `InvoiceManager` persona would stop compiling; the information finding gets most of the benefit.
- **Pick the alternative whose role matches the persona name.** Rejected: relies on naming.
- **Authenticated only when a policy says so; zero-policy persona as signed-in user or as anonymous.** Rejected: three-way divergence with Stage and Arc, or guessing intent.
- **A persona caller in the ESM with runtime `SemanticUnsupported`.** Rejected: needs a version and admission for something statically known and pulls #254 in.
- **Persona plus refinement body.** Not in v1; path-dependent callers use the explicit block.
- **Always-on coverage warnings.** Rejected as noisy; this is the "nothing uses X" family.

## Default if unanswered

`given caller as` keeps failing with PLAY0386, personas stay report-only and persona coverage is unchecked.

## Timeline and scope

In force from acceptance until superseded. In scope: the form, synthesis, the new errors, the `personas` check, provenance, MCP views, references, rename, editors, documentation and samples. Out of scope: any ESM change or persona admission, refinement bodies, persona-scoped screen specifications and screen availability ([#335](https://github.com/Cratis/Screenplay/issues/335) decides those), and reaction identities.

## Verification

**Done when:** both compilers parse, print and round-trip the form; an unknown persona fails in both; expanded and hand-written callers produce identical ESM bytes and revisions; the reference runner passes allowed and `then denied` scenarios through persona callers (role, `authenticated`, literal claims); negation, needed non-literal claims, implementations and zero-policy personas fail binding by name; the `personas` check reports the three findings and stays silent without personas; effective syntax, `find-fixtures` and `declaration-details` show the caller with provenance; rename updates the form; `personas.md` states the operand-order rule.

**Verify by:** parser, printer and conformance vectors; binder byte-equivalence specifications; reference-runner vectors; completeness specifications; MCP and rename specifications; Monaco specifications; the .NET and TypeScript gates with unchanged goldens.

## Consequences

Specifications name who acts, and persona edits flow to every scenario using them. Authors with negation, ownership claims or code policies keep explicit callers. The synthesis rules are published contract: changing them later changes revisions of every persona-backed specification.

Consumers to adapt: Studio's `SpecificationSyntaxVisitor` needs the new `GivenCallerPersona` member (otherwise importing silently drops the caller); Stage#120 and [#335](https://github.com/Cratis/Screenplay/issues/335) reuse the synthesis rule rather than defining their own; the AI corpus must say "persona means authenticated".
