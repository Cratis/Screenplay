// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_no_reaction_matches : given.a_settlement
{
    SemanticAccepted _accepted;

    void Establish() => Prepare(
        """
        reaction Finish
          when Continued
            produces Completed
        """);

    void Because() => _accepted = (SemanticAccepted)_settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_accept_without_changing_the_world() => ReferenceEquals(_accepted.World, _world).ShouldBeTrue();
    [Fact] void should_return_no_facts() => _accepted.Facts.ShouldBeEmpty();
}
