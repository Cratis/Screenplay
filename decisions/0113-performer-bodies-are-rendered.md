---
id: 0113
title: Performer bodies are rendered, with a documented typed context
status: accepted
stage: none
decided: 2026-10-23
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
   parameters, as Stage #743 describes. A hash proves integrity, not safety or correctness.

   When a body is missing, does not match its recorded hash, or does not compile, the render does not stop and does not
   claim to be runnable. Rendering is partial, never all-or-nothing (amended 2026-10-23, see Amendment):
   - Every other part is rendered.
   - The affected query becomes an explicit gap: generated code that compiles and, when called, fails with a message
     naming the query and the reason.
   - Each gap is reported as a warning diagnostic and listed in the render result.
   - The output carries a visible "not fully runnable" marker while any gap remains, and the render outcome is never
     reported as complete.
   - An exception that escapes the renderer is a bug, not a refusal.
2. **The context is Screenplay's.** The `context` passed to a performer is the documented one: arguments, tenant,
   caller, causation and `ReceivedAt` (the receipt instant of 0087). Hosts adapt Arc's `QueryContext` to it. A field a
   host cannot supply is named as unsupported, not omitted silently. `file` performers compile the same way under every
   host. The meaning of `Occurred` is kept, or an explicit alias to receipt time is provided.
3. **A binding contract that is versioned.** The contract names language, argument names and types, result cardinality,
   delivery, cancellation, context and read access. Stage does not silently redefine what `readModels` is.
   - **Name and record.** The version is the pair already carried by every implementation requirement:
     `ContextVersion` (the typed context) and `ResultVersion` (the result shape), both 1 today. It is recorded on the
     requirement in the executable model (`SemanticImplementationRequirement`) and is part of the requirement's
     identity. Stage copies it onto each implementation gap (`ArtifactImplementationGap`).
   - **Changes.** Either number changes only through a decision record.
   - **Existing renders.** A host that does not support a requirement's version, or a body verified against another
     version, renders that query as a gap under item 1. The warning names both versions, and nothing else in the
     render is affected. The author migrates by updating the body and verifying it again; the next render picks it up.
4. **Observable performers own their stream.** Lifetime and dependencies belong to the body. The automatic clock refresh
   of live answers does not apply to arbitrary code. Until a host can bind a body that owns its stream, an observable
   performer is rendered as a gap under item 1. It is never presented as a body-owned stream.
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

## Amendment (2026-10-23)

Decided by Sindre Alstad Wilting on 2026-10-23: "Partial render; explicit compilable gaps + warnings + 'not fully
runnable' marker; never abort or crash." This replaces the earlier wording of item 1, where an unusable body blocked a
runnable result and a throwing skeleton was allowed only in an explicitly incomplete scaffold. The intent is unchanged:
output must never pretend to be runnable. The means change: instead of producing no output, the render produces
everything it can and marks every gap explicitly. A silent throwing stub is still not allowed. The rule covers any part
of a model that a host cannot render, not only performer bodies. Stage applies it to performer bodies first (Stage
#743). The other paths that can still abort a whole render are tracked in
[Stage #761](https://github.com/Cratis/Stage/issues/761).

## Options considered

- **Refuse the whole render when one body is unusable.** Rejected by the amendment: one gap must not hide everything
  else that renders.
- **Silent throwing stub.** Rejected: it looks runnable and fails only at runtime.
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
- A stub becomes an implementation; nothing breaks. A body that cannot be rendered yields a partial render with
  explicit gaps, not an empty render.
- Risks: a misleading "executable" success when a body is missing, stream disposal, dependency resolution and target
  lock-in.

## Open points

1. Whether 0097 should be accepted together with this record. It is `proposed` on main, and this record depends on it.
2. Which `context` fields Arc cannot supply today (tenant, identity and causation need adapters).

## Related

Amends 0097. Builds on 0004. Screenplay #862, #981; Stage #743; the query predicates record.
