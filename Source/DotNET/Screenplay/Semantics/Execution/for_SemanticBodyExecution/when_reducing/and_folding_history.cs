// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_folding_history : given.a_reducer
{
    [Fact]
    void should_establish_the_first_event_and_answer_a_read_only_query()
    {
        Reset();
        var accepted = Accept(Evaluator.EstablishWorld(Plan, [Fact()]));
        accepted.World.ReadModels.Single().Key.ShouldEqual(Key);
        var context = Contexts.Single();
        context.State.ShouldBeNull();
        context.IsFirst.ShouldBeTrue();
        context.Key.ShouldEqual(Key);
        SemanticValueRules.AreEqual(context.Event, SemanticValue.Composite(Fact().Values)).ShouldBeTrue();
        context.SequenceNumber.ShouldEqual(0UL);
        context.Occurred.ShouldEqual(Fact().Occurred);
        context.Tenant.ShouldBeNull();
        Accept(Evaluator.Execute(Plan, accepted.World, SemanticExecutionRequest.ForQueries(Queries))).Queries.Single().Results.Length.ShouldEqual(1);
    }

    [Fact]
    void should_fold_in_order_with_prior_state()
    {
        Reset();
        Accept(Evaluator.EstablishWorld(Plan, [Fact(), Fact()]));
        Contexts.Count.ShouldEqual(2);
        SemanticValueRules.AreEqual(Contexts[1].State!, State).ShouldBeTrue();
        Contexts[1].IsFirst.ShouldBeFalse();
        Contexts[1].SequenceNumber.ShouldEqual(1UL);
    }

    [Fact]
    void should_extend_with_the_history_position()
    {
        Reset();
        var world = Accept(Evaluator.EstablishWorld(Plan, [Fact()])).World;
        Accept(Evaluator.ExtendWorld(Plan, world, [Fact()]));
        SemanticValueRules.AreEqual(Contexts[1].State!, State).ShouldBeTrue();
        Contexts[1].SequenceNumber.ShouldEqual(1UL);
    }

    [Fact]
    void should_delete_the_instance()
    {
        Reset();
        var world = Accept(Evaluator.EstablishWorld(Plan, [Fact()])).World;
        UseAnswer(SemanticReducerBodyResult.Delete());
        var after = Accept(Evaluator.ExtendWorld(Plan, world, [Fact()]));
        after.World.ReadModels.ShouldBeEmpty();
        Accept(Evaluator.Execute(Plan, after.World, SemanticExecutionRequest.ForQueries(Queries))).Queries.Single().Results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_create_and_dispose_a_factory_provider_for_replay(bool extend)
    {
        Reset();
        var evaluator = new SemanticEvaluator(new reducer_factory(Provider), Bodies);
        Accept(extend ? evaluator.ExtendWorld(Plan, SemanticWorld.Empty, [Fact()]) : evaluator.EstablishWorld(Plan, [Fact()]));
        Contexts.Count.ShouldEqual(1);
        Provider.Disposals.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_fold_command_productions_and_query_them(bool decide)
    {
        Reset();
        var result = decide ? Evaluator.Decide(Plan, SemanticWorld.Empty, Request()).Commit(SemanticWorld.Empty) : Evaluator.Execute(Plan, SemanticWorld.Empty, Request());
        Accept(result).Queries.Single().Results.Length.ShouldEqual(1);
        Contexts.Count.ShouldEqual(1);
    }
}
