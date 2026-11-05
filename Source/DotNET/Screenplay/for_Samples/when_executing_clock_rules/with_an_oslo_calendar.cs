// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.for_Samples.when_executing_clock_rules;

public class with_an_oslo_calendar : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    SemanticSpecificationRun _accepted;
    SemanticSpecificationRun _refused;

    void Because()
    {
        using var stream = typeof(with_an_oslo_calendar).Assembly.GetManifestResourceStream("Cratis.Screenplay.for_Samples.when_executing_clock_rules.clock-rules.play")!;
        using var reader = new StreamReader(stream);
        var bound = Bind(reader.ReadToEnd());
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var compiled = SemanticExecutionPlan.Compile(bound.Value!.Model);
        Assert.NotNull(compiled.Plan);
        var plan = compiled.Plan;
        var runner = new SemanticSpecificationRunner();
        _accepted = runner.Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "AcceptingAFutureDate").Id);
        _refused = runner.Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "RefusingADateAtMidnightInOslo").Id);
    }

    [Fact] void should_accept_a_future_date() => _accepted.Execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_pass_the_acceptance_assertion() => _accepted.Passed.ShouldBeTrue();
    [Fact] void should_refuse_today_in_oslo() => ((SemanticRejected)_refused.Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_pass_the_authored_midnight_error_assertion() => _refused.Passed.ShouldBeTrue();
}
