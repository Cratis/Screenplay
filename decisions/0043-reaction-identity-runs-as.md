---
id: 0043
title: Declare the system identity under which a reaction's invoked commands run
status: accepted
stage: none
decided: 2026-10-08
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/ReactionParser.cs
  - Source/DotNET/Screenplay/Parsing/ReactionRefusalValidator.cs
  - Source/DotNET/Screenplay/Semantics/SemanticReactions.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Reactions.cs
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticReactionLoop.cs
  - Source/DotNET/Screenplay.Mcp/McpDeclarationDetails.cs
  - Source/Screenplay/Compiler/Parsing/ReactionParser.ts
  - Source/Screenplay/Compiler/Parsing/ReactionRefusalValidator.ts
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/reactions.md
  - Documentation/screenplay/diagnostics.md
---

## Context

A reaction's `invokes` runs the command's full pipeline with no caller, so any gated command rejects. In Arc a reactor carrying `[ExecuteCommandsAsSystem(params string[] roles)]` runs its returned commands as `SystemPrincipal.WithRoles(roles)`: authenticated, holding exactly those roles, with no roles allowed. **Without** the attribute there is no principal and `[Authorize]` and `[Roles]` deny. The reference's no-caller default therefore matches an unmarked reactor; the model simply cannot state the attribute. (The claim in [0030](0030-reaction-refusals-and-redelivery.md), `reactions.md` and the PLAY0557 message that Arc runs reactor commands as the system is corrected there and below.)

