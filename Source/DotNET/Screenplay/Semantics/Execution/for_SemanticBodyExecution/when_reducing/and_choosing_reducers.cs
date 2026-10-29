// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_choosing_reducers : given.a_reducer
{
    [Fact]
    void should_refuse_an_unresolved_later_reducer_after_an_executable_one()
    {
        Reset();
        Configure(Source + "\n" + """
                  readmodel Other
                    accountId String key
                  reducer OtherReducer => Other
                    on AmountDeposited
                      ```csharp
                      return context.State;
                      ```
            """);
        var first = Bodies[0];
        var result = (SemanticUnsupported)new SemanticEvaluator(Provider, [first]).EstablishWorld(Plan, [Fact()]);
        result.Details.ShouldContain("OtherReducer");
        result.World.ReadModels.ShouldBeEmpty();
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_fold_every_executable_reducer_reached_by_a_fact()
    {
        Reset();
        Configure(Source + "\n" + """
                  readmodel Other
                    accountId String key
                  reducer OtherReducer => Other
                    on AmountDeposited
                      ```csharp
                      return context.State;
                      ```
            """);
        var properties = new Queue<SemanticId>(Plan.ReadModels.Values.OrderBy(model => model.Name == "Balance" ? 0 : 1).Select(model => model.Properties.Single().Id));
        var provider = new reducer_provider(context => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(properties.Dequeue(), context.Key)])));
        Accept(new SemanticEvaluator(provider, Bodies).EstablishWorld(Plan, [Fact()])).World.ReadModels.Length.ShouldEqual(2);
    }

    [Fact]
    void should_keep_mixed_projection_and_reducer_models_refused()
    {
        Reset();
        Configure(Source + "\n" + """
                  readmodel Projected
                    accountId String key
                  projection AlsoProject => Projected
                    from AmountDeposited
            """);
        var model = Plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => !slice.Reducers.IsEmpty);
        var mixed = slice with { Reducers = [slice.Reducers.Single() with { ReadModel = Plan.Projections.Values.Single().ReadModel }] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? mixed : candidate)] }] }] };
        var mixedPlan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        Evaluator.EstablishWorld(mixedPlan, [Fact()]).ShouldBeOfExactType<SemanticUnsupported>();
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_not_invoke_a_reducer_for_a_different_route()
    {
        Reset();
        Configure("eventsource Wanted\n" + Source.Replace("reducer BalanceReducer => Balance", "reducer BalanceReducer => Balance\n        from Wanted", StringComparison.Ordinal));
        var result = Accept(Evaluator.EstablishWorld(Plan, [Fact()]));
        result.World.ReadModels.ShouldBeEmpty();
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_keep_event_target_reducers_refused_by_name()
    {
        Reset();
        var fact = Fact();
        Configure(Source.Replace("slice StateView BalanceView", "slice Translate BalanceView\n      direction outbound", StringComparison.Ordinal)
            .Replace("reducer BalanceReducer => Balance", "public event Published\n        accountId String\n      reducer BalanceReducer => Published", StringComparison.Ordinal));
        var result = (SemanticUnsupported)Evaluator.EstablishWorld(Plan, [fact]);
        result.Details.ShouldContain("BalanceReducer");
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_use_the_default_unsupported_method_on_existing_providers()
    {
        Reset();
        var result = (SemanticUnsupported)new SemanticEvaluator(new predicate_provider(), Bodies).EstablishWorld(Plan, [Fact()]);
        result.Details.ShouldContain("is not supported by this provider");
        result.Details.ShouldContain("requirement");
    }

    sealed class predicate_provider : ISemanticBodyExecutionProvider
    {
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
