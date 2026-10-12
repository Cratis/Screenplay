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
        var indexes = new GenerationIndexes(before, after);
        foreach (var advancement in advancements) AddTargets(indexes, advancement, targets);
        return targets;
    }

    static void AddTargets(GenerationIndexes indexes, EventContractRevisionAdvancement advancement, Dictionary<SemanticAddress, SemanticAddress> targets)
    {
        var owner = CurrentOwner(indexes.Before, advancement.Address);
        if (owner is null) return;
        var successor = Successor(indexes.After, advancement);
        if (successor is null) return;
        foreach (var property in AddressableProperties(indexes.Before, owner)) AddPropertyTarget(indexes.After, successor, property, targets);
    }

    static WorkspaceSyntaxEntry? CurrentOwner(WorkspaceSyntaxIndex index, SemanticAddress address) => index.Entries
        .Where(entry => IsEventOwner(entry, address)).MaxBy(entry => ((EventSyntax)entry.Node).Generation);

    static WorkspaceSyntaxEntry? Successor(WorkspaceSyntaxIndex index, EventContractRevisionAdvancement advancement) => index.Entries.SingleOrDefault(entry => IsEventRevision(entry, advancement));

    static bool IsEventOwner(WorkspaceSyntaxEntry entry, SemanticAddress address) => entry.Address?.Equals(address) == true && entry.Node is EventSyntax;

    static bool IsEventRevision(WorkspaceSyntaxEntry entry, EventContractRevisionAdvancement advancement) => IsEventOwner(entry, advancement.Address) && ((EventSyntax)entry.Node).Generation == advancement.Revision.Value;

    static IEnumerable<WorkspaceSyntaxEntry> AddressableProperties(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry owner) => index.Entries.Where(entry => PropertyOf(entry, owner) && entry.Address is not null);

    static bool PropertyOf(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry owner) => entry.Parent == owner.Handle && entry.Node is PropertySyntax;

    static bool PropertyNamed(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry owner, string name) => PropertyOf(entry, owner) && ((PropertySyntax)entry.Node).Name == name;

    static void AddPropertyTarget(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry successor, WorkspaceSyntaxEntry property, Dictionary<SemanticAddress, SemanticAddress> targets)
    {
        var replacement = index.Entries.SingleOrDefault(entry => PropertyNamed(entry, successor, property.Address!.Name));
        if (replacement?.Address is { } address) targets[property.Address!] = address;
    }

    sealed record GenerationIndexes(WorkspaceSyntaxIndex Before, WorkspaceSyntaxIndex After);
}
