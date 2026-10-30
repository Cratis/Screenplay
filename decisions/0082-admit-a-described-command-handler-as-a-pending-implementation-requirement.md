---
id: 0082
title: Admit a described command handler as a pending implementation requirement with a fixed result contract
status: accepted
stage: none
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/CommandParser.cs
  - Source/DotNET/Screenplay/Diagnostics/DiagnosticCodes.cs
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/WorkspaceImplementationInventory*.cs
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.Contracts/Golden/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/commands.md
  - Documentation/screenplay/ast-authoring.md
  - Documentation/screenplay/interoperability.md
  - Documentation/screenplay/diagnostics.md
---

## Context

Users describe command logic in words (Studio board logic descriptions) on slices whose shape a renderer can otherwise
produce. Today that text reaches the model only as `description`, with no requirement identity, so a renderer cannot stub
it and nothing proves it was implemented. The cross-repo design (Cratis/Stage#316 v2, lead L18b) wants: Screenplay turns
described logic into a requirement with a stable RequirementId and a typed contract; Stage renders the declarative shape
plus a typed stub only in an explicit opt-in Stub mode; Studio's AI fills only stub bodies; verification proves every stub
is filled.

The language already has the form. `handler` with an `implementation` wrapper and no `file`/code is a *pending* intent
with ordered `hint`s ([#307](https://github.com/Cratis/Screenplay/issues/307), #362; `Documentation/screenplay/commands.md`
"The handler block"). The workspace inventory already gives a pending handler a RequirementId
(`ImplementationRequirementIdentity.Create(owner, CommandHandler, null)`), identical to the attached handler's, and decision
[0012](0012-typed-context-descriptor-and-command-handler-role.md) defines the CommandHandler typed context. Three things
stop it: the parser refuses `handler` + `produces` (`PLAY0035`); the binder creates no requirement for a handler without a
payload; and no ESM version admits handlers at all (`PLAY0268`). Command productions follow decision
[0023](0023-command-production-model.md).

## Decision

1. **No new construct.** A described command is a command whose `handler` has an `implementation` wrapper with no `file`
   and no code (pending), optional ordered `hint`s, and its `produces`. `description` stays the business "what"; hints are
   the implementer's "how". Studio exports a board logic description as the pending intent's hint(s).
2. **What the body owns: event payload values, nothing else (result version 1).** Under a pending intent `handler` and
   `produces` may coexist (`PLAY0035` no longer fires for that case; it still fires for `file`/code handlers). Every other
   part of the command stays declarative and normative, exactly as decision 0023 defines it:

   | Part | Owner in result version 1 |
   | --- | --- |
   | Which events, how many, in which order | Declarative: each `produces` line yields exactly one event, in declared order |
   | Destination (`for`, routes, allocation for plain productions) | Declarative (decision 0023) |
   | Tags and other production metadata | Declarative |
   | Generated values | Declarative; generated before the body and available to it |
   | Response | Declarative: the declared command property, as today |
   | Authorization and validation | Declarative; run before the body, which is never reached on denial or refusal |
   | Event property values | **The body.** Mappings are non-normative hints; required event properties may be unmapped |

   Renderers and the reference evaluator never evaluate a described command's event-property mappings. The validator's
   "every required event property is mapped" rule (`SemanticModelValidator.Commands`) does not apply to productions of a
   described command; it applies everywhere else. Non-normativity is limited to event-property mappings.
3. **Admitted shape in result version 1.** Unconditional productions only: `produces when`, repeated/occurrence productions
   and anything else whose cardinality the body would decide are refused at binding with a specific diagnostic until a
   later result version defines them. At least one production is required; a response-only pending command is refused at
   binding (intentionally out of scope: the body would have nothing to produce). The body does not reject; a result
   contract with a `ValidationResult` is a later result version.
4. **Authoring stays valid; admission is a binding rule.** Existing pending intents without `produces`, response-only
   pending intents and bare `implementation` remain valid source and keep their authoring-only inventory (no parser
   error, no migration, no invented productions). Only executable binding refuses them, with the diagnostics in item 9.
5. **Canonical ESM carries identity and contract; hints stay in the sidecar.** Under a new ESM version (number allocated
   by L15), the canonical command carries `HandlerRequirementId` and the result-contract version (1). The
   `SemanticImplementationRequirement` for it stays in the requirements sidecar like every other requirement (decision
   0023 keeps lifecycle metadata outside ESM): role CommandHandler, the existing identity rule (owner semantic id and
   role; never path, hints, description or productions), a new attachment state `Pending` (no file, language, body span
   or content hash), its ordered hints, the inventory's provisional/ambiguous flags, ContextVersion 1, ResultVersion 1.
   The CommandHandler typed-context descriptor is emitted for it. No source spans or path-dependent ordering enter the
   ESM. Canonical writer/reader, revision hashing and version validation cover the new members, with round-trip and
   downgrade tests; a dangling, mismatched or duplicated handler reference, or an unknown context/result version, is a
   contract error. Models without a described command keep their prior version and bytes. A consumer that does not admit
   the version refuses the model, so described logic is never silently dropped; Stage additionally refuses a Pending
   requirement outside its explicit Stub mode and refuses provisional or ambiguous requirements.
6. **One handler per described command.** The parser keeps "last handler wins" for existing models, but a command with a
   pending intent and any other `handler` occurrence (in either order, in source or composed/programmatic syntax) is an
   error in both compilers, so no intent is silently dropped.
7. **Scope.** Only command handlers. Attached (`file`/code) handlers remain refused with `PLAY0268`. Pending intents on
   reaction effects and query performers stay authoring-only. A pending intent on a command with legacy `reads` stays
   refused.
8. **Reference evaluation stops at the body.** Authorization, validation and earlier outcomes evaluate as today. When
   execution reaches a described body, directly or through a reaction that invokes the command, the result is
   `Unsupported` with a reason naming the command and RequirementId; it is never passed or failed, and the mappings are
   not evaluated. Historical `given` events whose producer is a described command are ordinary facts. Runner summaries
   report executed and unsupported counts separately, and a run in which every specification is unsupported is not green.
   Proving a stub filled needs positive, body-reaching acceptance in the target (Stage design §9), not a denial-only pass.
9. **Diagnostics (codes by L15).** Information: a command is described but not implemented (replaces `PLAY0268` for an
   admitted described command). Binding errors: described command without productions; conditional or repeated
   productions under a pending intent. Parser error (both compilers): pending intent plus another handler occurrence.
10. **Realization is a later step.** Moving a filled body to `handler file …` keeps the RequirementId, but an attached
    handler cannot keep `produces` today, so the result contract would be lost. Until a later decision defines a versioned
    result contract beside an attached handler and a body/signature/context ABI, renderers regenerate and re-verify, and
    Screenplay defines no reuse of a filled body across renders. Realization is the next decision. Until it lands, Studio's per-file three-way
    regeneration (StudioIssues#655, Studio decision 0018) is what keeps an AI-filled body across regenerations.
11. **Target neutrality.** The requirement, its hints, state and the typed-context and result contracts are
    language-neutral (decision 0012). Only a render target turns them into a stub signature, a body marker or a file path.
    Hints are free text written by the model's authors; they leave the model only through the sidecar a host already
    receives, normalized like other canonical strings.

## Options considered

- **A command-level `implementation` block without `handler`.** Not taken: a second form for the same intent, and #307
  already ships the pending `handler implementation` form, inventory and identity.
- **Imply "needs implementation" from a non-empty description.** Not taken: descriptions are business narrative and appear
  on commands whose declarative productions are complete; the explicit pending intent is the signal.
- **Mappings normative (body may override) or forbidden.** Normative would force renderers to evaluate expressions inside
  a stub and make two sources of truth for a property; forbidden discards intent Studio already exports. Partial,
  non-normative keeps one source of truth: the body.
- **Authoring-only (no ESM change).** Not taken: a target that ignores the intent renders the declarative productions and
  drops the described logic without a trace.
- **Let the stub produce any slice event.** Not taken: the stub signature and verification need a fixed output.

## Default if unanswered

Described logic stays a description without identity; renderers cannot stub it, and Studio's AI fill has no contract.

## Timeline and scope

Holds until superseded. In scope: pending command-handler intents with unconditional `produces` (parser, binder,
validator, ESM members, sidecar requirement, canonical JSON, diagnostics, reference-evaluator verdict, MCP views, docs). Out of scope: attached-handler
admission, realization and body reuse, rejection results, reaction-effect and query-performer intents, and Stage's own
slice-1 limits (StateChange, one plain production of bare events, no generated values, tags, routes, `produces when` or
occurrences), which Stage enforces.

## Verification

**Done when:** a described command with unconditional `produces` (including an unmapped required event property)
parses and binds under the allocated ESM version, the canonical command round-trips `HandlerRequirementId` and result
version, and the sidecar carries a Pending requirement with hints and flags; the RequirementId equals the inventory's and
survives hint/description/produces edits and pending→file; models without described commands are byte-identical;
response-only, production-less and conditional/repeated described commands are refused at binding while their source
stays valid; pending plus another handler is a parser error in both compilers (both orders); attached handlers still
`PLAY0268`; reference evaluation preserves denial/validation outcomes and returns Unsupported only when the body is
reached (direct and reaction-invoked), with counts; shared vectors hold both compilers to the parser rules. **Verify by:** binder, validator, serializer, runner and MCP specs, conformance
vectors, contract golden and a golden diff against `main`.

## Consequences

Studio and Stage get a stable, typed, target-neutral requirement for described logic, and older consumers refuse it
instead of dropping it. `produces` gains a second meaning (result contract) under a pending intent, which docs must make
explicit. Realization needs another decision before filled bodies survive a re-render.

## Related

#307, #362; decisions 0002, 0012, 0023, 0004 (admission); Cratis/Stage#316 (design v2, L18b).

## Implementation note — #1099 S5

The opt-in reference-run `HandleAsync` seam executes only a resolved, content-verified, provider-defined CommandHandler with context/result v1. `SemanticCommandBodyResult` carries positional semantic composites only: one payload per unconditional produces line, in declaration order, including separate slots when lines name the same event. There are no event descriptors, destinations, tags, routing, occurrence metadata, append envelopes, decisions or rejection channels in the answer. The executor validates the whole batch and required/optional typed properties before producing facts through the existing declarative machinery; mappings remain hints and are not evaluated. Pending handlers and default providers remain unsupported. No canonical bytes, ESM/ABI version or decision wording above changes; result v2, conditional/repeated productions and body-decided cardinality remain outside this seam. Verification is pending the #1099 gate.
