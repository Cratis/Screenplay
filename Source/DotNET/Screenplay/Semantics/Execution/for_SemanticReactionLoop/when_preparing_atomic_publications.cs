// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop;

public class when_preparing_atomic_publications : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    const string Source =
        """
        reaction batches atomic
        module M
          feature F
            slice Automation Work
              event Started
              event Changed
                value String
              event Observed
                value String
              reaction Apply
                when Started
                  produces Changed
                    value = "first"
                  produces Changed
                    value = "second"
              reaction Observe
                when Changed
                  value
                  produces Observed
                    value = value
            slice Translate Publication
              direction outbound
              public event Published
                value String
              projection Publisher => Published
                from Changed
                  value = value
        """;

    void Establish() => Compile(Source);

    [Fact]
    void should_publish_primary_facts_then_publications_before_cascades()
    {
        var (loop, failure) = Fire(0);
        failure.ShouldBeNull();
        Names(loop).ShouldEqual(["Changed", "Changed", "Published", "Published"]);
        loop.Settle().ShouldBeNull();
        Names(loop).ShouldEqual(["Changed", "Changed", "Published", "Published", "Observed", "Observed"]);
    }

    [Theory]
    [InlineData(996, true)]
    [InlineData(997, false)]
    void should_reserve_publications_together_with_the_primary_batch(int observed, bool fits)
    {
        var (loop, failure) = Fire(observed);
        (failure is null).ShouldEqual(fits);
        loop.Facts.Length.ShouldEqual(fits ? observed + 4 : observed);
        loop.World.Facts.Length.ShouldEqual(fits ? 5 : 1);
        if (!fits)
        {
            failure.ShouldBeOfExactType<SemanticUnsupported>();
            loop.World.ReadModels.ShouldBeEmpty();
            loop.World.PendingChildren.ShouldBeEmpty();
        }
    }

    (SemanticReactionLoop Loop, SemanticExecutionResult? Failure) Fire(int observed)
    {
        var input = new SemanticFact(_plan.Events.Values.Single(value => value.Name == "Started").Id, SemanticValue.Text("a"), [])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("a")))
        };
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), _plan, SemanticWorld.Create([input], []));
        loop.Observe(Enumerable.Repeat(input, observed), null).ShouldBeNull();
        var reaction = _plan.Reactions.Single(value => value.Name == "Apply");
        return (loop, loop.Fire(reaction, reaction.Triggers[0], new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, input, null));
    }

    string[] Names(SemanticReactionLoop loop) => [.. loop.Facts.Select(fact => _plan.Events[fact.EventContract].Name)];
}
