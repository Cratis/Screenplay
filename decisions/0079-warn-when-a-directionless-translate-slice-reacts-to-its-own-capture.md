---
id: 0079
title: Warn when a directionless Translate slice reacts to an event its own capture appends
status: accepted
stage: implemented
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: reversible
applies-to:
  - Source/DotNET/Screenplay/Parsing/PublicEventUsageValidator.cs
  - Source/DotNET/Screenplay/Diagnostics/DiagnosticCodes.cs
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/diagnostics.md
  - Documentation/screenplay/slices.md
  - Samples/**
---

## Context

[Decision 0049](0049-public-events-and-translation-direction.md) item 4 keeps a directionless `Translate` slice valid and
interprets it as inbound. One shape slips through that: a directionless slice that captures outside data **and** has a
`reaction` triggered by a local event the same slice's capture appends. With `direction inbound` written out, the same
slice is refused by `PLAY0616` (an inbound translation consumes foreign public events, not its own local facts). So the
implicit default accepts what the explicit one refuses, and the slice does two jobs: translating outside data into our
facts, and automating follow-up work on those facts. [#480](https://github.com/Cratis/Screenplay/issues/480) reshaped
the samples into a Translate slice (capture) plus an Automation slice (reaction). [#760](https://github.com/Cratis/Screenplay/issues/760)
asks the language to say so.

## Decision

1. **New warning `PLAY0694` (`DirectionlessTranslationReactsToOwnCapture`).** On a `Translate` slice with no `direction`,
   a `reaction` without a `from` filter that is triggered by an event some `capture` of the **same slice** appends gets one
   warning at the reaction, listing the distinct matching events. The message says: keep the capture in this Translate
   slice and move the reaction to an Automation slice. "Appends" means every capture `append`, including those under
   children and nested scopes; one capture may append several event types. Specification fixtures do not count.
2. **It stays a warning.** 0049 item 4 promised that legacy directionless translations keep compiling; a warning keeps
   that promise for ordinary compilation (builds run with warnings as errors, such as `--warnaserror`, will fail until the
   slice is split) and converges authoring guidance with explicit inbound. Making it an error would need a new decision
   and a deprecation cycle.
3. **No automatic repair.** The fix creates a new slice with its own name, placement and specifications; there is no
   unambiguous typed edit. The documented repair is the manual split shown in `slices.md`.
4. **Scope of the check.** Events are compared by resolved declaration on the assembled model (qualified references,
   imports and generations resolved; unresolved or ambiguous references do not match). It is a structural co-location
   check, not producer analysis: captures in other slices never establish a match, and other producers of the same event
   elsewhere do not suppress one. Reactions with a `from` route are excluded (captures are unrouted, so a routed reaction
   cannot consume their occurrences). Reactions to other events, projections, and slices that declare `direction` are
   unchanged; explicit inbound is covered by `PLAY0616`. The check runs for purely private models too, not only behind
   the public-contract validation that `PLAY0607`–`PLAY0620` use.
5. **Both compilers, editors, docs, samples.** C# and TypeScript compilers report it with shared vectors; Monaco and VS
   Code surface it through the language service; `diagnostics.md` and `slices.md` document it; samples stay warning-free
   (they already are after #480). No ESM or binding change.

## Options considered

- **Exempt the shape in PLAY0616 (accept it under explicit inbound too).** Not taken: it blesses one slice doing two jobs
  and contradicts the one-slice-one-job modelling the samples now follow.
- **Error.** Not taken: breaks models 0049 promised to keep valid.
- **Leave it (0049 as is).** Not taken: legacy and explicit inbound keep disagreeing, and nothing tells an author why.

## Default if unanswered

Directionless Translate slices keep silently accepting the two-job shape.

## Timeline and scope

Holds until superseded. In scope: the warning, its message, both compilers, editors, docs and samples. Out of scope:
making it an error, an automatic split, changing `PLAY0616`, and any ESM or reference-execution change.

## Verification

**Done when:** both compilers report `PLAY0694` from shared conformance vectors (fires on the shape, including child/nested appends and one warning per reaction for several events; silent with
`direction`, with a `from` route, on a separate Automation slice, for captures in other slices, and for reactions to
other events); docs and samples updated; samples
compile warning-free. **Verify by:** `dotnet test`, `yarn test`.

## Consequences

Authors of legacy models get a nudge toward the split. Ordinary compilation is unaffected; warning-as-error gates on such models fail until the slice is split, which the release note states. Code `PLAY0694` is permanent.

## Related

Decision 0049 (item 4), `PLAY0614`, `PLAY0616`, #480, #760.
