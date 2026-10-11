// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static string? InteractionCommand(SyntaxNode node) => node switch
    {
        ExecuteCommandActionSyntax execute => execute.Command,
        ScreenActionAlternativeSyntax alternative => alternative.Command,
        ScreenActionOtherwiseSyntax otherwise => otherwise.Command,
        _ => null
    };

    void IndexInteractions()
    {
        foreach (var entry in _entries.Where(entry => entry.Node is InteractionArgumentSyntax)) IndexInteractionArgument(entry);
    }

    void IndexInteractionArgument(WorkspaceSyntaxEntry entry)
    {
        var argument = (InteractionArgumentSyntax)entry.Node;
        var action = Parents(entry).FirstOrDefault(parent => parent.Node is InteractionActionSyntax or ScreenActionAlternativeSyntax or ScreenActionOtherwiseSyntax);
        var command = action is null ? null : InteractionCommand(action.Node);
        if (command is null) return;
        var owners = InteractionOwners(command, entry);
        var targets = owners.Length == 0 ? UnresolvedInteractionTargets(argument.Name) : Members(owners, argument.Name);
        var sources = SourceMembers(entry, argument.Binding);
        var sourceIncomplete = NestedSource(entry, argument.Binding) || sources.Length != 1;
        var targetIncomplete = owners.Length != 1 || targets.Length != 1;
        var incomplete = targetIncomplete || sourceIncomplete;
        Add(entry, "mapping-target", new(argument.Name, targets, sources is [var source] ? source : null, incomplete));
        IndexMappingSource(new(entry, argument.Binding, sources, sourceIncomplete), targets, targetIncomplete);
    }

    AuthoredDeclaration[] InteractionOwners(string command, WorkspaceSyntaxEntry entry)
    {
        var behavior = Parents(entry).Select(parent => parent.Node).OfType<BehaviorSyntax>().FirstOrDefault();
        if (behavior?.Parameters.Any(parameter => parameter.Name == command) is true) return [];
        var commands = _index.Declarations.Where(declaration => declaration.Syntax is CommandSyntax).ToArray();
        var candidates = commands.Select(declaration => new Declaration(declaration.Name, new(declaration.Scope))).ToArray();

        // Reuse the compiler's guarded-action/interaction resolution, including nearest scope and qualification.
        var resolution = ReferenceResolver.Resolve(command, new(Owner(entry)?.Scope ?? []), candidates);
        if (resolution.Resolved is not { } resolved) return [];

        return [.. commands.Where(declaration => declaration.Name == resolved.Name && declaration.Scope.SequenceEqual(resolved.Scope.Segments))];
    }

    AuthoredDeclaration[] UnresolvedInteractionTargets(string name) => Members(_properties.Keys.Where(owner => owner.Syntax is CommandSyntax), name);
}
