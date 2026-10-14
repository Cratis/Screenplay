// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ClockScheduleVectors.given;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ClockScheduleVectors;

public class when_executing_every_vector : Specification
{
    JsonArray _vectors;

    void Establish()
    {
        if (Environment.GetEnvironmentVariable(ClockVectorExecution.RegenerationVariable) == "1") ClockVectorExecution.Regenerate();
        _vectors = JsonNode.Parse(ClockScheduleVectors.Bytes.AsSpan())!["vectors"]!.AsArray();
    }

    [Fact]
    void should_match_every_reference_outcome_in_order()
    {
        _vectors.Count.ShouldEqual(23);
        _vectors.Select(vector => vector!["name"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count().ShouldEqual(_vectors.Count);
        foreach (var vector in _vectors)
        {
            var result = ClockVectorExecution.Run(vector!);
            var name = vector!["name"]!.GetValue<string>();
            if (vector["triggers"] is not null)
            {
                Assert.True(result.Refused == vector["refused"]!.GetValue<bool>(), $"{name}: refusal differs.");
                Assert.True(result.Due.Length == vector["count"]!.GetValue<int>(), $"{name}: occurrence count differs.");
            }
            else
            {
                Assert.False(result.Refused, $"{name}: unexpectedly refused.");
                var expected = vector["due"]!.AsArray().Select(value => value!.GetValue<string>());
                Assert.True(expected.SequenceEqual(result.Due, StringComparer.Ordinal), $"{name}: ordered due instants differ.");
            }
        }
    }
}
