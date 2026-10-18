---
title: Observation comparison API
description: Compare declared event relationships with bounded host-supplied evidence.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Compare a model with observed registrations and appends

Use `ObservationComparison.Compare` from `Cratis.Screenplay.Observations` in
`Cratis.Screenplay` to find disagreements between a compiled model and a bounded
observation snapshot. A host supplies registrations, counts, producer identities
and subscriptions; Screenplay performs no IO and has no Chronicle dependency.

This comparison answers whether observed event relationships agree with the
model. It does not execute specifications, inspect payloads or prove behavioral
correctness. Use [model comparison](model-comparison.md) for two authored models
and [specifications](specifications.md) for executable examples.

## Inputs

Pass a successful `SemanticCompilation` from a file or composed folder model,
an `ObservationSnapshot`, and optionally an `ObservationIdentityMap`.
`ObservationSnapshot.Create` accepts these required arguments:

| Argument | Evidence |
| --- | --- |
| `eventStore`, `namespace` | Host identifiers for the observed scope |
| `coverage` | One `ObservedSequenceCoverage`: sequence, first/last examined number, captured tail and gaps |
| `registeredEventTypes` | Whole registry of `ObservedEventType(TypeId, Generation)` pairs, not a ranged sample |
| `appends` | `ObservedAppend(Type, Count)` for examined events |
| `edges` | `ObservedEdge(Producer, Type, Events)`, attributed to the innermost named producer |
| `unattributedAppends` | `UnattributedAppend(Type, Count)` without an attributable producer |
| `consumers` | Registered `ObservedConsumer` subscriptions, including other sequences |
| `capturedAt` | Host capture time; no implicit clock read |

Counts mean **events**, never command or reaction invocations. Producers have
kind `Command` or `Reaction` and a host identifier. Consumers have an identifier,
kind (`Reaction`, `Projection`, `Reducer`, `Other`), owner (`Application`,
`Platform`, `Unknown`), sequence, subscribed event type/generation pairs and
optional `NextSequenceNumber` / `LastHandledSequenceNumber` progress.

Creation sorts evidence ordinally and copies it into immutable collections.
Duplicate keys, negative counts, invalid or overlapping ranges, attribution
exceeding append counts and overflowing totals throw `InvalidObservationSnapshot`.
Any append count not accounted for by producer edges remains unattributed even
when the host omits it from `unattributedAppends`. The snapshot has no payload,
event-source-identity or causation-property fields. There is no snapshot JSON API.

## Coverage and verdicts

Sequence numbers are inclusive. `StartsAtFirstEvent`, an examined end equal to
`CapturedTail`, and no `Unexamined` gaps establish full scan coverage. Null first,
last and tail describe an empty sequence, which counts as fully examined.
`OccurredFrom` and `OccurredTo` are informational; the host converts any time
filter to sequence coverage and must disclose skipped ranges.

`ObservationReport.Complete` requires full scan coverage **and no Unknown
findings**. `HasDrift` is true for any known drift, false only for a complete
comparison without drift, and null otherwise. Do not turn null into false.
A known drift remains true even when other evidence is incomplete.

Absence findings become Unknown `PartialCoverage` for a partial scan; the event
registry is the exception because the host supplies it whole. Presence findings
remain drift. “No matching appends within coverage” does not mean “never ran.”

## Findings

Every `ObservationFinding` has a kind, outcome and stable human `Statement`.
Where available it carries a reason, construct identity, typed address, owning
slice, event type and generation, producer, consumer, sequence and count.

| Kind | Outcome |
| --- | --- |
| `EventNotRegistered` | Drift: the current declared generation is missing from the whole registry |
| `EventNotAppended` | Informational for full coverage; Unknown for partial coverage |
| `UndeclaredEventAppended` | Drift, including in a shared namespace |
| `UndeclaredEventRegistered` | Informational when no examined append exists |
| `AppendedWithoutRegistration` | Informational for historical appends |
| `UndeclaredGeneration` | Drift for a generation absent from current and prior model revisions |
| `UndeclaredEdgeObserved` | Drift for a mapped producer outside its declared productions; Unknown for an opaque reaction |
| `DeclaredEdgeNotObserved` | Drift only under the conditions below; otherwise Unknown |
| `NoMatchingAppendsWithinCoverage` | Informational: no attributed event establishes that the producer ran |
| `ConsumerNotRegistered` / `ConsumerOnOtherSequence` | Drift for a missing application reaction subscription; Unknown for partial coverage |
| `ConsumerMismatch` | Drift for a mapped subscription mismatch; Unknown for deferred or incomplete consumer evidence; platform consumers are Excluded |
| `ForeignEvent` | Excluded: the contract has an origin outside this application |
| `NotEstablished` | Unknown: a coverage, attribution, mapping or opaque-behavior limitation without a comparable edge |

