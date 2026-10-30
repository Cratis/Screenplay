---
id: 0128
title: 'Caller-aware screens: report element availability per persona, never enforce or render it'
status: proposed
stage: none
class: contract
reversibility: reversible
decided: —
decider: Direction by woksin on 2026-10-30; detailed contract proposed
applies-to:
  - Source/DotNET/Screenplay/Completeness/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/screens.md
  - Documentation/screenplay/personas.md
---

## Context

Screens show actions, forms and data that depend on commands and queries, and those carry `authorize` gates
(`Documentation/screenplay/policies.md:34`). A screen today says nothing about who can use what. Two records and two
docs point at this issue as a later feature:

- [0029](0029-guarded-screen-action-renderer-contract.md) (clarification note, line 24): when the selected command is
  unavailable to the caller, the action is presented as unavailable, "as #335 defines once admitted".
- `screens.md:104` says the same, and that "the selected command still enforces its authorization".
- [0042](0042-persona-callers-in-specifications.md) (line 98) says #335 reuses the persona synthesis rule instead of
  defining its own. Personas stay outside the ESM, and screens are "not admitted to the executable semantic model"
  (0029, line 20).

woksin's direction of 2026-10-30 covered direction only: "report-only first". Availability is derived and reported; it is not
enforced and not rendered until a later admission. These drafts remain drafts until proven. The report
shape, derivation details and surface choices below are proposals, not woksin-approved contracts.

## Decision

Proposed contract implementing woksin's direction of 2026-10-30; pending evidence and detailed acceptance.

1. **Report only.** The compiler and tools may compute and report which screen elements a persona can use. Nothing
   changes in binding, ESM bytes, renderers or runtime behavior. Authorization is still enforced by the command or
   query, as `screens.md:104` says.
2. **Derivation.** For each persona, in order:
   1. Take the persona's policies (`personas.md`, "Rules").
   2. Build its deterministic caller with the published synthesis of 0042 (authenticated, roles, literal claims). Do not
      define a second synthesis.
   3. For each command and query a screen element names, take the effective gate (module, feature and own `authorize`,
      combined with `and`, `policies.md:34-56`) and evaluate it for that caller with the reference evaluator, as the
      `personas` completeness check already does for denied artifacts (0042, "Visibility and checks").
   4. Report each explicit command or query dependency separately, naming its artifact and the referencing element:
      an action, guarded-action alternative or form names its command; a `data ... via query` binding names its query.
      Do not propagate a query result to descendants or combine it with a nested action's command result. Container
      aggregation and combined element availability are deferred entirely.
3. **Result per dependency and persona:** `available`, `unavailable`, or `unknown`, always attached to the explicit
   command or query identity. An allowed command inside a denied-query section therefore has two separate dependency
   results, not an element verdict. Guarded actions report each alternative's command separately. Reporting never
   selects another alternative: authorization denial does not cause fall-through (0029).
4. **Unknown, not guessed.** The result is `unknown`, with a reason, when the gate cannot be proved for a synthesized
   caller: the persona has no policies or a policy containing `not`, or an inline or `file` implementation policy (the
   refusals of 0042); a non-literal claim the caller would need; a command or query that cannot be resolved; or a gate
   that depends on the command's input or the query's arguments (`context.Artifact`, `policies.md:199`), which a screen
   does not supply until runtime. Retain every 0042 synthesis refusal, including `roleClaim`: forbidden role-claim-type
   claims are never synthesized. A synthesis failure produces `unknown` for that persona's dependencies; an anonymous
   or universally allowed gate must not turn it into `available`. Only after successful synthesis may an anonymous
   gate report `available` (`policies.md:65`). Results concern one minimal witness, not every caller fitting a persona.
   Reports are witnesses, never authorization decisions or permission to execute.
5. **Report shape.** One report per screen, with separate rows keyed by persona, referencing element address and
   explicit command/query dependency identity; guarded alternatives also include their source-order index:

   | Field | Meaning |
   | --- | --- |
   | `persona` | Declared persona name |
   | `element` | Referencing action, form or explicit data-binding address |
   | `artifact` | Qualified command/query identity and kind; alternative index for a guarded action |
   | `availability` | This dependency's witness result: `available`, `unavailable` or `unknown` |
   | `reason` | The deciding policy, or for `unknown` the refusal reason |

   Output is ordered by persona declaration, then source order, so it is stable.
6. **Surfaces.** Recommend opt-in reporting through the existing CLI check/report surfaces and the `personas`
   completeness family of 0042, not a new CLI command. Propose an MCP `declaration-details` screen `availability` view
   (as `Persona` has a `caller` view); Studio and the board may display the report. All surfaces are read-only and
   must label witness-based findings. None hides, greys out or blocks an element on the strength of this report.
7. **Diagnostics.** Information-level only, as in [0122](0122-information-diagnostics-for-redundant-spellings.md); codes
   allocated at implementation. Cases: an explicit dependency unavailable to every successfully synthesized persona;
   a dependency unknown for every persona; or persona synthesis refusal. No finding asserts combined element or
   container availability. Ordinary compilation is not made noisier: they
   are recommended for the opt-in `personas` completeness family (0042), not the default build.
8. **ESM.** No change and no version. Nothing here needs [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md)
   allocation.

## What a later admission needs

A separate record for each of these, not covered here: a renderer contract for presenting an unavailable element
(shown disabled, hidden, or with a reason), which would replace the "once admitted" wording in 0029 and `screens.md`;
a decision whether persona or availability enters the ESM and under which version at admission; how a runtime caller,
rather than a synthesized persona, is evaluated; handling of input-dependent gates; and the downstream proofs from
Stage and Studio. Until then 0029's click-time rule stands: a renderer refreshes rather than executing a different
command.

## Options considered

- **Enforce or render now.** Rejected by woksin's direction of 2026-10-30: it needs a renderer contract and runtime evaluation.
- **A second persona-to-caller rule for screens.** Rejected: 0042 says to reuse its synthesis.
- **Treat unprovable gates as unavailable.** Rejected: that would claim a denial that was never shown.

## Default if unanswered

Screens say nothing about caller availability, and renderers decide for themselves how to show an unavailable command.

## Timeline and scope

Proposed only; not in force. In scope: the derivation, the report and its MCP,
CLI and board surfaces, Information diagnostics and documentation. Out of scope: container aggregation, combined
 element availability, rendering, enforcement and ESM change.

## Verification

**Done when:** explicit dependencies show the expected witness results; an allowed command nested in a denied-query
section remains two separate rows with no combined verdict; `roleClaim` and other synthesis refusals remain `unknown`
even for anonymous gates; ESM bytes and revision with and without the report are identical.

**Verify by:** completeness and MCP specifications over personas with `or`, `not`, anonymous and implementation
policies; a board specification; and the unchanged canonical vectors.

## Consequences

Authors see witness results for each explicit command/query dependency, not whether a combined element can be used.
The report never authorizes execution; command and query authorization remains authoritative.

## Open points

Detailed acceptance awaits evidence for the proposed derivation and report shape, opt-in check/report integration,
and parity across MCP and Studio. Diagnostics codes remain unallocated; no renderer or runtime admission is granted.

## Related

Builds on 0029, 0042, 0122 and 0025. Cratis/Screenplay#335.
