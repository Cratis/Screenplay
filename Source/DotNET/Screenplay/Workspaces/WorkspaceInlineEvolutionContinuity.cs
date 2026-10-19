// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

// Proves an extracted inline event's evolution draft against the original workspace: the original
// generation keeps its complete payload and identities, the contract and every route stay, and the
// selected properties are gone only from the new generation and its mappings.
sealed class WorkspaceInlineEvolutionContinuity(WorkspaceSyntaxIndex original, WorkspaceSyntaxEntry owner, WorkspacePropertyRequest staged)
{
    readonly EventSyntax _event = (EventSyntax)owner.Node;

    internal bool Holds(WorkspaceAuthoringResult result)
    {
        var candidate = WorkspaceSyntaxIndex.Create(result.Workspace!);
        var removed = Removed();
        return KeepsContract(result.Workspace!) && KeepsHistoricalGeneration(candidate) && RemovesFromCurrent(candidate, removed) &&
            RemovesMappings(candidate, removed) && original.Find(owner.Parent!) is { } production &&
            WorkspaceProductionRepairs.KeepsDestinations(original, production, result, true, true);
    }

    static (string Name, string Type, bool Collection, bool Optional, bool Identifier, bool Subject) Shape(PropertySyntax property) =>
        (property.Name, property.Type.Name, property.Type.IsCollection, property.Type.IsOptional, property.IsIdentifier, property.IsSubject);

    HashSet<string> Removed() =>
        [.. staged.AlsoRemove.Select(selection => selection.ExpectedName).Prepend(staged.ExpectedName!)];

    bool KeepsContract(ScreenplayWorkspace candidate) =>
        original.Workspace.IdentityCatalog.EventContracts.SingleOrDefault(assignment => assignment.Address.Equals(owner.Address))?.Id ==
        candidate.IdentityCatalog.EventContracts.SingleOrDefault(assignment => assignment.Address.Equals(owner.Address))?.Id;

    WorkspaceSyntaxEntry[] Generation(WorkspaceSyntaxIndex candidate, uint generation) =>
        [.. candidate.Entries.Where(entry => entry.Node is EventSyntax @event && @event.Generation == generation && owner.Address!.Equals(entry.Address))];

    bool KeepsHistoricalGeneration(WorkspaceSyntaxIndex candidate)
    {
        if (Generation(candidate, _event.Generation) is not [var historical]) return false;
        var before = original.Entries.Where(entry => entry.Parent == owner.Handle && entry.Node is PropertySyntax).ToArray();
        var after = candidate.Entries.Where(entry => entry.Parent == historical.Handle && entry.Node is PropertySyntax).ToArray();
        return before.Length == after.Length && before.Zip(after).All(pair =>
            Shape((PropertySyntax)pair.First.Node) == Shape((PropertySyntax)pair.Second.Node) && pair.First.SemanticId == pair.Second.SemanticId);
    }

    bool RemovesFromCurrent(WorkspaceSyntaxIndex candidate, HashSet<string> removed) =>
        Generation(candidate, checked(_event.Generation + 1)) is [{ Node: EventSyntax current }] && !current.Properties.Any(property => removed.Contains(property.Name));

    bool RemovesMappings(WorkspaceSyntaxIndex candidate, HashSet<string> removed)
    {
        if (original.Find(owner.Parent!) is not { Parent: { } command } || original.Find(command) is not { Address: { } address }) return false;
        var commands = candidate.Entries.Where(entry => entry.Node is CommandSyntax && address.Equals(entry.Address)).ToArray();
        return commands is [{ Node: CommandSyntax after }] && after.Produces.Where(produces => produces.Event == _event.Name)
            .All(produces => !produces.Mappings.Any(mapping => removed.Contains(mapping.Property)));
    }
}
