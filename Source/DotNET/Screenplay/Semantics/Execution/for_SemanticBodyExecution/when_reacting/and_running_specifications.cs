// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_running_specifications : given.a_reaction_effect
{
    SemanticSpecificationRun _run;
    void Because() => _run = new SemanticSpecificationRunner(new SemanticEvaluator(Provider, Bodies)).Run(Plan, Plan.Specifications.Values.Single().Id);
    [Fact] void should_satisfy_the_specification() => _run.Failures.ShouldBeEmpty();
    [Fact] void should_pass_the_given_clock() => Context.Occurred.ShouldEqual(Occurred);
    [Fact] void should_leave_caller_owned_disposal_to_the_host() => Provider.Disposals.ShouldEqual(0);
}
