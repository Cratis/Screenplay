// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_a_prior_declared_generation_was_appended : given.a_compiled_model
{
    void Establish()
    {
        Model(Source.Replace("event Registered\n        name String", "event Registered generation 1\n        name String\n      event Registered generation 2\n        name String", StringComparison.Ordinal));
        _snapshot.Registered.Add(new("Registered", 2));
    }

    void Because() => _result = Compare();

    [Fact] void should_recognize_the_declared_history() => _result.Findings.Any(value => value.Kind == ObservationFindingKind.UndeclaredGeneration).ShouldBeFalse();
    [Fact] void should_keep_the_prior_generation_count() => _result.Slices.Single().Events[0].GenerationCounts.Single().Type.Generation.ShouldEqual(1u);
    [Fact] void should_establish_no_drift() => _result.HasDrift.ShouldEqual(false);
}
