// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors;

public class when_executing_every_vector : Specification
{
    static readonly string[] Names =
    [
        "present-state-race-recorded",
        "present-state-race-removal",
        "same-event-other-key",
        "other-view-same-key",
        "merged-keys",
        "no-fact-command-conflict",
        "no-fact-command-accept",
        "irrelevant-fact-preserved",
        "absent-then-created",
        "rejected-decision",
        "late-uniqueness-claim",
        "no-later-facts",
        "repeat-commit-accept-then-conflict",
        "repeat-commit-late-constraint",
        "repeat-commit-stale-generated-source",
        "reconstructed-nonprefix-history"
    ];

    JsonArray _vectors;
    SemanticExecutionPlan _plan;

    void Establish()
    {
        _vectors = JsonNode.Parse(ProtectedCommandReadRaceVectors.Bytes.AsSpan())!["vectors"]!.AsArray();
        _plan = RaceVectorExecution.Plan(SemanticModelSerializer.Deserialize(ProtectedCommandReadRaceVectors.Model.EsmBytes.AsSpan()));
    }

    [Fact] void should_publish_every_named_vector_once_in_order() => _vectors.Select(vector => vector!["name"]!.GetValue<string>()).ShouldEqual(Names);

    [Fact]
    void should_match_every_outcome_through_the_public_calls()
    {
        foreach (var vector in _vectors) RaceVectorExecution.Verify(_plan, vector!);
    }
}
