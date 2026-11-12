---
id: 0162
title: Availability is an authorization-reasoned gate over named audiences, evaluated by the Choreography engine
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-12
decider: Einar Ingebrigtsen (direct request for the availability epic, Cratis/Screenplay#1296)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

`policy` and `authorize` say who may do something. Nothing says whether a part of the model exists at all for a given audience: a commercial package that includes Data Quality, a capability rolled out to a few tenants first, a part switched off for an incident. Today that is rebuilt by hand per portal view, API route and MCP tool, and the copies drift. The driving case is Raa Labs IDEA-279, where the decider is a property of the thing operated on (the vessel's commercial package), one customer can differ across the fleet, and the decision must hold on screens, API and MCP alike.

This record settles the design tracked in Cratis/Screenplay#1298 under the epic Cratis/Screenplay#1296. It depends on decision 0161 (context values, Cratis/Screenplay#1297).

## Decision

Three pieces that compose: an `audience`, an `availability` (a reusable rule) and an `available` gate on modules, features and slices.

- **Names:** `audience`, `availability` (reusable block), `available for | when | with`. Not "feature flag". An audience is a declaration with a description, like a `policy`. An `availability` block holds `for` and `when` lines and can use `with` to build on another block.
- **Gate forms:** `available for <audiences>` (the caller or tenant holds the audience), `available when <condition>` (a condition over the context), `available with <Availability>` (the named block holds). They combine with `and`, `or` and parentheses as `authorize` does. Gates AND down the path: module, feature, slice, then command, query and screen.
- **Enforcement is authorization.** A gate compiles to an authorization policy that Arc's existing policy runtime evaluates, so there is no second gating mechanism. The policy carries a distinct reason (`NotAvailable`, apart from an authorization denial), so clients can tell "not available for you" from "not allowed" and hide what is not available. Availability is evaluated before `authorize`.
- **Unknown never opens a gate.** A missing audience, missing property or unresolved value is unavailable. The result is one of Available, Unavailable (with an availability reason) or Unknown, which denies. A failure is an error, never a denial (decision 0161).
- **Events and observers are unaffected.** Reactions that produce into an unavailable slice are fine, and projections keep running, so switching a part on again needs no replay.
- **Specifications always run with everything available.** No `given availability` form is added.
- **Runtime, per request.** Build-time exclusion by a renderer is a later optimization.
- **Audience membership is not in the model.** The model declares audiences and never who is in them, so nothing ships as an accidental entitlement. A tenant, a user or the system receives audiences at runtime through grants, carried in `$context.audiences`.
  - A grant for an undeclared audience is kept and carried as recorded. It never matches a gate, and the engine reports it as "granted but not declared". Re-declaring the audience makes old grants live again.
  - Effective audiences are the union of the tenant's, the user's and the system caller's. Withdrawing a grant removes it from its own level only. Grant events carry their level so an explicit deny or most-specific-wins rule can be added later without changing stored facts.
  - Membership otherwise comes from environment configuration bound by name. Choreography offers an environment-level "all declared audiences granted" switch for local and test environments, off by default.
- **Persona `audience` lines are authoring-only,** like a persona's policies. They tell the board, Studio and documentation which audiences a persona is in. They have no runtime effect and no ESM change, and specifications ignore them.
- **Cross-slice invocation is a warning.** The compiler warns when a screen action, form or reaction `invokes` a command whose audiences can be off while the invoker's are on, compared as audience sets. `when` conditions are opaque and skipped, never guessed. This follows the `PLAY0557` precedent. A reaction's `invokes` is evaluated with the tenant's audiences plus the system's, never a user's.
- **Naming:** `audience` is kept, confirmed twice. Docs and hover text state that a Screenplay audience is who a part of the application is available to, unrelated to a token's `aud` claim. Chronicle subscription routing (`ForAudience`, `ProductAudience`, `OperatorAudience`) and Marquee already use the word with the same meaning, "who it is for". The Showrunner contract keeps the senses apart: the payload field is `Audience`, and Chronicle's routing label stays in options and configuration, never in an event field.
- **Change latency and ownership.** Applications never ask Direct. Direct produces events, published on its outbox and received in the product's inbox, so a change takes effect as fast as that transfer and no faster. The application evaluates every request against its own local state built from those events, so an outage of the link leaves the last known state in force. The language promises "current as of the last event the application has received". Open sessions update reactively from local read models. A lag metric can be added later.
- **Relation to Box Office is separate.** Audiences are a new mechanism beside Box Office module entitlements, which are unchanged and keep owning tenant-level commercial modules (`ModuleGranted`, `IEntitlements`, namespace-based gating).

Who owns what:

| Piece | Owner |
| --- | --- |
| Declarations, gate grammar, ESM binding, printer, diagnostics, docs, samples | Screenplay |
| The engine (Cratis.Choreography.Availability): blocks against the context, audience resolution, reactive change; public API usable without Arc | Choreography (Arc and Stage consume it) |
| Audience contracts: public events granting and withdrawing an audience at Tenant, User and System level, with their read models, queries and APIs; newest wins; audience membership source consumed by the engine | Showrunner |
| Enforcement of the generated policy, proxy generation, React navigation | Arc |
| Rendering the gate and engine wiring into a generated application | Stage |
| Control plane for configuring audiences per tenant and user | Direct |

## Options considered

- **Reuse Box Office module entitlements.** Not taken: they are per tenant only, not conditional, not reusable rules, bound to code namespaces, and not declared by the model. Extending them would change a shipped product. The cost is two vocabularies, accepted and documented.
- **A second gating mechanism beside authorization.** Not taken: the policy runtime already enforces on commands, queries, API and MCP.
- **Declare membership or static defaults in the model.** Not taken: a model that says who is in an audience can ship an entitlement by accident.
- **Precedence rules (most specific wins, explicit deny) now.** Not taken: union only, with level on the events so precedence can be added without migrating facts.
- **Applications asking Direct per request or per session.** Not taken: it couples applications to the operator and makes an outage an outage of availability.
- **A different word for audience.** Not taken after two checks: no clash inside Screenplay, and other uses mean the same thing.

## Default if unanswered

Each product keeps hand-rolling the same decision per portal view, API route and MCP tool, and the copies drift. Raa Labs IDEA-279 stays unmet.

## Timeline and scope

In scope: the declarations, gate grammar, policy compilation, reasons, cross-slice warning, persona authoring lines, the ownership above and the surfaces in the language-change checklist (compilers, diagnostics, ESM and admission, printer and layout, reference evaluator, MCP, CLI, Monaco, VS Code grammar, `availability.md` linked from `policies.md` and `personas.md`, samples, this record). Out of scope: Studio design, a build-time renderer exclusion, the event model board, a role- or claim-to-audience mapping, an explicit deny rule, and any change to Box Office.

## Verification

Done when: a model with `audience`, `availability` and `available for | when | with` gates compiles on both compilers, binds into the ESM as a policy with the availability reason, and an application built on the Choreography engine answers Unavailable for a caller outside the audience and Unknown (denied) for an unresolved value.

Verify by: compiler and binder specifications per gate form, the reference evaluator treating everything as available, the cross-slice warning specification, and the Invoicing sample plus a multi-file sample compiling with no diagnostics.

## Consequences

One gate holds on screens, API and MCP, and clients can hide what is not available. Authorization policies gain a second reason, and the compiler gains audience-set analysis for cross-slice calls. Two vocabularies, Box Office modules and audiences, coexist, and Screenplay `module` and Box Office module are different things with the same word.

Open:

- Whether a gate needing both a Box Office module and an audience bridges through `$context`, for example `provide entitlements from host` read in an `available when` condition. Not decided.
- The name and home of the part of Showrunner that owns audience contracts.
- Studio design, which follows the backend.
- A lag metric for change latency.
- A role- or claim-to-audience mapping, and an explicit deny or most-specific-wins rule, each addable without changing stored facts.

Related: Cratis/Screenplay#1296 (epic), Cratis/Screenplay#1297 and decision 0161 (context values), Cratis/Screenplay#1298 (this design).
