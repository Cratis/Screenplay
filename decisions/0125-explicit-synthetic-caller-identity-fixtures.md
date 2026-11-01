---
id: 0125
title: Supply a specification's audit identity through an explicit `given identity` fixture, separate from the caller
status: proposed
stage: none
class: contract
reversibility: costly
decider: proposed by L58 for woksin's review
applies-to:
  - Source/DotNET/Screenplay/Parsing/SpecificationParser.cs
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/grammar.md
  - Documentation/screenplay/context.md
---

## Context

[#1097](https://github.com/Cratis/Screenplay/issues/1097): a specification can declare `given caller as BillingClerk`, but that supplies no audit identity (no subject, name or userName). A mapping such as `registeredBy = $context.identity.name` therefore cannot run reproducibly. Cratis/Stage#885 removed `registeredBy` and `cancelledBy` from the Billing sample because of this gap. The issue requires identity values to be explicit and synthetic, never taken from the process, a persona or the environment. Evidence is at `14478619` (`origin/main`):

- **The caller is an authorization witness only.** `given caller` admits `authenticated`, `role` and `claim` ([`SpecificationParser.cs:568-611`][spec-parser]; TypeScript [`SpecificationParser.ts:31,253-261`][spec-parser-ts]). It binds to `authenticated roles claims` ([`SemanticModelBinder.Specifications.cs:148-151`][spec-binder]; ESM shape `Caller` at [`EsmBaselineShapes.cs:103`][esm-shapes]). `given caller as <Persona>` has no body ([`SpecificationParser.cs:550-563`][spec-parser]), and [0042](0042-persona-callers-in-specifications.md) fixes that a persona supplies no actor.
- **Scenarios only supply time.** A pinned clock becomes a time-only occurrence with empty identity strings ([`SemanticScenario.cs:57,132`][scenario]; [`SemanticCommandOccurrence.cs:13`][occurrence]).
- **Execution refuses identity reads.** A mapping reading the context with no occurrence is a contract error. One reading `causedBy` or `identity` from a time-only occurrence is `Unsupported` ([`SemanticCommandExecution.cs:73-74,284-291`][execution]).
- **The context paths already exist.** `$context.identity.*`, `$identity.*` and `$context.causedBy.*` (`id`/`subject`, `name`, `userName`) are the portable subset and bind to the same bytes ([context.md](../Documentation/screenplay/context.md), line 183). Reactions record causation rather than a caller identity, and `RunsAs` never supplies one ([reactions.md](../Documentation/screenplay/reactions.md), lines 39 and 377; [0043](0043-reaction-identity-runs-as.md)).
- **The Specification ESM shape is versioned per member** (for example `givenClock@6`, `thenInvocations@34`; [`EsmBaselineShapes.cs:78`][esm-shapes]). A new member changes canonical bytes only when present. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) numbers versions at admission, and [0004](0004-admission-and-governance-of-portable-executable-semantics.md) with [0066](0066-published-esm-versions-are-frozen.md) requires explicit consumer admission.
- **Targets.** Stage renders `given caller` as a `ClaimsPrincipal` ([Stage `SemanticCommandSpecificationRenderer.cs:225-240` @ `6d9d219e`][stage-renderer]). Chronicle's client takes an explicit identity through `IIdentityProvider.SetCurrentIdentity` ([Chronicle `IIdentityProvider.cs:21` @ `c27f48316f`][chronicle-provider]). Its ASP.NET Core provider derives the live identity from the `sub`, `name` and `preferred_username` claims, uses empty strings for missing values, and falls back to `Identity.NotSet` ("[Not Set]") when all three are empty ([`IdentityProvider.cs:24-33`][chronicle-aspnet], [`Identity.cs:21`][chronicle-identity]).

## Decision (proposed)

1. **Fixture.** A specification may declare zero or one `given identity` block. Its children are `subject "<text>"`, `name "<text>"` and `userName "<text>"`, each at most once and in any order. At least one must be present; an empty block is an error. An omitted member is the empty string, matching Chronicle's handling of a missing claim. Values are literals only: no case references, `$` expressions or defaults from anywhere else.

   ```
   specification "Registering records the clerk"
     given caller as BillingClerk
     given identity
       subject "clerk-1"
       name "Ada Clerk"
       userName "ada"
     given clock "2026-01-05T09:00:00Z"
     when RegisterAccount
       ...
   ```

