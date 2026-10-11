// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_observed_batch_uses_the_fact_budget : given.a_settlement
{
    SemanticUnsupported _unsupported;

    void Establish()
    {
        Prepare(
            """
            reaction Continue
              when Started
                produces Continued
            """);
        _world = SemanticWorld.Create([.. Enumerable.Repeat(_input, SemanticReactionLoop.MaximumFacts)], []);
    }

    void Because() => _unsupported = (SemanticUnsupported)_settlement.Settle(_plan, _world, _world.Facts, null);

    [Fact] void should_count_the_input_facts_toward_the_cascade_limit() => _unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
    [Fact] void should_keep_the_input_world() => ReferenceEquals(_unsupported.World, _world).ShouldBeTrue();
}
