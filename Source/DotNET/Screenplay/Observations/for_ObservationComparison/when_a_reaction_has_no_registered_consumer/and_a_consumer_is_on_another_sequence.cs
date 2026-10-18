// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_has_no_registered_consumer;

public class and_a_consumer_is_on_another_sequence : given.a_compiled_model
{
    void Establish()
    {
        Reaction();
        _snapshot.Consumers = [_snapshot.Consumers[0] with { SequenceId = "other" }];
    }

    void Because() => _result = Compare();

    [Fact] void should_report_sequence_drift() => Finding(ObservationFindingKind.ConsumerOnOtherSequence).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_preserve_the_other_sequence_in_the_slice() => _result.Slices.Single().Consumers.Single().References.Single().SequenceId.ShouldEqual("other");
}
