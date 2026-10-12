// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

// Bare references deliberately follow an explicitly advanced contract's current shape. This is
// reference correspondence only: the historical property identities never migrate to the new shape.
static class WorkspaceEventGenerationReferences
{
    internal static Dictionary<SemanticAddress, SemanticAddress> Targets(
        WorkspaceSyntaxIndex before,
        WorkspaceSyntaxIndex after,
        IEnumerable<EventContractRevisionAdvancement> advancements,
        Dictionary<SemanticAddress, SemanticAddress> migrations)
    {
        var targets = new Dictionary<SemanticAddress, SemanticAddress>(migrations);
        foreach (var advancement in advancements)
        {
            var owner = before.Entries.Where(entry => entry.Address?.Equals(advancement.Address) == true && entry.Node is EventSyntax)
                .MaxBy(entry => ((EventSyntax)entry.Node).Generation);
            if (owner is null) continue;
            var successor = after.Entries.SingleOrDefault(entry => entry.Address?.Equals(advancement.Address) == true && entry.Node is EventSyntax @event && @event.Generation == advancement.Revision.Value);
            if (successor is null) continue;
            foreach (var property in before.Entries.Where(entry => entry.Parent == owner.Handle && entry.Node is PropertySyntax && entry.Address is not null))
            {
                var replacement = after.Entries.SingleOrDefault(entry => entry.Parent == successor.Handle && entry.Node is PropertySyntax shape && shape.Name == property.Address!.Name);
                if (replacement?.Address is { } address) targets[property.Address!] = address;
            }
        }
        return targets;
    }
}
