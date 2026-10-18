// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_appended_generation_is_not_declared : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Appends.Add(new(new("Registered", 2), 3));
        _snapshot.Edges.Add(new(Register, new("Registered", 2), 3));
    }

    void Because() => _result = Compare();

    [Fact] void should_report_generation_drift() => Finding(ObservationFindingKind.UndeclaredGeneration).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_identify_the_generation() => Finding(ObservationFindingKind.UndeclaredGeneration).Generation.ShouldEqual(2u);
    [Fact] void should_retain_generation_counts_in_the_owning_slice() => _result.Slices.Single().Events[0].GenerationCounts.Length.ShouldEqual(2);
}
