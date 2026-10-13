---
id: 0075
title: Carry the stored event type name in the executable model
status: accepted
stage: none
decided: 2026-10-13
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
---

## Context

[#704](https://github.com/Cratis/Screenplay/issues/704). An event can pin the name it is stored under: `event NewName` with `id "OldName"`. [0044](0044-evolve-event-properties-through-mcp.md) makes `propose-rename` pin by default so stored events stay readable, and [0070](0070-derive-event-mutability-from-an-admitted-release-baseline.md) defines an event's effective stored type name (`eventType`) as its `id` pin, otherwise its name.

The pin never reaches the executable model (ESM):

- `SemanticModelBinder.Structure.cs` (~line 179) reports an event's description, documentation and `id` as authoring metadata with the report-only `PLAY` information diagnostic.
- `SemanticEventContract` (`SemanticBehaviors.cs`) carries `Id`, `ContractId`, `Revision`, `Name` and properties. `Name` is the display name.
- Stage's ledger (`Source/Rendering.Cratis/Semantics/SemanticSurfaceLedger.cs:88`, read-only check) ignores `EventContractId` because "Stable contract identity is already validated by Screenplay; the generated event uses its name."

A renderer therefore generates the Chronicle event type from the new name. A pinned rename silently changes the stored type of a rendered application, and events stored under the old name are no longer read. Sources and streams do not have this gap: under [0036](0036-admit-event-sources-streams-and-command-routes.md) their `id` pin is carried as `sourceKind`/`streamKind`. A shipped ESM also cannot say which stored name a release used, which 0070's `--from-esm` admission needs ([#686](https://github.com/Cratis/Screenplay/issues/686) fails closed on a mismatch until then).

Renderers cannot recover the pin from anything else. The ESM is their only input, so the information has to be in it.

Chronicle and Arc: `[EventType]` takes an explicit identifier string for an existing stored contract and is argument-free otherwise. The generation is a separate number (0015), so the stored type id belongs to the contract, not to a revision.

## Decision

Carry each event contract's effective stored type name in the ESM as an admitted member, `storedName`.

- Value: the `id` pin when the event declares one, otherwise the name.
- Omitted when equal to `name`, also when the pin itself equals the name. A model without event pins keeps its bytes, revisions and version. Only a model that pins an event writes the member and claims the new version.
- Placement: on the contract entry (the current revision), once. Prior revisions share it, because generations are revisions of one stored contract (0015). If generations of one contract declare different pins, binding fails; a stored type name cannot change between generations.
- Version: no number is reserved. Under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) the next free number goes to whichever admission is release-ready first, and the number is frozen on publication under [0066](0066-published-esm-versions-are-frozen.md). Priority: 0075 goes ahead of any other admission that has not yet claimed a number when it is release-ready, because it fixes a data-compatibility fault. It does not stall an admission that is ready first (for example 0074), and that admission keeps its claim.
- Fail closed on older versions. A reader below the new version rejects the member (strict reader), and a consumer that has not admitted the new version refuses a model that carries event pins instead of rendering a wrong stored type. A source whose language version is pinned below the new version and that declares an event pin gets a binding error, not a silent drop.
- Valid pin: non-empty, no surrounding whitespace, and acceptable to Chronicle as an event type id. Until Chronicle evidence says otherwise, the character rule is the same identifier rule as event names. Renderers escape the value as a string literal.
- Collisions: binding rejects two events in one application with the same effective stored name, for example `event A id "B"` next to `event B`.
- Interim mitigation, shipped as a patch before the admission and without any ESM version: the event `id` pin diagnostic becomes a warning, "the pin is not yet carried to renderers; generated code uses the new name". MCP `propose-rename` output for event renames carries the same caveat.
- The `PLAY` report-only information diagnostic for event `id` stays for description and documentation only. Event `id` becomes executable.

Consumers:

