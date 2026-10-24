// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;

// The authoring table the published fixture is generated from. Each commit states the outcome the vector is meant to show, so a
// regeneration that observes anything else fails instead of silently pinning it.
internal static class RaceVectorCases
{
    const string Accepted = "accepted";
    const string Conflict = "conflict";
    const string Validation = "rejected:Validation";
    const string Constraint = "rejected:Constraint";
    const string Contract = "rejected:Contract";
    const string Concurrency = "rejected:Concurrency";
    const string GeneratedSource = "0192f3a4-5b6c-7d8e-9f01-23456789abcd";

    internal static readonly RaceCase[] All =
    [
        new(
            "present-state-race-recorded",
            "A later change to the read key by an event the view handles conflicts.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Conflict, Fact("Recorded", "read", "changed"))]),
        new(
            "present-state-race-removal",
            "A later removal of the read key by an event the view removes with conflicts.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Conflict, Fact("Removed", "read"))]),
        new(
            "same-event-other-key",
            "The same handled event at another key does not conflict; the command settles with its decided value.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Accepted, Fact("Recorded", "write", "changed"))]),
        new(
            "other-view-same-key",
            "An event another view handles at the read key does not conflict.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Accepted, Fact("OtherRecorded", "read", "changed"))]),
        new(
            "merged-keys",
            "Reads of one key by two views merge into one dependency whose event set covers both views.",
            [Fact("Recorded", "read", "original"), Fact("OtherRecorded", "read", "second")],
            new("Merge", [("id", "write"), ("lookup", "read")]),
            [
                Later(Conflict, Fact("Recorded", "read", "changed")),
                Later(Conflict, Fact("OtherRecorded", "read", "changed")),
                Later(Conflict, Fact("OtherRemoved", "read")),
                Later(Accepted, Fact("Recorded", "write", "changed"))
            ]),
        new(
            "no-fact-command-conflict",
            "A command that produces no facts still conflicts on a matching later fact.",
            [Fact("Recorded", "read", "original")],
            new("NoFacts", [("id", "write"), ("lookup", "read")]),
            [Later(Conflict, Fact("Removed", "read"))]),
        new(
            "no-fact-command-accept",
            "A command that produces no facts is accepted without appending when no dependency changed.",
            [Fact("Recorded", "read", "original")],
            new("NoFacts", [("id", "write"), ("lookup", "read")]),
            [Later(Accepted, Fact("OtherRecorded", "read", "other"))]),
        new(
            "irrelevant-fact-preserved",
            "A later fact outside the dependency set stays in the world the command settles against.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Accepted, Fact("Saved", "other", "elsewhere"))]),
        new(
            "absent-then-created",
            "A read of absent state is refused at decision time and stays refused after the state is created.",
            [],
            Update(),
            [Later(Validation, Fact("Recorded", "read", "now present"))]),
        new(
            "rejected-decision",
            "A decision rejected by its own requirement keeps that rejection instead of conflicting.",
            [Fact("Recorded", "read", string.Empty)],
            Update(),
            [Later(Validation, Fact("Recorded", "read", "now valid"))]),
        new(
            "late-uniqueness-claim",
            "A uniqueness claim that lands after the decision is enforced against the later world.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Constraint, Fact("Saved", "competitor", "original"))]),
        new(
            "no-later-facts",
            "A commit against the decided world itself settles the decision.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Accepted)]),
        new(
            "repeat-commit-accept-then-conflict",
            "One decision is accepted against one world, conflicts against a longer one, and is accepted against the first again.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Accepted), Later(Conflict, Fact("Removed", "read")), Later(Accepted)]),
        new(
            "repeat-commit-late-constraint",
            "One decision is accepted against one world, rejected by a constraint against a longer one, and accepted against the first again.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Accepted), Later(Constraint, Fact("Saved", "competitor", "original")), Later(Accepted)]),
        new(
            "repeat-commit-stale-generated-source",
            "A generated event source taken by another writer after the decision is refused as stale; the same decision settles against a world without it.",
            [],
            new("Create", [], [("id", GeneratedSource)]),
            [Later(Accepted), Later(Concurrency, Fact("Created", GeneratedSource, sourceType: "GeneratedId")), Later(Accepted)]),
        new(
            "reconstructed-nonprefix-history",
            "A commit world that is shorter than, or rewrites, the decided history is refused; an equal history rebuilt from the log is accepted.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [History(Contract), History(Contract, Fact("Recorded", "read", "rewritten")), History(Accepted, Fact("Recorded", "read", "original"))])
    ];

    internal static readonly RaceCase[] AllV1R2 =
    [
        .. All,
        new(
            "read-race-and-late-uniqueness-claim",
            "A constraint refusal takes precedence over a protected-read conflict against the same later world.",
            [Fact("Recorded", "read", "original")],
            Update(),
            [Later(Constraint, Fact("Recorded", "read", "changed"), Fact("Saved", "competitor", "original"))])
    ];

    static RaceFact Fact(string @event, string source, string? value = null, string? sourceType = null) => new(@event, source, value, sourceType);

    static RaceRequest Update() => new("Update", [("id", "write"), ("lookup", "read")]);

    static RaceCommit Later(string intended, params RaceFact[] facts) => new(intended, facts, null);

    static RaceCommit History(string intended, params RaceFact[] facts) => new(intended, null, facts);
}

internal sealed record RaceFact(string Event, string Source, string? Value, string? SourceType);

internal sealed record RaceRequest(string Command, (string Name, string Value)[] Values, (string Name, string Value)[]? Generated = null);

internal sealed record RaceCommit(string Intended, RaceFact[]? Later, RaceFact[]? History);

internal sealed record RaceCase(string Name, string Description, RaceFact[] Decided, RaceRequest Request, RaceCommit[] Commits);