2. **Separate from authorization.** `given identity` is a sibling of `given caller` and `given caller as`, not part of either. It never changes the authorization outcome. A persona, caller claims (`sub` and so on), the process and the environment never supply or change it. The 0042 persona contract and PLAY0387/PLAY0389 are unchanged.
3. **Independent of the clock.** The scenario occurrence combines the clock (if pinned) and the identity (if declared). Each read is satisfied only by its own fixture. Reading the identity without `given identity` keeps today's outcomes (the contract error without an occurrence, `Unsupported` with a time-only occurrence). Reading `occurred` without a clock also keeps its existing outcome. The internal occurrence type stops tying identity to time.
4. **Propagation.** The fixture is the audit identity of the specification's `when` step: a command, or a `when append`. Events appended by that step carry it as `causedBy`, so mappings, rule bodies (`CausedBy`), projections reading `$eventContext.causedBy` and reaction contexts of those events see the same values. Reaction-invoked commands keep 0043 and the causation rule, so they do not inherit it as a caller. `given` history events carry no identity, as today.
5. **Replay.** The identity is part of the specification's ESM, so a re-run or replay produces byte-identical event contexts and never consults ambient state.
6. **Unauthenticated callers (recommended; open for the owner).** The fixture does not depend on authentication. An anonymous scenario that reads the identity either declares explicit values or gets today's `Unsupported`. The language defines no anonymous or "not set" identity of its own. See *Open question*.
7. **Executable model.** `Specification` gains an appended, optional member `givenIdentity@N` with shape `Identity = subject name userName`. It is omitted when absent, so every existing model keeps its bytes. N is allocated at admission (0025). Stage admits it explicitly, and older readers reject the member.
8. **Syntax tree and parity.** `SpecificationSyntax` gains an init-only `GivenIdentity` (members plus location), omitted from syntax JSON when absent. Both compilers parse, print and diagnose it identically: an empty block, a duplicate member or block, a non-literal value, or an unknown child. Each gets a new code allocated in the implementation slice. Printing puts members in the canonical order `subject`, `name`, `userName` and puts the block straight after the caller line.
9. **Rendering.** Each target renders the fixture through its own explicit identity seam, never through synthesized claims. The Cratis target sets Chronicle's current identity before the `when` step. Axon sets its own message metadata. A target without such a seam reports a blocking gap.

### Open question

What should an unauthenticated caller's audit identity be?

- **U1, recommended:** explicit values only, independent of authentication. This is portable and has no target constant. The cost is that a specification can pair an anonymous caller with a named identity, a state a live Cratis host never produces.
- **U2:** the language defines a "not set" identity that a bodyless `given identity not set` selects, using Chronicle's `Identity.NotSet` values. This matches the live Cratis host, but it puts a Chronicle constant into portable semantics, and Axon would have to reproduce it.
- **U3:** `given identity` with an unauthenticated caller is an error. Strict, but the unauthenticated acceptance case in #1097 can then only show `Unsupported`.

The syntax, binding and propagation slices do not depend on this answer; only the anonymous-case specs and diagnostics do.

## Options considered

