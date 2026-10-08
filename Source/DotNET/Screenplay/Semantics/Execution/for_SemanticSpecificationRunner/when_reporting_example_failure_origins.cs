// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_reporting_example_failure_origins : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        example Input : Record
          amount = 10
        example ExpectedFact : Recorded
          amount = 30
        module M
          feature F
            slice StateChange Recording
              command Record
                id String identifier
                amount Int
                produces Recorded
                  for id
                  amount = amount
              event Recorded
                amount Int
              specification Mismatch
                when Input
                  id = "key"
                then ExpectedFact amount = 40
        """;

    SemanticCompilation _compilation;
    SemanticSpecificationRun _run;
    SemanticSpecificationRun _withoutSource;

    void Establish() => _compilation = Bind(Source).Value!;
    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
        var id = plan.Specifications.Values.Single().Id;
        var runner = new SemanticSpecificationRunner();
        _run = runner.Run(_compilation, id);
        _withoutSource = runner.Run(plan, id);
    }

    [Fact] void should_keep_the_mismatch_failed() => _run.Passed.ShouldBeFalse();
    [Fact] void should_show_the_inherited_input() => _run.Failures.Single().ShouldContain("when Input: amount = 10 (example)");
    [Fact] void should_show_the_authored_input() => _run.Failures.Single().ShouldContain("when Input: id = \"key\" (authored)");
    [Fact] void should_show_the_override_and_replaced_value() => _run.Failures.Single().ShouldContain("then ExpectedFact: amount = 40 (override, replaces 30)");
    [Fact] void should_preserve_the_underlying_comparison_failure() => _run.Failures.Single().StartsWith(_withoutSource.Failures.Single(), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_invent_origins_for_an_esm_only_plan() => _withoutSource.Failures.Single().ShouldNotContain("Effective fixtures:");
    [Fact] void should_execute_rather_than_return_unsupported() => _run.Execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_keep_execution_unchanged() => _run.Execution.Kind.ShouldEqual(_withoutSource.Execution.Kind);
    [Fact] void should_keep_produced_values_unchanged() => ((SemanticAccepted)_run.Execution).Facts.SelectMany(fact => fact.Values).Select(value => value.Value).ShouldContainOnly(((SemanticAccepted)_withoutSource.Execution).Facts.SelectMany(fact => fact.Values).Select(value => value.Value));

    [Fact]
    void should_refuse_an_unknown_specification_without_reusing_another_sidecar()
    {
        var run = new SemanticSpecificationRunner().Run(_compilation, SemanticId.Create(SemanticKind.Specification, "unknown-specification"));
        run.Passed.ShouldBeFalse();
        run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        run.Failures.Single().ShouldNotContain("Effective fixtures:");
    }

    [Fact]
    void should_leave_plain_source_failure_messages_unchanged()
    {
        var compilation = Bind(Source.Replace("when Input", "when Record amount = 10", StringComparison.Ordinal).Replace("then ExpectedFact amount = 40", "then Recorded amount = 40", StringComparison.Ordinal)).Value!;
        var plan = SemanticExecutionPlan.Compile(compilation.Model).Plan!;
        var id = plan.Specifications.Values.Single().Id;
        new SemanticSpecificationRunner().Run(compilation, id).Failures.ShouldContainOnly(new SemanticSpecificationRunner().Run(plan, id).Failures);
    }
}
