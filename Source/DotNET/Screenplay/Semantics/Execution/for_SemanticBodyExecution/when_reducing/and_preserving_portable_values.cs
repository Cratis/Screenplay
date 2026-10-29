// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_preserving_portable_values : given.a_reducer
{
    [Fact]
    void should_format_numeric_keys_as_invariant_text_but_store_the_numeric_identity()
    {
        Reset();
        Configure(Source.Replace("String", "Int", StringComparison.Ordinal));
        UseKey(SemanticValue.Number(42));
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var world = Accept(Evaluator.EstablishWorld(Plan, [Fact()])).World;
            Contexts.Single().Key.ShouldEqual(SemanticValue.Text("42"));
            world.ReadModels.Single().Key.ShouldEqual(SemanticValue.Number(42));
            Accept(Evaluator.Execute(Plan, world, SemanticExecutionRequest.ForQueries(Queries))).Queries.Single().Results.Length.ShouldEqual(1);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_unknown_circumstances_null_and_refuse_a_null_identity(bool extend)
    {
        Reset();
        Accept(Evaluator.EstablishWorld(Plan, [Fact() with { Occurred = null }]));
        Contexts.Single().Occurred.ShouldBeNull();
        Contexts.Single().Tenant.ShouldBeNull();
        var fact = Fact() with { Destination = SemanticValue.Null, Context = null };
        var result = extend ? Evaluator.ExtendWorld(Plan, SemanticWorld.Empty, [fact]) : Evaluator.EstablishWorld(Plan, [fact]);
        result.ShouldBeOfExactType<SemanticUnsupported>();
        Contexts.Count.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_an_unusable_reducer_identity_before_history_contract_validation(bool extend)
    {
        Reset();
        var fact = Fact() with { Destination = SemanticValue.Null, Context = null, Values = [] };
        var baseline = extend ? new SemanticEvaluator().ExtendWorld(Plan, SemanticWorld.Empty, [fact]) : new SemanticEvaluator().EstablishWorld(Plan, [fact]);
        baseline.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)baseline).Details.ShouldEqual("Fact occurrence metadata is malformed.");
        var result = extend ? Evaluator.ExtendWorld(Plan, SemanticWorld.Empty, [fact]) : Evaluator.EstablishWorld(Plan, [fact]);
        result.ShouldBeOfExactType<SemanticUnsupported>();
        ((SemanticUnsupported)result).Details.ShouldContain("The event source key must be a non-null scalar identity.");
        result.World.ShouldBeSame(SemanticWorld.Empty);
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_materialize_optional_properties_and_order_them_by_the_declaration()
    {
        Reset();
        Configure(Source.Replace("accountId String key", "accountId String key\n        label String optional", StringComparison.Ordinal));
        var properties = ReadModel.Properties;
        var provider = new reducer_provider(_ => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(properties[0].Id, Key)])));
        var first = Accept(new SemanticEvaluator(provider, Bodies).EstablishWorld(Plan, [Fact()])).World.ReadModels.Single();
        first.Values.Select(value => value.TargetProperty).ShouldEqual(properties.Select(property => property.Id));
        first.Values[1].Value.ShouldEqual(SemanticValue.Null);
        provider = new(_ => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(properties[1].Id, SemanticValue.Text("label")), new(properties[0].Id, Key)])));
        var second = Accept(new SemanticEvaluator(provider, Bodies).EstablishWorld(Plan, [Fact()])).World.ReadModels.Single();
        second.Values.Select(value => value.TargetProperty).ShouldEqual(properties.Select(property => property.Id));
    }

    [Fact]
    void should_validate_nested_composites_and_collection_elements()
    {
        Reset();
        Configure("type Details\n  names String[]\n" + Source.Replace("accountId String key", "accountId String key\n        details Details", StringComparison.Ordinal));
        var nested = Plan.Model.Application.Types.Single();
        var properties = ReadModel.Properties;
        var invalid = SemanticValue.Composite([new(nested.Properties.Single().Id, SemanticValue.Array([SemanticValue.Number(1)]))]);
        var provider = new reducer_provider(_ => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(properties[0].Id, Key), new(properties[1].Id, invalid)])));
        new SemanticEvaluator(provider, Bodies).EstablishWorld(Plan, [Fact()]).ShouldBeOfExactType<SemanticUnsupported>();
    }

    [Fact]
    void should_start_over_after_deletion_and_keep_keys_isolated()
    {
        Reset();
        var world = Accept(Evaluator.EstablishWorld(Plan, [Fact()])).World;
        UseKey(SemanticValue.Text("second"));
        world = Accept(Evaluator.ExtendWorld(Plan, world, [Fact()])).World;
        world.ReadModels.Length.ShouldEqual(2);
        Contexts[1].IsFirst.ShouldBeTrue();
        UseAnswer(SemanticReducerBodyResult.Delete());
        world = Accept(Evaluator.ExtendWorld(Plan, world, [Fact()])).World;
        world.ReadModels.Single().Key.ShouldEqual(SemanticValue.Text("first"));
        UseAnswer(null);
        Accept(Evaluator.ExtendWorld(Plan, world, [Fact()]));
        Contexts[^1].IsFirst.ShouldBeTrue();
    }

    [Fact]
    void should_fold_two_event_types_in_history_order()
    {
        Reset();
        Configure(Source.Replace("    slice StateView", "      command Adjust\n        accountId String identifier\n        produces Adjusted\n          for accountId\n          accountId = accountId\n      event Adjusted\n        accountId String\n    slice StateView", StringComparison.Ordinal) + "\n" + """
                    on Adjusted
                      ```csharp
                      return context.State;
                      ```
            """);
        var events = Plan.Events.Values.OrderBy(contract => contract.Name == "AmountDeposited" ? 0 : 1).ToArray();
        var context = new SemanticEventContext(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), Key));
        var facts = events.Select(contract => new SemanticFact(contract.Id, Key, [new(contract.Properties.Single().Id, Key)]) { Context = context }).ToImmutableArray();
        Accept(Evaluator.EstablishWorld(Plan, facts));
        Contexts.Count.ShouldEqual(2);
        Contexts[1].IsFirst.ShouldBeFalse();
    }
}
