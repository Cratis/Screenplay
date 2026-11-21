// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.given;

public class a_pending_invocation : a_settlement_with_an_ability
{
    protected SemanticPendingAbilityInvocation _pending;

    async Task Establish()
    {
        _answer = Answer(SemanticAbilityAnswerKind.Pending);
        await SettleAsync();
        _pending = _accepted.PendingInvocations.Single();
    }
}
