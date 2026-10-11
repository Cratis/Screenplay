// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_no_facts_were_appended : given.a_settlement
{
    SemanticAccepted _accepted;

    void Establish() => Prepare(
        """
        reaction Continue
          when Started
            produces Continued
        """);

    void Because() => _accepted = (SemanticAccepted)_settlement.Settle(_plan, _world, [], null);

    [Fact] void should_not_reobserve_existing_history() => ReferenceEquals(_accepted.World, _world).ShouldBeTrue();
    [Fact] void should_return_no_facts() => _accepted.Facts.ShouldBeEmpty();
}
