// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_extending_a_world;

public class with_an_accepted_suffix : for_SemanticEvaluator.when_committing_protected_reads.given.a_protected_command
{
    SemanticFact[] History() =>
    [
        Fact("Recorded", "read", "first"),
        Fact("OtherRecorded", "read", "second"),
        Fact("Removed", "read"),
        Fact("Recorded", "other", "third")
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    void should_equal_the_world_established_from_the_whole_history(int split)
    {
        var history = History();
        var prefix = World(history[..split]);
        var extended = (SemanticAccepted)_evaluator.ExtendWorld(_plan, prefix, [.. history[split..]]);
        var expected = _evaluator.EstablishWorld(_plan, [.. history]).World;
        given.world_comparison.AreEqual(extended.World, expected).ShouldBeTrue();
    }

    [Fact]
    void should_report_only_the_extension_facts()
    {
        var history = History();
        var extended = (SemanticAccepted)_evaluator.ExtendWorld(_plan, World(history[..2]), [.. history[2..]]);
        extended.Facts.Length.ShouldEqual(2);
        extended.World.Facts.Length.ShouldEqual(4);
    }

    [Fact]
    void should_keep_the_prefix_facts_by_reference_and_leave_the_input_world_unchanged()
    {
        var prefix = World(History()[..2]);
        var prefixFacts = prefix.Facts;
        var extended = _evaluator.ExtendWorld(_plan, prefix, [History()[2]]);
        extended.World.Facts[0].ShouldBeSame(prefixFacts[0]);
        extended.World.Facts[1].ShouldBeSame(prefixFacts[1]);
        prefix.Facts.Length.ShouldEqual(2);
    }

    [Fact]
    void should_accept_no_facts_as_the_same_world_state()
    {
        var extended = (SemanticAccepted)_evaluator.ExtendWorld(_plan, _world, []);
        extended.Facts.ShouldBeEmpty();
        given.world_comparison.AreEqual(extended.World, _world).ShouldBeTrue();
    }
}