A missing production is drift only with full coverage, an unambiguous producer
mapping, another event attributed to that producer, and no unattributed appends
of the expected type. The production must be unconditional; a command must have
no described handler, and a reaction needs an event trigger without a predicate
or opaque requirement. A mapped reaction whose consumer has not reached the tail
reports Unknown `ConsumerBehind`, including when progress is absent.

Other typed reasons are `ConditionalProduction`, `DescribedHandler`,
`OpaqueReaction`, `NonEventTrigger`, `AmbiguousIdentity`, `UnmappedProducer`,
`UnmappedConstruct`, `UnattributedEvents` and `ConsumerKindNotCompared`.
`PlatformConsumer` explains an exclusion. Projection and reducer consumer checks
are deferred; a matching identifier is not enough to establish their semantics.

## Identity mapping

`ObservationIdentityMap.FromModel(compilation.Model)` maps an event by
`StoredName ?? Name`, never by portable `ContractId`. Its current revision is the
observed generation; declared prior revisions are recognized too. A pinned
[event rename](events.md#authoring-metadata) therefore keeps its stored identity.

Commands map by name only when that name is unique across the model. Duplicate
names report `AmbiguousIdentity`. Reactions have no default producer or consumer
mapping: qualified host names and reactor identifiers belong to the render target.
Supply those explicitly with immutable overrides:

```csharp
var map = ObservationIdentityMap.FromModel(compilation.Model)
    .With(reaction.Id,
        producer: new(ObservedProducerKind.Reaction, "follow-host"),
        consumerIdentifier: "follow-host");
var report = ObservationComparison.Compare(compilation, snapshot, map);
```

This excerpt assumes a compiled model, its reaction declaration and a snapshot.
`With` also accepts `eventTypeId` or a command `producer` override. It returns a
new map; omitted arguments leave existing mappings unchanged. Do not guess host
identities from display addresses. Unmapped observed producers report Unknown.
Type-level reaction subscription checks still run without an identity mapping.

## Test an adapter with synthetic evidence

This in-memory example describes a registered event in an empty sequence. It
needs only the Screenplay package and ordinary .NET collection imports:

```csharp
using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Observations;

const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
var workspace = ComparedModel.FromSources("Projects",
    new Dictionary<string, string> { ["application.play"] = source }).Workspace;
var snapshot = ObservationSnapshot.Create(
    "Projects", "tenant", new("log", true, null, null, null, []),
    [new("Registered", 1)], [], [], [], [], DateTimeOffset.UnixEpoch);
var report = ObservationComparison.Compare(workspace.Compilation.Value!, snapshot);
```

Check `workspace.Compilation.Success` before comparing external source. Here
`report.HasDrift` is false and `report.Complete` is true; the only finding is
Informational `EventNotAppended`. Removing the registration instead reports
`EventNotRegistered` drift. Add partial coverage, missing attribution and a
lagging consumer to adapter tests rather than testing only a successful scan.

## Result views and JSON

`Slices` presents owned event generation counts and registration, attributed
producer edges, and consumer subscriptions with progress. Deferred consumers
remain Unknown. Append totals live in `Coverage` and are computed once from the
snapshot, not by summing slices; a second slice producing the same event shows
only its own attributed count, not another copy of the appends.

`ObservationReportJson.Write(report)` writes deterministic JSON with camelCase
properties, string enums, fixed field order and null fields omitted. A null
`HasDrift` is omitted, never serialized as false. Findings carry exactly the same
statements as the typed report. Model-position order comes first, then finding
kind and ordinal observed identities; undeclared evidence comes last.

`NotEstablished` states the remaining limits: no payload agreement, behavioral
correctness, specification execution, external-effect verification, comparison
of other sequences or projection/reducer consumer semantics. JSON is a result
format, not a transport for event contents or a deployment approval.
