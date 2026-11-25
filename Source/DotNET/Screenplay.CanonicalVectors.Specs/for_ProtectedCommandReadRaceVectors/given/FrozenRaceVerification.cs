// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;

internal static class FrozenRaceVerification
{
    internal static void Verify(SemanticExecutionPlan plan, JsonArray vectors, string vectorId)
    {
        var pending = ProtectedReadReplacements.RaceDispositions.Where(disposition => disposition.VectorId == vectorId)
            .ToDictionary(disposition => disposition.CaseName, StringComparer.Ordinal);
        foreach (var vector in vectors)
        {
            var name = vector!["name"]!.GetValue<string>();
            if (pending.Remove(name, out var disposition)) VerifyDisposed(plan, vector, disposition);
            else RaceVectorExecution.Verify(plan, vector);
        }
        pending.ShouldBeEmpty();
    }

    static void VerifyDisposed(SemanticExecutionPlan plan, JsonNode vector, ProtectedReadVectorDisposition disposition)
    {
        var observed = RaceVectorExecution.Run(plan, vector);
        JsonNode.DeepEquals(observed.Dependencies, vector["dependencies"]).ShouldBeTrue();
        var expected = vector["commits"]!.AsArray();
        observed.Commits.Count.ShouldEqual(expected.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            var outcome = expected[index]!["expect"]!;
            if (index == disposition.CommitIndex) VerifyMismatch(observed.Commits[index]!, outcome, disposition);
            else JsonNode.DeepEquals(observed.Commits[index], outcome).ShouldBeTrue();
        }
        Assert.InRange(disposition.CommitIndex!.Value, 0, expected.Count - 1);
    }

    static void VerifyMismatch(JsonNode observed, JsonNode expected, ProtectedReadVectorDisposition disposition)
    {
        expected["message"]!.GetValue<string>().ShouldEqual(disposition.OriginalMessage);
        var actual = observed["message"]!.GetValue<string>();
        string[] failures = actual == disposition.OriginalMessage
            ? []
            : [$"Expected refusal message '{disposition.OriginalMessage}', got '{actual}'."];
        Assert.Equal(disposition.ExpectedFailures.ToArray(), failures);
        JsonNode.DeepEquals(observed, expected).ShouldBeFalse();
        var replacement = expected.DeepClone();
        replacement["message"] = disposition.ReplacementMessage;
        JsonNode.DeepEquals(observed, replacement).ShouldBeTrue();
    }
}
