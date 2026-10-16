// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_extending_a_world;

public class with_malformed_facts : for_SemanticEvaluator.when_committing_protected_reads.given.a_protected_command
{
    [Fact] void should_reject_default_facts() => AssertRejected(_evaluator.ExtendWorld(_plan, _world, default));

    [Fact] void should_reject_an_unknown_event_contract() =>
        AssertRejected(_evaluator.ExtendWorld(_plan, _world, [new(SemanticId.Create(SemanticKind.EventContract, "not-in-plan"), SemanticValue.Text("read"), [])]));

    [Fact] void should_reject_missing_event_properties() =>
        AssertRejected(_evaluator.ExtendWorld(_plan, _world, [Fact("Recorded", "read") with { Values = [] }]));

    [Fact] void should_reject_a_fact_without_occurrence_context() =>
        AssertRejected(_evaluator.ExtendWorld(_plan, _world, [Fact("Recorded", "read", "value") with { Context = null }]));

    [Fact]
    void should_reject_a_valid_fact_when_a_later_one_is_malformed_without_extending_anything()
    {
        var result = _evaluator.ExtendWorld(_plan, _world, [Fact("Recorded", "read", "valid"), Fact("Recorded", "read") with { Values = [] }]);
        AssertRejected(result);
        result.World.Facts.ShouldEqual(_world.Facts);
        given.world_comparison.AreEqual(result.World, _world).ShouldBeTrue();
        result.World.ReadModels.ShouldEqual(_world.ReadModels);
    }

    void AssertRejected(SemanticExecutionResult result)
    {
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.World.ShouldBeSame(_world);
    }
}
