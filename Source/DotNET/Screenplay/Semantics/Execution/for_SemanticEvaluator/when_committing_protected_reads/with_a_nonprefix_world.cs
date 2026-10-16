// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_nonprefix_world : given.a_protected_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reject_shorter_or_rewritten_history(bool rewritten)
    {
        var decision = _evaluator.Decide(_plan, _world, Request());
        var latest = rewritten ? World(Fact("Recorded", "read", "rewritten")) : SemanticWorld.Empty;
        var result = (SemanticRejected)decision.Commit(latest);
        result.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.World.ShouldBeSame(latest);
    }

    [Fact]
    void should_admit_an_equivalent_reconstructed_prefix()
    {
        var decision = _evaluator.Decide(_plan, _world, Request());
        decision.Commit(World(Fact("Recorded", "read", "original"))).ShouldBeOfExactType<SemanticAccepted>();
    }
}
