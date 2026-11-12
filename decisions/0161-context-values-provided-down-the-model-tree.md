---
id: 0161
title: Models add their own values to `$context` with `provide`, inherited down the model tree
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

`$context.` reaches only the command and query contexts today (tenant, identity, causedBy, causation, occurred, command, arguments), and every job (command handler, query performer, rule, policy) gets its own fixed context type. A model cannot add an ambient value of its own, such as the vessel being viewed, the project the user works in or the audiences that apply, and no level lets a module or feature narrow such a value for what is beneath it. Each slice re-derives them. Availability (decision 0162, Cratis/Screenplay#1298) needs "the vessel" as a value in context first. Without a decision, availability would grow a private way to read the vessel and every other feature would copy it.

This record settles the design tracked in Cratis/Screenplay#1297 under the epic Cratis/Screenplay#1296.

## Decision

`$context` stays the single namespace. There is no separate tree and no block keyword. A model adds a value with `provide <name> <Type>[?] from <source>` in the body of the application, a module, a feature or a slice. Each level inherits every value above it by name, and the nearest declaration wins. An override keeps the type.

- **Levels:** application, module, features (which already nest) and slice. Per-command or per-query `provide` is not added now; it can be added later if a slice needs different keys for its command and its query. Tooling (hover, board, MCP) shows the winning declaration for any value.
- **Application level** is what `$context` already holds, supplied by the runtime: tenant, identity, causedBy, causation, occurred, plus `audiences`. Nothing existing changes meaning.
- **Reserved names:** tenant, identity, causedBy, causation, occurred, audiences, and the job-specific command and arguments. Redeclaring one is an error, so no level can change who a request is.
- **Sources:** a read model query (event-built or performer) with `by ...`, a literal, an expression over other context values, `from host` (a declared name and a type the host supplies), or an inline `csharp` / `file` body where policies and performers allow it. What a value reads is visible in the model.
  - A cycle between provided values is an error.
  - Code and host sources are opaque: the reference evaluator reports a gate that reads one as unsupported, never as allowed or denied, as it does for an opaque policy predicate.
  - Observable queries are not allowed as sources, because resolution is per request.
- **Resolution:** lazy, once per request. A provider runs when its value is first read and the result is reused for the rest of the request, so every gate in the request sees one consistent value. The language promises no cross-request cache. The engine may cache or invalidate reactively underneath, within the staleness bound set by decision 0162.
- **Contexts:** `CommandContext`, `QueryContext`, `PolicyContext`, `RuleContext` and a new `ProvideContext` (the context of a code body in a `provide`: the job's arguments, tenant, identity, occurred and previously resolved values) each get a typed `Values` member, so `context.Values.Vessel` reads the value the model provides.
- **Failure:** absence is unknown, failure is an error.
  - Absence (missing key, no matching row, host value not supplied) is unknown. A gate denies, since unknown never opens a gate. A `produces` mapping refuses the command, so no event is appended. Code sees `null` for an optional value (`provide vessel Vessel?`).
  - A value declared without `?` is required: when absent, the request is refused with a clear reason.
  - Failure (store unavailable, code body throws, timeout) fails the request as a retryable error, logged, and is never reported as a denial. An outage is not shown as "not available".

## Options considered

- **A `context` block or keyword with its own tree.** Not taken: a second namespace beside `$context` would make authors choose where a value lives and how it is read. One namespace with `provide` lines keeps the existing read path (`$context.vessel...`).
- **Resolve once per session, or cache across requests in the language.** Not taken: the language would then promise a staleness it cannot keep. Once per request gives consistency inside a request and leaves caching to the engine.
- **Restrict sources to read model queries.** Not taken: a value such as a host-supplied entitlement set, or a derived expression, has no query. The cost, opaque sources the evaluator cannot decide, is accepted and reported as unsupported.
- **Keep identity out of rules.** Not taken. `RuleContext` has no `Identity` today, and a value derived from identity (`provide caller = $context.identity.roles`) becomes readable in a rule. The author makes that choice explicitly in a `provide` line.
- **Lowest level at the feature.** Not taken: a feature-level provider cannot state the key for both a command (`$context.command.vesselId`) and a query (`$context.arguments.vesselId`); a slice can.

## Default if unanswered

Availability would have to read the vessel through a one-off mechanism, and each slice would keep re-deriving ambient values. The cost is the drift that availability exists to remove.

## Timeline and scope

In scope: the `provide` line at four levels, inheritance and override, reserved names, the source kinds above, per-request lazy resolution, the typed `Values` member on all five contexts, and the absence and failure rules. Out of scope: a cross-request cache in the language, observable sources, and a per-command or per-query `provide`.

## Verification

Done when: a model can declare `provide vessel Vessel from query VesselByKey by $context.arguments.vesselId` at module level, override it in a feature, and read `$context.vessel` in a gate on both compilers, with the surfaces in the language-change checklist updated, a typed-contexts sidecar update and an ESM revision admitted.

Verify by: compiler specifications for inheritance, override, reserved-name and cycle errors; evaluator specifications that an absent required value refuses and an opaque source is reported unsupported; the compile-all-samples specification.

## Consequences

Models gain ambient values without a new namespace, and availability can read the vessel. All four job contexts change shape, which is a language change on every surface and needs an ESM revision and a typed-context sidecar update. Because identity-derived values reach rules, a diagnostic that flags a provided value derived from identity or causedBy that a rule reads is still to be decided.

Open:

- Whether that diagnostic exists, to be settled with the follow-up.
- Per-command or per-query `provide`, only if a slice needs it.
- The exact ESM version and the typed-context sidecar shape, settled with the grammar change.

Related: Cratis/Screenplay#1296 (epic), Cratis/Screenplay#1297 (this design), Cratis/Screenplay#1298 and decision 0162 (availability, the first consumer).
