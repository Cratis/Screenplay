// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_invocation_records_have_default_inputs : given.a_settlement_with_an_ability
{
    SemanticAbilityInvocationContext _context;
    SemanticPendingAbilityInvocation _pending;
    string _key;
    string _invariantKey;

    void Because()
    {
        var identity = new SemanticAbilityInvocationIdentity(_plan.RunningReactions.Single().Id, 1234L, 1, 2);
        _context = new(_answer.System, "Send", default, identity.Reaction, _input, _occurred, identity);
        _pending = new(identity, 1, 2, _answer.System, "Send", default, _input, _occurred);
        _invariantKey = identity.Key;
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            _key = identity.ToString();
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact] void should_normalize_context_inputs() => _context.Inputs.IsEmpty.ShouldBeTrue();
    [Fact] void should_normalize_pending_inputs() => _pending.Inputs.IsEmpty.ShouldBeTrue();
    [Fact] void should_produce_a_culture_independent_key() => _key.ShouldEqual(_invariantKey);
    [Fact] void should_include_the_identity_components() => _key.ShouldEqual($"{_context.Reaction}/1234/1/2");
}
