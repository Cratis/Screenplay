// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_edge_is_not_observed;

public class and_the_command_has_a_described_handler : given.a_compiled_model
{
    void Establish()
    {
        Model("module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n      event Audited\n        name String\n      command Register\n        handler\n          implementation\n            hint \"Build the payload\"\n        produces Registered\n        produces Audited\n");
        OnlyAudited();
    }

    void Because() => _result = Compare();

    [Fact] void should_not_assume_handler_execution() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Reason.ShouldEqual(ObservationUnknownReason.DescribedHandler);
    [Fact] void should_leave_the_verdict_unknown() => _result.HasDrift.ShouldBeNull();
}
