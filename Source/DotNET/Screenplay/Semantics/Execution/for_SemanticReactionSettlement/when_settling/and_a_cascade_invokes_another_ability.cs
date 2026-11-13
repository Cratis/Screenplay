// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_cascade_invokes_another_ability : given.a_settlement_with_an_ability
{
    void Establish() => PrepareAbility(extra: """
                  reaction Finish
                    when Continued
                      invokes Carrier.Send
                        message = "cascade"
                        on returned
                          produces Completed
            """);

    async Task Because() => await SettleAsync();

    [Fact] void should_invoke_for_both_reactions() => _calls.Count.ShouldEqual(2);
    [Fact] void should_assign_the_cascade_fact_its_appended_sequence() => _calls[1].Identity.CauseSequence.ShouldEqual(1L);
    [Fact] void should_keep_the_reaction_identities_distinct() => (_calls[0].Identity.Reaction != _calls[1].Identity.Reaction).ShouldBeTrue();
    [Fact] void should_return_the_whole_cascade() => _accepted.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Continued"), EventId("Completed")]);
}
