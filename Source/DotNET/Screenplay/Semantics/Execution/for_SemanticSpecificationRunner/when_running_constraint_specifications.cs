// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// Given events are replayed onto the event source under test, so a specification can pin both a violation and a
// re-claim of the event source's own value.
public class when_running_constraint_specifications : Execution.given.a_constrained_plan
{
    SemanticSpecificationRun _violation;
    SemanticSpecificationRun _reclaim;

    void Because()
    {
        var runner = new SemanticSpecificationRunner();
        _violation = runner.Run(_plan, _plan.Specifications.Values.Single(_ => _.Name == "RecordingAPaymentTwice").Id);
        _reclaim = runner.Run(_plan, _plan.Specifications.Values.Single(_ => _.Name == "ReclaimingItsOwnCode").Id);
    }

    [Fact] void should_pass_the_violation_specification() => _violation.Failures.ShouldBeEmpty();
    [Fact] void should_reject_the_violating_command() => ((SemanticRejected)_violation.Execution).Code.ShouldEqual("OnePaymentPerAttempt");
    [Fact] void should_pass_the_reclaim_specification() => _reclaim.Failures.ShouldBeEmpty();
    [Fact] void should_accept_the_reclaiming_command() => _reclaim.Execution.ShouldBeOfExactType<SemanticAccepted>();
}
