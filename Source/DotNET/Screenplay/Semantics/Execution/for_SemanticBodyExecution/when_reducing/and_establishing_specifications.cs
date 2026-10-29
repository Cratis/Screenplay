// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_establishing_specifications : given.a_reducer
{
    const string GivenHistory = """
              specification GivenHistory
                given AmountDeposited
                  for "first"
                  accountId = "first"
                when Touch
                  accountId = "first"
                then AccountTouched
        """;
    const string UnrelatedAction = """
              command Touch
                accountId String identifier
                produces AccountTouched
                  for accountId
              event AccountTouched
        """;

    [Theory]
    [InlineData("unresolved")]
    [InlineData("mixed")]
    [InlineData("event-target")]
    void should_preserve_the_no_provider_outcome_without_a_reducer_state_assertion(string kind)
    {
        ConfigureSpecification(kind, false);
        var baseline = new SemanticSpecificationRunner().Run(Plan, SpecificationId);
        var actual = Run(kind);
        ShouldMatchBaseline(actual, baseline);
        Contexts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("unresolved")]
    [InlineData("mixed")]
    void should_preserve_the_refusal_when_unexecutable_reducer_state_is_asserted(string kind)
    {
        ConfigureSpecification(kind, true);
        var baseline = new SemanticSpecificationRunner().Run(Plan, SpecificationId);
        baseline.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        ((SemanticUnsupported)baseline.Execution).Details.ShouldEqual("Reducer 'BalanceReducer' has opaque transitions and requires a target provider to compute read-model state.");
        ShouldMatchBaseline(Run(kind), baseline);
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_fold_executable_reducers_while_establishing_given_history_without_a_state_assertion()
    {
        ConfigureSpecification("executable", false);
        var run = Run("executable");
        run.Passed.ShouldBeTrue();
        var instance = run.Execution.World.ReadModels.Single();
        SemanticValueRules.AreEqual(SemanticValue.Composite(instance.Values), State).ShouldBeTrue();
        Contexts.Single().SequenceNumber.ShouldEqual(0UL);
    }

    SemanticId SpecificationId => Plan.Specifications.Values.Single().Id;

    SemanticSpecificationRun Run(string kind) =>
        new SemanticSpecificationRunner(new SemanticEvaluator(Provider, kind == "unresolved" ? [] : Bodies)).Run(Plan, SpecificationId);

    static void ShouldMatchBaseline(SemanticSpecificationRun actual, SemanticSpecificationRun baseline)
    {
        actual.Passed.ShouldEqual(baseline.Passed);
        actual.Failures.SequenceEqual(baseline.Failures).ShouldBeTrue();
        actual.Execution.Kind.ShouldEqual(baseline.Execution.Kind);
        for_SemanticEvaluator.when_extending_a_world.given.world_comparison.AreEqual(actual.Execution.World, baseline.Execution.World).ShouldBeTrue();
    }

    void ConfigureSpecification(string kind, bool assertsState)
    {
        Reset();
        var source = Source.Replace("    slice StateView", UnrelatedAction + "\n    slice StateView", StringComparison.Ordinal);
        source = kind == "event-target"
            ? source.Replace("slice StateView BalanceView", "slice Translate BalanceView\n      direction outbound", StringComparison.Ordinal)
                .Replace("reducer BalanceReducer => Balance", "public event Published\n        accountId String\n      reducer BalanceReducer => Published", StringComparison.Ordinal)
            : source;
        if (kind == "mixed")
        {
            source += "\n" + """
                      readmodel Projected
                        accountId String key
                      projection AlsoProject => Projected
                        from AmountDeposited
                """;
        }
        var assertion = assertsState ? $"\n        then readmodel {(kind == "mixed" ? "Projected" : "Balance")}\n          accountId = \"first\"" : string.Empty;
        var slice = kind == "event-target" ? "    slice Translate BalanceView" : "    slice StateView BalanceView";
        Configure(source.Replace(slice, GivenHistory + assertion + "\n" + slice, StringComparison.Ordinal));
        if (kind == "mixed") RetargetReducer();
    }

    void RetargetReducer()
    {
        var model = Plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => !slice.Reducers.IsEmpty);
        var mixed = slice with { Reducers = [slice.Reducers.Single() with { ReadModel = Plan.Projections.Values.Single().ReadModel }] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? mixed : candidate)] }] }] };
        Plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }
}
