---
id: 0113
title: Performer bodies are rendered, with a documented typed context
status: proposed
stage: none
class: contract
reversibility: cheap
decider: Sindre Alstad Wilting
amends: 0097
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Documentation/screenplay/**
---

## Context

Decision 0097 (still `proposed`) makes a query with a `performer` an opaque read: the model records the code it needs
(`QueryPerformer`), reports `PLAY0270`, and leaves the query out of the executable model. A specification that reads
such a query is left out too (0097 item 2; `queries.md:282`). An amendment puts the query's shape on the requirement so
a host can render the query without executing it.

Stage renders the performer query as a stub that throws `NotImplementedException`
(`SemanticStateViewArtifactRenderer.cs:251-271`, message at `:270`). Reducers, policies, validation, generation and types
already render verified bodies through `SemanticImplementationAdmission.TryGetVerifiedBody`
(`SemanticImplementationAdmission.cs:57-121`; call sites `SemanticCratisAdmission.Reducers.cs:74`,
`SemanticPolicyArtifactRenderer.cs:178`). Stage #743 proposes the same for performers.

The documented performer context is arguments, tenant, caller, causation and received-at (`queries.md:236-278`,
`context.md:26-35`). Arc's `QueryContext` is not the full documented context (`QueryContext.cs:22-50`). The generated
method names `IReadModels` (`SemanticStateViewArtifactRenderer.cs:253`), while #743 proposes a typed source.

Performers are the escape hatch, not the way to state everyday predicates: they cannot be specified or evaluated, so
putting "overdue" or "dormant" there takes the main domain rules out of `screenplay test` (the query predicates
record). The live Host keeps treating a performer as opaque, so in Play a performer-backed list shows nothing useful.

This record is allowed by the record that lifts the freeze (0105 on branch `screenplay-1035-typed-capture-key`).

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

## Decision

1. **Render the body.** A performer body that is resolved and verified is rendered as the method body, with typed
   parameters, as Stage #743 describes. A body that is missing, does not match its recorded hash, or does not compile
   blocks a runnable result. A throwing skeleton is allowed only in an explicitly incomplete scaffold. A hash proves
   integrity, not safety or correctness.
2. **The context is Screenplay's.** The `context` passed to a performer is the documented one: arguments, tenant,
   caller, causation and `ReceivedAt` (the receipt instant of 0087). Hosts adapt Arc's `QueryContext` to it. A field a
   host cannot supply is named as unsupported, not omitted silently. `file` performers compile the same way under every
   host. The meaning of `Occurred` is kept, or an explicit alias to receipt time is provided.
3. **A binding contract that is versioned.** The contract names language, argument names and types, result cardinality,
   delivery, cancellation, context and read access. Stage does not silently redefine what `readModels` is.
4. **Observable performers own their stream.** Lifetime and dependencies belong to the body. The automatic clock refresh
   of live answers does not apply to arbitrary code.
5. **Visible in the live Host.** The live Host does not run the body. A table or view fed by a performer query shows
   "performed by code; runs in the rendered application" instead of an empty list or an error. Running verified bodies
   in the Host is a later step and a separate product decision (isolation, security, deployment).
6. **Diagnostics name the file.** A compile error in a body reports the `.play` or performer file. Rerendering keeps
   ownership of implementation files.
7. **Not for time lists.** Documentation and the AI corpus say: prefer declarative `where` and filters; a performer is
   the escape hatch.
8. **Amendment of 0097.** Item 1 of 0097 stays: a performer query is an opaque read of the executable model. This
   record adds that Stage renders its body. 0097 items 2 to 5 stay. This record does not change `QueryPerformer`, any
   ESM byte or any version.

## Options considered

- **Hand-edit generated methods or use partial classes.** Rejected: `cratis render` refuses changed files.
- **Force all logic into the language.** Rejected: it creates an unbounded language.
- **Run arbitrary code in the live Host by default.** Rejected: isolation and deployment are separate decisions.
- **Treat performers as the main time-relative mechanism.** Rejected: they cannot be specified or evaluated.
- **A Stage-only context adapter.** Rejected: hosts would disagree about the context.

## Consequences

- **Stage:** `RenderPerformedQuery`, the admission role, the reference documentation, the Host placeholder.
- **Screenplay:** a documentation note only. **Arc:** supplies the services behind the context.
- **AI corpus:** the guidance in item 7. **Scene, Chronicle, CLI:** none; the CLI reports incomplete and failed bodies
  distinctly.
- A stub becomes an implementation; nothing breaks.
- Risks: a misleading "executable" success when a body is missing, stream disposal, dependency resolution and target
  lock-in.

## Open points

1. Whether 0097 should be accepted together with this record. It is `proposed` on main, and this record depends on it.
2. Which `context` fields Arc cannot supply today (tenant, identity and causation need adapters).

## Related

Amends 0097. Builds on 0004. Screenplay #862, #981; Stage #743; the query predicates record.
