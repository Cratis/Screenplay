---
id: 0169
title: Availability part two - `not`, suspension, screen elements and the engine in Arc
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-24
decider: Einar Ingebrigtsen (direct request in a design session, Cratis/Screenplay#1377)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
  - Samples/**
amends: 0158, 0162
---

## Context

Decision 0162 delivered audiences, `availability` blocks and `available for | when | with` gates (v6.26.0). Using it end to end showed four things it does not settle:

- A part can be closed only for someone who lacks a grant. Closing a part for an audience that is granted (an incident group, a kill switch) cannot be said. Closing a part nobody planned for needs a gate written beforehand on every part.
- Decision 0158 derives the availability of screen elements from authorization only. A button bound to a command in a part that is not available is still offered.
- An explicit gate on a screen element is not allowed, and Stage refuses an `available` gate on a screen (Cratis/Stage#1128).
- The engine lives in Choreography while the declaration and the enforcement live in Arc, so declaring and deciding availability are two libraries with two registrations.

## Decision

- **`not` is an audience term.** `AudienceTerm = Ident | "not", Ident | "(" AudienceRequirement ")"`. `available for Plus and not Incident` and `available for not Incident` are valid. `not` binds tighter than `and`, and applies to one audience name. It is three-valued: `not` of an unknown audience is unknown, so the part stays closed. A part is closed for a *granted* audience by granting it; withdrawing the grant reopens the part. The union rule of 0162 is unchanged, and no new operator verb is needed.
- **Suspension.** An operator can *suspend* and *resume* any part (module, feature, slice, command, query or screen) at tenant, user or system level, with nothing declared in the model. A suspension is a negative fact that wins over grants at every level (the explicit deny that 0162 left for later). Newest decision wins per level. It is checked before the part's gates and before `authorize`. Its reason is `Suspended`, distinct from `NotAvailable`: a suspension is temporary, while `NotAvailable` means the part is not in the caller's offer. Suspending a module or feature suspends everything beneath it. It is a runtime concern: no grammar, and the manifest lists the parts an operator can choose from.
- **Verbs.** Audiences are *granted* and *withdrawn*. Parts are *suspended* and *resumed*. The two pairs never share a verb. Events are `PartSuspended` and `PartResumed`, not `TenantSuspended`, which belongs to Box Office.
- **Screen elements, derived.** Amends 0158. An element bound to a command or query is available when the caller satisfies its authorization **and** its availability path holds (suspension, then gates down module, feature, slice and construct). An element whose part is not available or is suspended is not rendered. An element the caller may not use keeps the rendering rule 0158 gave unauthorized elements. The report names which of the reasons applies.
- **Screen elements, explicit.** `available for | when | with` is allowed on screen elements, one form per line, as everywhere else. An explicit gate can only narrow what the bound part allows; a gate broader than the bound part is a diagnostic. On an element with no bound command or query a gate is presentation only, because nothing can refuse a request, and the documentation says so.
- **Availability depends on the data a screen shows.** An element's availability is asked with the values of the data the screen shows (for example one vessel through `provide vessel ... by arguments.vesselId`), not only with the tenant and the user.
- **Condition style in other languages.** A `when` is always Screenplay's condition grammar: comparisons joined by `and` and `or` over `$context`. A host language may also offer a named condition written in code, which is an opaque implementation attachment as in 0167, listed by name in the manifest.
- **The engine moves to Arc.** Amends the ownership table of 0162. The availability engine (evaluator, expressions, audience resolution, context values, manifest) lives in Arc, usable without ASP.NET. Choreography keeps state machines, jobs, workflows and workers, and forwards its availability namespace for one release. Showrunner (contracts) and Stage (rendering) are unchanged.
- **Unchanged:** unknown never opens a gate; a failure is an error, never a denial; events, projections and reactions are not touched; specifications run with everything available; audience membership and suspension are never in the model; Box Office stays separate.

## Options considered

- **A second verb for withdrawing availability ("withdraw availability").** Not taken: withdrawing an audience removes a grant at its own level, and under the union rule it can never close a part by itself. Giving one word two meanings would make the history unreadable.
- **`unless` or `except` instead of `not`.** Not taken: they add a second connective and a new line kind, where `not` is one word in the existing production.
- **A separate "suspended" audience granted to everyone.** Not taken: under union a withdrawal at one level cannot close a part held at another level, so closing needs a deny.
- **Close and reopen, block and unblock, deny and lift as the operator's words.** Not taken: *close* collides with "a closed gate", *deny* with authorization denial, *block* reads as security; *suspend* and *resume* are plain English for a temporary hold.
- **Leaving the engine in Choreography.** Not taken: declaring and deciding stay two libraries, and every language port would copy both.

## Default if unanswered

An incident group cannot be written. A part nobody gated cannot be switched off. Buttons bound to unavailable commands are still shown. Each host keeps two libraries and two registrations.

## Timeline and scope

In scope: the `not` grammar and every surface in the language-change checklist (Cratis/Screenplay#1378), availability on screen elements (Cratis/Screenplay#1379), and the decision text above. Out of scope here: the Arc, Choreography, Showrunner, Direct, Stage and Arc.Elixir work, which follows in their repositories (Cratis/Arc#3979 to #3982, Cratis/Choreography#243, Cratis/Showrunner#191, Cratis/Direct#3332, Cratis/Stage#1209, Cratis/Arc.Elixir#36).

## Verification

Done when both compilers accept `not`, the executable model admits it with a golden vector, the reference evaluator treats everything as available, screen element availability reports the availability path of the bound part, and Stage hides an element bound to an unavailable command.

## Consequences

An incident or preview group can be modelled with grants alone. An operator can suspend any part live. Screens offer only what the caller may use and what is there. One library declares and decides. The language gains a third admitted audience shape and one more reason, and the operator vocabulary has two pairs of verbs that must stay apart in the documentation.

Open:

- The ESM version numbers for `not` and for element gates, allocated at admission.
- The renderer rule for an unauthorized element, per renderer.
- A coded reason apart from availability in Arc for Elixir, once it has the hook.

Related: Cratis/Screenplay#1296 (availability epic), decisions 0158, 0161, 0162 and 0167.
