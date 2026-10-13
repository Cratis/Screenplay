---
id: 0075
title: Carry the stored event type name in the executable model
status: proposed
stage: none
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
- Omitted when equal to `name`. A model without event pins keeps its bytes, revisions and version. Only a model that pins an event writes the member and claims the new version.
- Placement: on the contract entry (the current revision), once. Prior revisions share it, because generations are revisions of one stored contract (0015). If generations of one contract declare different pins, binding fails; a stored type name cannot change between generations.
- Version: the next free ESM version at admission, numbered then under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) and frozen on publication under [0066](0066-published-esm-versions-are-frozen.md). In the current queue (v16 = 0074, v17 = 0072, v18 = #616 2b-2) that is v19. The order is an open question below.
- Readers below that version reject the member (strict reader). A model with an event pin cannot be represented faithfully earlier, so it claims the new version, and a consumer that has not admitted it refuses the model instead of rendering a wrong stored type.
- The `PLAY` report-only information diagnostic for event `id` stays for description and documentation only. Event `id` becomes executable.

Consumers:

- **Reference evaluator:** no behavior change. Facts are identified by contract and display name; the stored name does not affect evaluation.
- **C# and TypeScript compilers:** emit and strictly read `storedName` identically, and select the new version only when an event pin exists. Both are covered by the same canonical vectors.
- **MCP:** declaration details and `propose-rename` already show the pin; the exported ESM and any mutability report show `storedName`.
- **0070:** `--from-esm` reads the shipped `storedName` (absent means the name), replacing the fail-closed path from #686.
- **Stage:** emit `[EventType("<storedName>")]` when `storedName` is present, argument-free otherwise, and refuse a model whose ESM version it has not admitted. Stage lists `storedName` in its ledger as rendered, not ignored.
- **Corpus:** a new canonical vector with a pinned rename and a mixed pinned/unpinned model; existing vectors stay byte-identical.

Follow-up issues: Stage (emit and admit), Studio and cli (version admission), `Cratis/AI` (skills that say event `id` is metadata).

## Options considered

1. **Carry the stored name in the ESM (taken).** One declared fact, readable by every renderer, fixed in a frozen version.
2. **A renderer-side rule.** Not possible: a renderer sees one model, not its history, and cannot tell a pinned rename from a new event.
3. **Drop pins and make a rename always a new contract.** Not taken: it removes the only way to rename an event without orphaning stored events, and it changes 0044 and 0070.
4. **Use `ContractId` as the Chronicle event type id.** Not taken: it changes the stored type of every existing rendered application, not only pinned ones.
5. **Always write `storedName`.** Not taken: it changes the bytes of every existing model and forces a new version on all of them. See the open questions.

## Default if unanswered

Pins keep being dropped silently. Every pinned rename in a rendered application changes its stored event type, and 0070's `--from-esm` stays fail-closed. The cost grows with each release that renders pinned models.

## Scope

In: the ESM member and version, both compilers, strict reading, corpus vectors, MCP export, the 0070 admission read, Stage's emission rule. Out: revision-qualified event consumption (0015), changing how pins are authored or when `propose-rename` pins (0044), and migrating stored events.

## Consequences

- Breaking for applications that already rendered a pinned rename. Their events are stored under the new name. Re-rendering with this change switches the type to the pinned old name, and those events are no longer read. Such an application must remove the pin (accepting that events stored under the old name are not read) or migrate the stored events first. Screenplay cannot tell which applies. See the open questions.
- Omit-when-equal keeps old models' bytes and versions, and only pinned models move to the new version. 0066 is respected: no admitted version changes.

## Open questions for the decider

1. **Ordering.** The change fixes a data-compatibility fault, so it may belong before v18 (#616 2b-2), or before v16 and v17, rather than at v19. Reordering is cheap only while those versions are unpublished.
2. **Omit-when-equal or always present.** Recommended: omit-when-equal. Always-present is simpler to read but moves every model to the new version.
3. **Applications that already stored under the new name.** Accept the break with the guidance above, or add an explicit opt-out such as a deprecation period in which Stage warns before it switches.
4. **Older-version readers.** The proposal is refusal, because a pinned model claims the new version. Confirm that a model with event pins, which used to bind at older versions with the pin dropped, should now stop being renderable by consumers that have not admitted the new version.

## Verification

**Done when:** a model with a pinned rename emits `storedName` and claims the new version in both compilers, a model without event pins keeps its bytes and version, a strict reader below the version rejects the member, Stage renders `[EventType("<old name>")]` for the pinned event, and `--from-esm` admits the shipped stored name.

**Verify by:** canonical vectors (pinned, unpinned and mixed) read by both compilers, a version-selection spec, a strict-reader refusal spec, a Stage conformance run on the pinned vector, a `baseline admit --from-esm` spec. Check the `Decision: 0075` trailer.