[#383](https://github.com/Cratis/Screenplay/issues/383) asks for the declaration. 0030 makes it a precondition of admitting refusal handling. PLAY0557 (#489) warns on `on refused by authorization` over gated commands using a stub that always reports "no declared identity". [0004](0004-admission-and-governance-of-portable-executable-semantics.md) and [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) govern admission; [0022](0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md) defines reaction execution; [0028](0028-declared-module-and-feature-dependencies.md) defines dependency boundaries.

## Decision

### Syntax

```screenplay
reaction CloseExpiredClaims
  runs as system role "ClaimsAutomation" and role "Auditor"
  when ClaimDeadlinePassed
    claim
    invokes CloseClaim
      claim = claim
```

`runs as system` is optionally followed by `role "<Role>"` repeated with `and`: zero or more roles, mirroring Arc's `SystemPrincipal`. It is one reaction-level line, at most once. The established printer order is description, documentation, triggers, then `where`; `runs as` prints after documentation and before the first trigger, and the parser accepts it anywhere in the body before the first trigger, like the other reaction-level lines. Roles are non-empty quoted literals; duplicates are errors. `runs as <Persona>` is rejected for v1: the attribute carries no claims, personas would start affecting portable behavior and reopen [#254](https://github.com/Cratis/Screenplay/issues/254) (see [0042](0042-persona-callers-in-specifications.md)), and a persona edit would silently widen automation.

The identity applies to every command the reaction returns or `invokes`, under every trigger. It does **not** cover imperative `ICommandPipeline` calls inside implementation bodies (inline or `file` code), nor `produces`, `reads`, refusal-branch productions or causation. Omitting the line keeps today's no-caller behavior and bytes. `given caller` still supplies no actor to invocations.

Clock and application triggers: `runs as` is allowed in syntax on any trigger. Stage renders neither yet, so there is no Arc realization; this is stated as a readiness note in MCP `declaration-details` and the documentation, not a diagnostic, so authors get no warning for valid models.

### Semantics and audit

The reference evaluator runs each invocation of an identity-bearing reaction with a system caller: authenticated, exactly the declared roles, no portable claims. Every claim condition evaluated against it is unknown (final unknown denies; `not unknown` stays unknown; a satisfied role alternative allows). This is a deliberate, recorded, deny-only divergence from Arc, whose system principal carries `[System]` subject claims; the portable contract does not pin them. The identity governs authorization only: Chronicle still records `Identity.System` on behalf of the triggering cause, and `$context.causedBy` in invocations stays unsupported.

### ESM

`SemanticReaction` gains an optional member **`runsAs`** (not `identity`, so it is not read as audit identity): `{"kind":"system","roles":["ClaimsAutomation"]}`. It is omitted when absent, `roles` is sorted ordinally and may be empty (the strict reader accepts `[]`), `kind` is a closed vocabulary admitting only `system`, and earlier strict readers reject the member.

### Admission

Before admission, syntax, printing, validation and diagnostics may land; binding refuses `runs as` with PLAY0268 naming the feature and #383, never a version number, and MCP readiness lists it. Identity is admitted at the first ESM version claim whose own 0004 gate-2 evidence is ready, **never later than 0030's admission**. No unreleased ESM claim exists today (`interoperability.md` claims no version; `Versions.cs` supports released v1-v7), and the planned 0036 routes batch is not a claim holder or an unconditional predecessor. Under 0025 only one unreleased claim may exist at a time, so if another claim has merged first and is unreleased, identity joins that claim or waits for its release. It does not amend 0036. Stage must explicitly refuse the member at admission (a `SemanticVersionFeatures` entry); rendering `[ExecuteCommandsAsSystem]` is a follow-up, not an admission prerequisite, because Stage#79 blocks every v6 reaction anyway. Admission evidence: Arc runtime meaning, golden vector, reference allowed/denied vectors, source-backed corpus vector, prior-reader rejection, activation only when used.

### Diagnostics

- **PLAY0557's stub becomes real:** a declared `runs as` suppresses it; its message names `runs as system role` as the remedy and states that Arc runs commands as the system only for a reactor carrying `[ExecuteCommandsAsSystem]`. `Documentation/screenplay/reactions.md` is corrected the same way, with #383's implementation.
- **New warning, both compilers: gated invocation without identity** when an `invokes` targets a gated command and the reaction declares no `runs as`; same gate-exists predicate as PLAY0557 (an absent caller satisfies no gate, opaque ones included), at most one finding per invocation (PLAY0557 wins when the invocation has an `on refused by authorization` branch).
- **New warning, C# only (like the other effective-gate analyses, which need the bound semantic model): least privilege.** A declared role referenced by no invoked command's effective gate; silent when gates are opaque and silent when the reaction has an inline or `file` implementation body.
- **New warning, C# only: declared identity cannot satisfy** a definite-deny gate; unknown results stay silent.
- **New warning, both compilers: unused identity** on a reaction with no `invokes` and no implementation body.
- **New error, both compilers:** malformed, repeated or misplaced `runs as`, empty or duplicate roles.
- **Opt-in `privilege` completeness finding** (confused deputy: a low-privilege caller's event can trigger an elevated reaction; Arc limits the override to request-less work, so HTTP-origin commands are not elevated and the reaction path is the channel). For each event-triggered reaction with `runs as`, it reports a **Warning** for every producer of the trigger event whose effective gate does not require each declared `runs as` role.
  - Ungated commands, captures and events produced by other reactions count as unprivileged producers and are reported.
  - Opaque gates (named rules or implementation bodies the evaluator cannot see) are reported at **Information** as "cannot be compared", never silently.
  - Clock and application triggers are silent: there is no producer.
- **Cross-boundary roles.** A role that appears only in effective gates of commands outside the reaction's own module (the 0028 module boundary) is allowed, produces no diagnostic, and is listed in MCP `declaration-details`.

### MCP and corpus

`declaration-details` for `Reaction` adds `runsAs` (kind, roles, location, readiness note); `syntax-schema` shows `ReactionSyntax.RunsAs`; `propose-ast` can set or remove it (with no special review flag; see Options considered); after admission the executable-model view includes `runsAs`. The Cratis/AI trap "never invent `runs as`" stays until a release ships the syntax, then changes (`ai-corpus: tracked`). #286's acknowledge item is superseded by 0030.

## Options considered

- **Per-reaction role list (chosen).** One-to-one with Arc's attribute.
- **Per-`invokes` identity:** not realizable without Arc changes.
- **Single role:** forces an invented umbrella role; Arc takes `params`.
- **`runs as <Persona>` or a full caller:** rejected for v1 (above); additive later.
- **Reuse `SemanticCaller` in the ESM:** makes system indistinguishable from a user.
- **Mirror Arc's `[System]` claims in the reference:** pins runtime constants; unknown is safer.
- **Join the 0036 batch or extend released v7:** rejected; needs amending an accepted record or changes a version Stage already admitted.
- **Warning on clock and application triggers:** rejected; it would flag valid models.
- **A review flag on `propose-ast` edits to `runs as`:** rejected. Any source edit can change authorization, and `read-proposal view=semantic-diff` shows the `runsAs` change.

## Default if unanswered

Invocations keep running with no caller; PLAY0557 stays unresolvable and 0030's admission stays blocked.

## Timeline and scope

In force from acceptance through admission. Out of scope: persona identities, per-invocation identity, claims on system identities, audit alignment with Chronicle, unreachable authorization branches, imperative pipeline calls, and #286 fan-out.

## Verification

**Done when:** both compilers parse, print and round-trip `runs as` with matching syntax JSON; the diagnostics fire and stay quiet as specified; binding refuses with PLAY0268 by feature name; models without the line keep their bytes. At admission: a role-gated invocation passes with the identity and is denied without it, earlier strict readers reject `runsAs`, goldens and corpus vectors exist, Stage refuses explicitly.

**Verify by:** parser, printer and validator specifications; binder and readiness specifications; reference runner and golden comparisons at the claim; the .NET and TypeScript gates; Stage and Cratis/AI tracking review.

## Consequences

Models state the trusted path instead of prose. Existing models invoking gated commands gain a warning (visible to `--warnaserror` users). 0030's admission becomes possible. `runsAs` is a published ESM kind that Stage, CritterStack and Studio must admit or refuse.
