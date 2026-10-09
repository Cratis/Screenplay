---
id: 0030
title: Reaction refusal handling and redelivery specifications
status: accepted
stage: none
decided: 2026-10-07
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
---

## Context

An invoked command can refuse a reaction's work through validation, an append-time constraint or authorization. Today that refusal ends the scenario. Recovery can deliver the same observed fact again, but a specification cannot identify that delivery without appending another fact. [#433](https://github.com/Cratis/Screenplay/issues/433) needs both an explicit refusal decision and a specification of recovery redelivery.

[0022](0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md) preserves each command's atomicity and already accepted cascade facts. [0023](0023-command-production-model.md) excludes operations and inline event declarations from reactions. [0004](0004-admission-and-governance-of-portable-executable-semantics.md) requires portable semantics and fail-closed admission. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) separates semantic-design acceptance from numbering an executable model release.

## Decision

> **2026-10-07 — refusal and admission clarification.** The `then no events` correction is confirmed: it waits for the same admission as refusal handling and redelivery. This record replaces item 1's `on violation <Constraint> acknowledge` spelling in [#286](https://github.com/Cratis/Screenplay/issues/286). `acknowledge` records that the refusal was handled; it does not prove the work already completed. Matching a constraint name or message alone is insufficient proof of completion.

Until [#383](https://github.com/Cratis/Screenplay/issues/383) gives invoked commands an identity, reactions run without a caller: for authorization-gated commands, `by authorization` branches always fire in the reference runner, while Arc runs reactor commands as the system. [#489](https://github.com/Cratis/Screenplay/issues/489) tracks a warning for branches without an identity; admission requires resolving this mismatch. Redelivery locators must be able to match a stated stream/route once 0031 is admitted, tracked by [#490](https://github.com/Cratis/Screenplay/issues/490). [Arc#3058](https://github.com/Cratis/Arc/issues/3058) (classifying reactor command refusals) and [Stage#214](https://github.com/Cratis/Stage/issues/214) (rendering refusal branches) are prerequisites of admission, not optional consumer follow-ups.

### Refusal branches

An `invokes <Command>` may carry ordered branches:

```screenplay
on refused [by validation | by constraint [<Name>] | by authorization]
  acknowledge
```

Instead of `acknowledge`, a branch may contain one or more ordinary `produces <Event>` blocks with mappings and optional `for`. Empty branches, repeated acknowledgements and combining acknowledgement with production are errors. Operations, inline event declarations and implementation attachments are not branch effects.

The first matching branch wins. Bare `on refused` matches validation and constraint refusals only; **authorization requires `by authorization`**. Validation includes property and concept rules and `require`. A constraint selector optionally identifies the violated constraint by name. Authorization matches an actual unauthorized result, not an opaque policy's Unsupported outcome.

Contract errors, Unsupported outcomes, infrastructure failures, exceptions and concurrency conflicts are never refusals. Concurrency is transient: realization fails the partition and recovery retries. No `by concurrency` form is introduced.

Branch mappings may use these String values:

| Value | Meaning | Scope |
| --- | --- | --- |
| `$refusal.reason` | `validation`, `constraint` or `authorization` | Any branch |
| `$refusal.constraint` | Violated constraint name | Only a `by constraint` branch |
| `$refusal.message` | Rejection details verbatim, including an unresolved `$strings.` key | Any branch |

Messages are authored text, not a stable identity. Other mapping sources retain the trigger's rules, including occurrence context. Event triggers default branch productions to the triggering event source; clock and application triggers require `for`. Read aliases are not branch inputs. The decision comes from the invoked command's protected pipeline, not an unprotected reaction read (0006).

The refused command contributes no facts. Branch productions use ordinary append constraints, projection and reaction causation; a rejected branch append ends the scenario and cannot itself be handled by another branch. An acknowledged or successfully produced refusal stops the remaining invocations of that trigger. Other reactions and cascades from accepted facts continue. An unhandled refusal ends the scenario exactly as today. Prior accepted facts remain: no cascade-wide transaction or external once-only guarantee is implied.

Duplicate selectors and narrower selectors after a covering branch are unreachable warnings. Bare refusal covers validation and constraints, but never shadows authorization. Named constraints must resolve; an otherwise valid constraint branch targeting none of the invoked command's events is unreachable. Refusal values outside their allowed mapping scope, unknown members and incompatible target types are errors.

### Redelivery specifications

```screenplay
when redelivered <Event> to <Reaction>
  for <event-source-value>
  <property> = <value>
```

`to <Reaction>` is required. The reaction must resolve unambiguously and have an event trigger on the stated event. The locator matches given facts of that event, narrowed by `for` and every stated value. Exactly one given occurrence must match; zero or several is an error. An empty body is sufficient if there is exactly one given fact of that event. Typed examples expand before matching.

Delivery identity is the pair (reaction identity, zero-based given-fact position). This mirrors Chronicle's per-reactor delivery identity, not event-value equality as a runtime deduplication guarantee. The canonical admission design stores `whenRedelivered: {"reaction": "<id>", "given": <index>}`. Equivalent locators produce identical bytes.

The reference establishes givens without reactions, fires only the named reaction against the existing fact and then settles the ordinary cascade. It never appends the given fact again. New effects occur at `given clock`; event expectations compare all newly accepted facts. Unhandled rejections remain assertable. `given caller` does not supply an actor to an invocation. Recovery redelivery is ordinary observation, not replay or `[OnceOnly]` behavior. Clock, application-trigger and capture repetition keep their existing forms.

### No-event assertion

`then no events` explicitly asserts no new events. It conflicts with event expectations, `then events in any order`, error expectations and denial. It is rejected after `when append` in this increment. It remains syntax-only until it is admitted together with refusal handling and redelivery. Every existing ESM version and its strict reader reject successful action specifications without a success outcome; omitting event expectations alone is not an admitted no-event assertion. Its canonical representation and reference execution are established at that shared admission checkpoint, not by relaxing existing versions' contracts.

### Admission and reference execution

Reaction refusal handling, redelivery and explicit no-event assertions require a **new executable model version admitted together**. No number is assigned here. The number is assigned only at the serialized release-ready admission checkpoint under 0025, after all 0004 gate-2 evidence exists.

Before that checkpoint syntax, printing, syntax JSON, validation and authoring readiness may land. Binding rejects refusal branches, refusal expressions and redelivery with PLAY0268 naming “reaction refusal handling and redelivery … not admitted by any supported executable model (ESM) version yet (#433)”. Binding also rejects `then no events` with PLAY0268 naming explicit no-event assertions, and MCP readiness names each feature as unadmitted. Existing ESM versions, canonical bytes, revisions and outcomes are unchanged.

Reference-runner implementation before admission is allowed only behind that gate, with specifications exercising an internal semantic model without claiming a version. If that cannot be isolated from public ESM admission, runner work remains at the admission checkpoint. No new serialized semantic members, numbered goldens or version claims are introduced by syntax-only delivery.

At admission the canonical design adds ordered `onRefused` branches to invocations, omission when absent, with selector and optional constraint identity, and produced events (an empty production list means acknowledgement). Refusal expressions identify their member and String type. Strict reading and programmatic validation enforce scope, branch shape, redelivery index and trigger compatibility; activation occurs only when used. Golden and source-backed corpus vectors, reference outcomes and consumer admission tracking complete 0004's gate.

## Options considered

- **Ordered explicit refusal branches and one-reaction redelivery (chosen).** They express a business refusal separately from transient failure and preserve recovery delivery identity.
- **Include authorization in bare refusal.** Rejected: a catch-all could conceal a missing reaction actor. Explicit authorization handling keeps that decision visible.
- **Continue invoking after a handled refusal.** Rejected: a refused claim must not permit the next external effect. Stop-after-handled matches Arc's command side-effect executor's stop-at-first-failure discipline.
- **Treat concurrency or Unsupported as a refusal.** Rejected: it would acknowledge an uncompleted effect or a model defect instead of retrying or failing closed.
- **Append the event again or redeliver to every observer.** Rejected: recovery is delivery of an existing fact to one observer partition, not another append.
- **Assign a version at design acceptance or extend a released contract.** Rejected under 0025: a new contract is numbered at admission, not before it is ready.

## Default if unanswered

The accepted design applies, while binding stays closed until admission. Existing models retain their outcomes. Authors can state intent but cannot execute new refusal, redelivery or explicit no-event behavior yet; execution targets cannot silently ignore it.

## Timeline and scope

This design governs #433 from acceptance through its eventual admission. In scope: invocation refusal branches, their values, stop-after-handled semantics, event recovery redelivery and the no-event assertion. Out of scope: reaction actor declaration (#383), collection fan-out (#286), direct-production or capture refusal handlers, durable external-effect deduplication, replay, invocation assertions, operations and consumer realization. Consumer admission remains separately tracked at release.

## Verification

**Done when:** both compilers parse and print the forms with matching syntax JSON; validation identifies malformed, unreachable and invalid branches and nonunique redelivery locators; binding and MCP readiness fail closed by feature name; binding refuses `then no events` until admission and validation rejects append actions. At admission the reference executes first-match, explicit authorization, stop-after-handled, one-observer redelivery and explicit no-event assertions, with unchanged prior-version bytes and complete 0004 evidence.

**Verify by:** parser/printer and conformance specifications; focused validation, binder and MCP readiness specifications; no-event binding refusal and strict-reader rejection of outcome-less actions; reference refusal/redelivery/no-event specifications at admission; prior golden and corpus comparisons; strict-reader rejection and consumer tracking review. A build alone does not advance this record's stage.

## Consequences

Refused claims can be acknowledged or recorded without hiding transient failures. Recovery delivery can be specified without inventing another event. The cost is explicit authoring and a separate admission checkpoint; syntax support alone never promises executable behavior.

## Status notes

**2026-10-07 — technical correction: no-event admission remains closed.** The no-event assertion waits for the same admission checkpoint as refusal handling and redelivery. The existing contract of every supported ESM version, including its strict reader, rejects an action specification without at least one success outcome (unless it asserts a rejection). Omitting event expectations therefore was not an executable no-event assertion by itself. Relaxing that rule across existing versions would change their accepted contract. Syntax, printing, validation and editor support remain available, but binding `then no events` now fails with PLAY0268 naming explicit no-event assertions, without a version number. The original success-outcome rule is retained for every action kind and version; no new semantic members or executable acceptance are introduced here.

**2026-10-07 — accepted semantic design.** Accepted by Sindre Alstad Wilting on 2026-10-07 after reviewing the amended text. Acceptance includes the correction that bare refusal excludes authorization and confirms that `then no events` waits for the shared admission. This accepts the semantic design only: `stage: none`, no version claim and no executable admission. The version number is assigned at admission under 0025.

**2026-10-07 — reference execution deferred to admission.** The current runner takes a capability-admitted `SemanticExecutionPlan` backed by an `ExecutableSemanticModel`. Its invocation and specification records are sealed public ESM contracts with no refusal branches, refusal values or redelivery action. There is no separate internal semantic model in which specifications can exercise these features. Implementing them now would require new public semantic members or a second, out-of-contract execution representation. Execution therefore lands at admission, together with the canonical members, strict validation and reference specifications required by 0004. The syntax-only delivery keeps PLAY0268 closed and introduces no semantic members or version claim.

**2026-10-08 — correction: Arc runs reactor commands as the system only when marked.** The "Until #383" paragraph and `Documentation/screenplay/reactions.md` say Arc runs a reactor's commands as the system. That premise is wrong. Arc does so only when the reactor carries `[ExecuteCommandsAsSystem]`; otherwise there is no principal and `[Authorize]` and `[Roles]` deny. The reference runner's no-caller invocation therefore matches an unmarked Arc reactor, not every Arc reactor. [0043](0043-reaction-identity-runs-as.md) specifies `runs as` for [#383](https://github.com/Cratis/Screenplay/issues/383). The `reactions.md` line and the PLAY0557 message are corrected with #383's implementation. The Decision text and the choices of this record are unchanged.
