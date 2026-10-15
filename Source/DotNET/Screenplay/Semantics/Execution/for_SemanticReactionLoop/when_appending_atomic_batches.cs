// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop;

public class when_appending_atomic_batches : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    [Theory]
    [InlineData(true, 1, false)]
    [InlineData(false, 2, false)]
    [InlineData(false, 2, true)]
    public void should_keep_or_discard_the_prefix_only_as_authored(bool atomic, int count, bool laterVersion)
    {
        Compile(Source(atomic,
            """
            produces Changed
              for "a"
              value = "same"
            produces Changed
              for "b"
              value = "same"
            """));
        if (laterVersion) _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22, _plan.Model.Application)).Plan!;
        var (loop, failure) = Execute();
        var rejected = (SemanticRejected)failure!;
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Constraint);
        rejected.Details.ShouldEqual("Already held");
        loop.World.Facts.Length.ShouldEqual(count);
        loop.World.ReadModels.Length.ShouldEqual(atomic ? 0 : 1);
        if (atomic)
        {
            rejected.Production!.ProductionIndex.ShouldEqual(1);
            rejected.Production.EventContract.ShouldEqual(EventId("Changed"));
            rejected.Production.Route.ShouldBeNull();
            loop.World.PendingChildren.ShouldBeEmpty();
            loop.Facts.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("a", "same", "a", "same", true)]
    [InlineData("a", "first", "b", "second", true)]
    [InlineData("a", "same", "b", "same", false)]
    public void should_reserve_values_per_source(string first, string value, string second, string next, bool accepted)
    {
        Compile(Source(true, $"produces Changed\n  for \"{first}\"\n  value = \"{value}\"\nproduces Changed\n  for \"{second}\"\n  value = \"{next}\""));
        var (loop, failure) = Execute();
        (failure is null).ShouldEqual(accepted);
        loop.World.Facts.Length.ShouldEqual(accepted ? 3 : 1);
    }

    [Fact]
    void should_not_free_a_batch_property_reservation_on_value_change()
    {
        Compile(Source(true,
            """
            produces Changed
              for "a"
              value = "first"
            produces Changed
              for "a"
              value = "second"
            produces Changed
              for "b"
              value = "first"
            """));
        var (loop, failure) = Execute();
        ((SemanticRejected)failure!).Production!.ProductionIndex.ShouldEqual(2);
        loop.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    void should_release_an_event_cycle_before_a_later_claim()
    {
        Compile(Source(true,
            """
            produces Claimed
              for "a"
            produces Released
              for "a"
            produces Claimed
              for "a"
            """));
        var (loop, failure) = Execute();
        failure.ShouldBeNull();
        loop.World.Facts.Length.ShouldEqual(4);
    }

    [Fact]
    void should_reserve_the_whole_expanded_batch_before_adoption()
    {
        Compile(Source(true, "produces Changed\n  for \"a\"\n  value = \"first\"\nproduces Changed\n  for \"a\"\n  value = \"second\""));
        var input = Input();
        var world = SemanticWorld.Create([input], []);
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), _plan, world);
        loop.Observe(Enumerable.Repeat(input, SemanticReactionLoop.MaximumFacts - 1), null).ShouldBeNull();
        var reaction = _plan.Reactions.Single();
        var failure = loop.Fire(reaction, reaction.Triggers[0], new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, input, null);
        failure.ShouldBeOfExactType<SemanticUnsupported>();
        loop.World.ShouldBeSame(world);
        loop.Facts.Length.ShouldEqual(SemanticReactionLoop.MaximumFacts - 1);
    }

    [Fact]
    void should_discard_materialized_candidates_if_a_later_mapping_requires_a_clock()
    {
        var source = Source(true, "produces Changed\n  for \"a\"\n  value = \"first\"\nproduces Ticked\n  for \"a\"\n  at = $context.occurred")
            .Replace("      event Claimed", "      event Ticked\n        at DateTime\n      event Claimed", StringComparison.Ordinal);
        Compile(source);
        var (loop, failure) = Execute();
        ((SemanticRejected)failure!).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        loop.World.Facts.Length.ShouldEqual(1);
        loop.World.ReadModels.ShouldBeEmpty();
        loop.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_report_the_first_production_then_the_ordinal_constraint_name()
    {
        var source = Source(true, "produces Changed\n  for \"a\"\n  value = \"same\"\nproduces Changed\n  for \"b\"\n  value = \"same\"\nproduces Claimed\n  for \"a\"")
            .Replace("      constraint UniqueValue", "      constraint Alpha\n        unique value on Changed\n        message \"First constraint\"\n      constraint UniqueValue", StringComparison.Ordinal);
        Compile(source);
        var (loop, failure) = Execute();
        var rejected = (SemanticRejected)failure!;
        rejected.Code.ShouldEqual("Alpha");
        rejected.Details.ShouldEqual("First constraint");
        rejected.Production!.ProductionIndex.ShouldEqual(1);
        loop.World.Facts.Length.ShouldEqual(1);
    }

    (SemanticReactionLoop Loop, SemanticExecutionResult? Failure) Execute()
    {
        var input = Input();
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), _plan, SemanticWorld.Create([input], []));
        var reaction = _plan.Reactions.Single();
        return (loop, loop.Fire(reaction, reaction.Triggers[0], new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, input, null));
    }

    SemanticId EventId(string name) => _plan.Events.Values.Single(value => value.Name == name).Id;
    SemanticFact Input() => new(EventId("Started"), SemanticValue.Text("cause"), [])
    {
        Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("cause")))
    };

    static string Source(bool atomic, string productions) =>
        (atomic ? "reaction batches atomic\n" : string.Empty) +
        """
        module Billing
          feature Work
            slice Automation Apply
              event Started
              event Changed
                value String
              event Claimed
              event Released
              constraint UniqueValue
                unique value on Changed
                message "Already held"
              constraint OneClaim
                unique event Claimed
                released by Released
              reaction Apply
                when Started
        """ + "\n" + string.Join('\n', productions.Split('\n').Select(line => $"          {line}")) + "\n" +
        """
            slice StateView View
              readmodel Current
                accountId String key
                value String
              projection Current => Current
                from Changed
                  accountId = $eventSourceId
        """;
}
