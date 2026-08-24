// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_the_register_project_specifications : a_valid_semantic_model
{
    SemanticSpecificationRun _rejection;
    SemanticSpecificationRun _success;

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _success = runner.Run(plan, plan.Specifications.Values.Single(_ => _.ThenQueries.Length > 0).Id);
        _rejection = runner.Run(plan, plan.Specifications.Values.Single(_ => _.ThenErrors.Length > 0).Id);
    }

    [Fact] void should_pass_the_success_specification() => _success.Passed.ShouldBeTrue();
    [Fact] void should_report_no_success_failures() => _success.Failures.ShouldBeEmpty();
    [Fact] void should_accept_the_success_specification() => _success.Execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_pass_the_rejection_specification() => _rejection.Passed.ShouldBeTrue();
    [Fact] void should_report_no_rejection_failures() => _rejection.Failures.ShouldBeEmpty();
    [Fact] void should_reject_the_rejection_specification() => _rejection.Execution.ShouldBeOfExactType<SemanticRejected>();
}
