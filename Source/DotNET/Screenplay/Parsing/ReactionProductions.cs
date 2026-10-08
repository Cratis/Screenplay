// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static class ReactionProductions
{
    internal static IEnumerable<ProducesSyntax> Refusals(ReactionTriggerSyntax trigger) =>
        (trigger.Invokes ?? []).SelectMany(invocation => invocation.OnRefused).SelectMany(branch => branch.Produces);

    internal static IEnumerable<ProducesSyntax> In(ReactionTriggerSyntax trigger) =>
        (trigger.Produces ?? []).Concat(Refusals(trigger));
}
