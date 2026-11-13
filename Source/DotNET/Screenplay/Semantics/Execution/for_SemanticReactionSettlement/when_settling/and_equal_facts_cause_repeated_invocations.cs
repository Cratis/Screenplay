// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_equal_facts_cause_repeated_invocations : given.a_settlement_with_an_ability
{
    SemanticAbilityInvocationIdentity[] _first;
    SemanticAbilityInvocationIdentity[] _retry;
    SemanticFact[] _firstFacts;
    SemanticFact[] _retryFacts;

    void Establish()
    {
        PrepareAbility(additionalInvocation: """
                      invokes Carrier.Send
                        message = "again"
                        on returned
                          produces Completed
            """);
        _world = SemanticWorld.Create([_input, _input], []);
    }

    async Task Because()
    {
        var first = (SemanticAccepted)await _settlement.SettleAsync(_plan, _world, [_input, _input], _occurred);
        _firstFacts = first.Facts.ToArray();
        _first = _calls.Select(call => call.Identity).ToArray();
        _calls.Clear();
        var retry = (SemanticAccepted)await _settlement.SettleAsync(_plan, _world, [_input, _input], _occurred);
        _retryFacts = retry.Facts.ToArray();
        _retry = _calls.Select(call => call.Identity).ToArray();
    }

    [Fact] void should_distinguish_equal_causes_by_sequence() => _first.Select(identity => identity.CauseSequence).ShouldEqual([0L, 0L, 1L, 1L]);
    [Fact] void should_distinguish_invocations_by_index() => _first.Select(identity => identity.Index).ShouldEqual([0, 1, 0, 1]);
    [Fact] void should_have_unique_keys() => _first.Select(identity => identity.Key).Distinct().Count().ShouldEqual(4);
    [Fact] void should_reproduce_identities_on_retry() => _retry.AsEnumerable().ShouldEqual(_first);
    [Fact] void should_reproduce_facts_on_retry() => _retryFacts.AsEnumerable().ShouldEqual(_firstFacts);
}