- **Reference evaluator:** no behavior change. Facts are identified by contract and display name; the stored name does not affect evaluation.
- **C# and TypeScript compilers:** emit and strictly read `storedName` identically, and select the new version only when an event pin exists. Both are covered by the same canonical vectors.
- **MCP:** declaration details and `propose-rename` already show the pin; the exported ESM and any mutability report show `storedName`.
- **0070:** removing a pin changes the revision's `eventType`, which is refused on a baselined revision. The baseline records what the shipped release stored. When a release was rendered before this decision under the new name, the baseline entry's `eventType` is corrected by a person in a reviewed pull request (0070 allows hand edits of the file for this kind of correction), or the team re-admits from the shipped ESM with `--from-esm` once the ESM carries stored names. This is the explicit exception to the refusal. `--from-esm` reads the shipped `storedName` (absent means the name), replacing the fail-closed path from #686.
- **Stage:** a follow-up refuses re-rendering when an existing class's `[EventType]` id would change and points to the remedies above. Emit `[EventType("<storedName>")]` when `storedName` is present, argument-free otherwise, and refuse a model whose ESM version it has not admitted. Stage lists `storedName` in its ledger as rendered, not ignored.
- **Corpus:** a new canonical vector with a pinned rename and a mixed pinned/unpinned model; existing vectors stay byte-identical.

Follow-up issues: Stage (emit and admit), Studio and cli (version admission), `Cratis/AI` (skills that say event `id` is metadata).

## Options considered

1. **Carry the stored name in the ESM (taken).** One declared fact, readable by every renderer, fixed in a frozen version.
2. **A renderer-side rule.** Not possible: a renderer sees one model, not its history, and cannot tell a pinned rename from a new event.
3. **Drop pins and make a rename always a new contract.** Not taken: it removes the only way to rename an event without orphaning stored events, and it changes 0044 and 0070.
4. **Use `ContractId` as the Chronicle event type id.** Not taken: it changes the stored type of every existing rendered application, not only pinned ones.
5. **Always write `storedName`.** Not taken: it changes the bytes of every existing model and forces a new version on all of them.

## Default if unanswered

Pins keep being dropped silently. Every pinned rename in a rendered application changes its stored event type, and 0070's `--from-esm` stays fail-closed. The cost grows with each release that renders pinned models.

## Scope

In: the ESM member and version, the pin validity and collision rules, the interim warning, both compilers, strict reading, corpus vectors, MCP export, the 0070 admission read, Stage's emission rule. Out: revision-qualified event consumption (0015), changing how pins are authored or when `propose-rename` pins (0044), and migrating stored events.

## Consequences

- Applications that already stored events under the new name are accepted in Screenplay, which flags nothing: it cannot tell them from applications storing under the old name. Re-rendering with this change would switch the type to the pinned old name, so Stage refuses it (see Consumers) and points to the remedies: remove the pin with the 0070 correction, or migrate the stored events.
- The mixed case, with some events stored under each name, needs a data migration and is out of scope.
- Omit-when-equal keeps old models' bytes and versions, and only pinned models move to the new version. 0066 is respected: no admitted version changes.

## Resolved questions

Decided by Sindre Alstad Wilting, 2026-10-13.

1. **Ordering.** No version number is reserved. 0075 takes priority over unclaimed admissions when release-ready and does not stall one that is ready first. See Decision.
2. **Omit-when-equal or always present.** Omit when equal to the name, also when the pin equals the name.
3. **Applications that already stored under the new name.** Accepted in Screenplay with no flag. A Stage follow-up refuses re-rendering when an existing class's `[EventType]` id would change and points to remedies. The mixed old/new case needs a data migration and is out of scope.
4. **Older-version readers.** Fail closed: a consumer that has not admitted the new version refuses models with event pins, and a source pinned below the new language version gets an event pin as a binding error.

## Verification

**Done when:** a model with a pinned rename emits `storedName` and claims the new version in both compilers, a model without event pins keeps its bytes and version, a strict reader below the version rejects the member, Stage renders `[EventType("<old name>")]` for the pinned event, and `--from-esm` admits the shipped stored name.

**Verify by:** canonical vectors (pinned, unpinned and mixed) read by both compilers, a version-selection spec, a collision and invalid-pin binding spec, a strict-reader refusal spec, a Stage conformance run on the pinned vector, a `baseline admit --from-esm` spec. Check the `Decision: 0075` trailer.
