// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_edge_is_not_observed;

public class and_the_production_is_conditional : given.a_compiled_model
{
    void Establish()
    {
        Model(Source.Replace("produces Registered\n          for projectId\n          name = name", "produces when name == \"first\"\n          Registered\n            for projectId\n            name = name", StringComparison.Ordinal));
        OnlyAudited();
    }

    void Because() => _result = Compare();

    [Fact] void should_not_assume_the_condition_was_satisfied() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Reason.ShouldEqual(ObservationUnknownReason.ConditionalProduction);
    [Fact] void should_leave_the_outcome_unknown() => _result.HasDrift.ShouldBeNull();
}