- **Sibling `given identity` block (recommended).** It works with both caller forms, including the bodyless persona form (`given caller as InvoiceDraftCreator` in the Invoicing sample, and `given caller as BillingClerk` in #1097's example). It follows the existing `given <fixture>` family, and it reads the same as `$identity.*`. The [0052](0052-identity-details-as-authoring-metadata.md) top-level `identity` block describes detail *shapes*, so a later `given identity` could also carry detail values without new syntax.
- **`subject`/`name`/`userName` inside `given caller`.** Rejected. It mixes the authorization witness with audit identity (the issue's own analysis names them separate contracts) and cannot attach to `given caller as <Persona>` without reopening 0042's no-body rule.
- **Derive identity from caller claims (`sub`, `name`, `preferred_username`), as Chronicle's ASP.NET Core provider does.** Rejected. Identity would be implicit, persona synthesis could indirectly supply it (the issue forbids that), and authorization and audit specifications could no longer be changed independently.
- **Per-`when` identity (`when X as ...`).** Not needed. A specification has one `when` step, and the block form leaves room for later fixtures.
- **Do nothing and keep `Unsupported`.** Rejected. Samples keep dropping audit fields.

## Default if unanswered

Specifications cannot assert audit fields. Models drop them (as Stage#885 did) or leave them unverified, and a mapping from `$context.identity` stays unprovable in the reference.

## Timeline and scope

In scope: the specification fixture, its binding, reference execution, replay and rendering. Out of scope: caller authorization, persona synthesis, Chronicle's event context shape, `onBehalfOf`, identity details (#600), and identities on `given` history events.

Implementation slices:

1. This record. Its owner decision settles U1, U2 or U3.
2. Syntax in both parsers, the syntax tree and transport, printer, diagnostics, binder, and the ESM member at a version allocated at admission. Grammar and the specifications page are updated here.
3. Occurrence construction in `SemanticScenario` from explicit fixtures, propagation to event contexts, replay, and the no-ambient-fallback specs (`for_SemanticModelBinder/when_binding_an_audit_identity_fixture`, `for_SemanticSpecificationRunner/when_running_with_an_audit_identity`).
4. MCP fixture tools, editors, documentation, and restoring `registeredBy`/`cancelledBy` in the Billing sample.
5. Stage admits the version and renders the fixture. Stage.Axon follows.

Performance (large models): the cost is constant per specification. Parsing is linear in the block lines, binding builds one value per specification, and execution builds the occurrence once per `when` step. There is no model-wide pass or index, so compile cost on a model with thousands of specifications grows only with the declared fixtures. The repository has no benchmark project yet. Slice 3 adds a compile-and-run measurement on a generated model with thousands of specifications, with and without the fixture, and accepts no regression for models without it.

## Verification

Done when slices 2 to 4 are merged and Stage admits the version.

Verify by:

- Specifications cover a named caller with a subject, an unauthenticated caller (per the owner's answer), and a replay with byte-identical event contexts.
- A spec proves that removing the fixture restores today's `Unsupported` rather than reading any ambient identity.
- ESM goldens show unchanged bytes for models without `given identity`.
- Parser-parity fixtures pass in C# and TypeScript.

## Consequences

Audit fields become provable with complete expected events, and authorization and audit identity stay separate. Authors must state identity explicitly in every specification that reads it. Ambient or persona-derived identities in specifications are ruled out, as is a language-level anonymous identity unless the owner chooses U2.

[spec-parser]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Parsing/SpecificationParser.cs#L550-L611
[spec-parser-ts]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/Screenplay/Compiler/Parsing/SpecificationParser.ts#L253-L261
[spec-binder]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Specifications.cs#L148-L159
[esm-shapes]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/Serialization/EsmBaselineShapes.cs#L78-L103
[scenario]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/Execution/SemanticScenario.cs#L57
[occurrence]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/Execution/SemanticCommandOccurrence.cs#L13
[execution]: https://github.com/Cratis/Screenplay/blob/14478619501f0673f8d36051a8045334867d334a/Source/DotNET/Screenplay/Semantics/Execution/SemanticCommandExecution.cs#L284-L291
[stage-renderer]: https://github.com/Cratis/Stage/blob/6d9d219e/Source/Rendering.Cratis/Semantics/SemanticCommandSpecificationRenderer.cs#L225-L240
[chronicle-provider]: https://github.com/Cratis/Chronicle/blob/c27f48316f/Source/Clients/DotNET/Identities/IIdentityProvider.cs#L21
[chronicle-aspnet]: https://github.com/Cratis/Chronicle/blob/c27f48316f/Source/Clients/AspNetCore/Identities/IdentityProvider.cs#L24-L33
[chronicle-identity]: https://github.com/Cratis/Chronicle/blob/c27f48316f/Source/Clients/DotNET/Identities/Identity.cs#L21
