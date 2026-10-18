// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_is_mapped;

public class and_another_reaction_maps_to_the_same_consumer : given.a_compiled_model
{
    const string TwoReactions = """
        module Projects
          feature Registration
            slice Automation Follow
              event Registered
                name String
              event Audited
                name String
              event Archived
                name String
              reaction Follow
                when Registered
                  name
                  produces Archived
                    name = name
              reaction Trail
                when Audited
                  name
                  produces Archived
                    name = name
        """;

    void Establish()
    {
        Model(TwoReactions);
        _snapshot.Registered = [Registered, Audited, new("Archived", 1)];
        _snapshot.Edges = [];
        _snapshot.Consumers = [new("shared-host", ObservedConsumerKind.Reaction, ObservedConsumerOwner.Application, "log", [Registered, Audited], 2, 1)];
        _map = _map
            .With(Slice.Reactions[0].Id, consumerIdentifier: "shared-host")
            .With(Slice.Reactions[1].Id, consumerIdentifier: "shared-host");
    }

    void Because() => _result = Compare();

    [Fact] void should_not_report_the_shared_subscription_as_drift() => _result.Findings.Any(value => value.Kind == ObservationFindingKind.ConsumerMismatch && value.Outcome == ObservationOutcome.Drift).ShouldBeFalse();
    [Fact] void should_mark_each_consumer_link_ambiguous() => _result.Slices.Single().Consumers.All(value => value.Reason == ObservationUnknownReason.AmbiguousIdentity).ShouldBeTrue();
}
