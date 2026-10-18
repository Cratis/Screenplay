// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_is_mapped;

public class and_its_consumer_matches : given.a_compiled_model
{
    void Establish()
    {
        Reaction(mapped: true);
        _snapshot.Edges = [new(new(ObservedProducerKind.Reaction, "follow-host"), Audited, 1)];
        _snapshot.Appends = [new(Audited, 1)];
    }

    void Because() => _result = Compare();

    [Fact] void should_establish_no_drift() => _result.HasDrift.ShouldEqual(false);
    [Fact] void should_be_complete() => _result.Complete.ShouldBeTrue();
    [Fact] void should_show_the_matching_consumer() => _result.Slices.Single().Consumers.Single().Outcome.ShouldEqual(ObservationOutcome.Informational);
}
