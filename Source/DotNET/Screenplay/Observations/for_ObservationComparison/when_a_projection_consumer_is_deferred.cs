// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_a_projection_consumer_is_deferred : given.a_compiled_model
{
    void Establish() => Model(Source + "\n    slice StateView Overview\n      readmodel Overview\n        overviewId Uuid\n        name String\n      query GetOverview => Overview optional\n        by overviewId Uuid\n      projection Overview => Overview\n        from Registered\n          overviewId = $eventSourceId\n          name = name\n");
    void Because() => _result = Compare();

    [Fact] void should_not_guess_projection_ownership() => Finding(ObservationFindingKind.ConsumerMismatch).Reason.ShouldEqual(ObservationUnknownReason.ConsumerKindNotCompared);
    [Fact] void should_keep_the_deferred_edge_visible() => _result.Slices[1].Consumers.Single().Reason.ShouldEqual(ObservationUnknownReason.ConsumerKindNotCompared);
    [Fact] void should_not_claim_a_complete_comparison() => _result.Complete.ShouldBeFalse();
}
