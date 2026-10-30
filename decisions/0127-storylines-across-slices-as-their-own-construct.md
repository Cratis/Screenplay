---
id: 0127
title: 'Storylines: an ordered scenario across slices as its own top-level construct'
status: proposed
stage: none
class: contract
reversibility: costly
decided: —
decider: Direction by woksin on 2026-10-30; detailed contract proposed
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/Screenplay/**
  - Documentation/screenplay/specifications.md
---

## Context

A `specification` has one action and runs one slice: "A `specification` block lives inside a `slice`"
(`Documentation/screenplay/specifications.md:25`), and [0115](0115-a-specification-runs-its-own-slice-only.md) item 1
makes the runner start only that slice's reactions, captures and projections. A process that crosses slices, such as
reserve, pay, ship, therefore cannot be stated as one scenario. The authoring guidance says so: "Screenplay has no
multi-step storyline today (... Screenplay#394, open, not available)" and tells authors to emulate one with independent
specifications that repeat cumulative givens
(`.cratis/ai/skills/cratis-screenplay-scenario-coverage/references/view-and-story-specs.md:39-40`).

woksin's direction of 2026-10-30 covered direction only: "storylines across slices as their own construct". A storyline is a
separate construct, not a multi-step form of `specification`, and does not weaken 0115. These drafts
remain drafts until proven. The syntax and detailed choices below are proposals, not approved contracts.

## Decision

Proposed contract implementing woksin's direction of 2026-10-30; pending evidence and detailed acceptance.

1. **A new construct, `storyline`.** It is not a `specification` and does not change that construct's grammar,
   diagnostics or ESM bytes. A model without a storyline is byte-identical to today.
2. **Minimal syntax.** A storyline has a name, an optional `description`, optional shared `given` lines, and ordered
   `step` blocks:

   ```screenplay
   storyline OrderToDelivery
     description "An order is placed, paid and shipped"
     given caller as Shopper
     step PlacingTheOrder
       when PlaceOrder orderId = "o-1"
       then OrderPlaced orderId = "o-1"
     step PayingForIt
       when PayOrder orderId = "o-1"
       then OrderPaid orderId = "o-1"
       then readmodel OrderStatus
         orderId = "o-1"
         status = "Paid"
   ```

   Each step has exactly one action (`when <Command>`, `when append`, `when query`, `when trigger`, `when clock` or
   `when capture`, using the existing forms in `Documentation/screenplay/specifications.md:94-137`) followed by zero or more `then` checkpoints. The `then` lines are the ones a
   specification already has. Step names are unique within the storyline.
3. **Where it lives.** At feature level, as a sibling of `slice`, not inside a slice or directly under a module or
   application. It is a first-class construct outside slices, not an application-root declaration. It may reference
   declarations in other slices through the existing scope and import rules.
4. **Given.** Shared `given` lines (`event`, `readmodel`, `caller`, `clock`, `capture`, including `caller as <Persona>`
   from [0042](0042-persona-callers-in-specifications.md)) apply once, before step 1. A step may state its own `given
   caller` to change the caller for that step only. A step has no other `given`.
5. **State between steps.** Steps share one reference-runner world. The facts and read-model state a step produces stay
   for the next step. This is the opposite of a specification, which starts from its own givens each time. The step's
   event expectations compare only facts produced during that step, including its triggered cross-slice work;
   earlier events are not restated. Read-model and query checkpoints inspect cumulative state after the step.
   A failed checkpoint stops the storyline: later steps are reported as not run, not as failed.
6. **Reach of a step.** A step runs every slice's reactions, captures and projections that the action triggers, across
   slices. That is the point of the construct and is the stated difference from 0115: a slice-scoped specification proves
   one slice; a storyline proves the hand-over. A storyline does not replace per-slice specifications and the
   completeness checks that count specifications do not count it.
7. **Runner and run result.** The reference runner (`SemanticSpecificationRunner`,
   `Source/DotNET/Screenplay/Semantics/Execution/SemanticSpecificationRunner.cs:59`) gets a storyline entry point that
   returns one result per step. `screenplay test`, `--filter` and MCP `run-specifications` select storylines by address
   and report them next to specifications, labelled as storylines.
8. **Diagnostics.** Codes are allocated at implementation, not here. The record fixes the cases: malformed header or
   step, duplicate storyline or step name, a storyline with no step, a step with no or more than one action, an unknown
   command, event or read model, and a `then` that contradicts every possible producer of the step's action (the
   reasoning of `PLAY0285`, `diagnostics.md:1145`, restricted to the whole model rather than one slice). Unresolvable
   persona callers reuse the existing persona refusal. Do not name a code in documentation until it is allocated.
9. **ESM.** #394 requires an executable-model contract and consumer admission before execution. Admission must define
   ordered steps, shared world state and checkpoints, with canonical vectors and reference-runner evidence. Allocate
   its version at admission under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md)
   item 2 against current `main`; no version is claimed here. Stage must execute only an explicitly admitted contract
   and return a typed refusal for unsupported storyline semantics, never silently skip steps or run them in isolation.
10. **Editor and MCP surfaces.** Both compilers (C# and TypeScript) parse, print and round-trip the construct. Monaco
    gets syntax, hover and completion for step names and the existing action and `then` forms. MCP lists storylines as
    a declaration kind, returns them in `declaration-details`, indexes their references for find-references and rename,
    and runs them through `run-specifications`. The event model board shows them as an ordered strip of steps. Studio's
    importer must keep the new member, as 0042 required for `GivenCallerPersona`.

## Options considered

- **Storyline as a mode of `specification` (several `when` steps in one block).** Rejected by woksin's direction of 2026-10-30 and by 0115: it
  would give one construct two scopes.
- **Storyline inside a slice.** Rejected: a cross-slice scenario has no single owning slice.
- **Keep emulating with families of specifications.** The status quo; it repeats givens and proves no ordering.
- **Execution without ESM admission.** Rejected: #394 requires versioned execution and Stage typed refusal.
  Syntax authoring may precede admission; execution may not.

## Default if unanswered

Authors keep emulating storylines with cumulative-given specification families, and cross-slice ordering stays
unproved.

## Timeline and scope

Proposed only; not in force. In scope: syntax, both parsers and printers, ESM admission before execution, reference
runner, typed consumer refusals, diagnostics, MCP, Monaco and documentation. Version allocation waits for admission.
Out of scope: branching or looping steps, parameterised or case-table storylines, and storylines across applications.

## Verification

**Done when:** a storyline whose second step depends on the first step's facts passes; changing the first step's
outcome fails the checkpoint and reports later steps as not run; state checkpoints inspect cumulative state;
unsupported Stage contracts return typed refusals; a model without storylines keeps its ESM bytes.

**Verify by:** parser, printer and conformance vectors in both compilers; reference-runner specifications for state
carry-over and stop-on-failure; MCP and Monaco specifications; and the Acme Commerce sample with one storyline.

## Consequences

Cross-slice behavior gets one executable statement. Per-slice specifications keep their meaning from 0115. Every
consumer that reads syntax trees (Studio, Stage, the board) must handle the new member or refuse it explicitly.

## Open points

Detailed contract acceptance awaits proof of the proposed grammar, canonical encoding, reference execution and
Stage admission/refusal behavior. woksin's direction of 2026-10-30 approves the direction, not these implementation details.

## Related

Builds on 0115, 0042 and 0025. Cratis/Screenplay#394.
