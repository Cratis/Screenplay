// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static string? SourcePath(SyntaxNode node) => node switch
    {
        PathExpressionSyntax value => value.Path,
        ContextExpressionSyntax value when value.Path.StartsWith("command.", StringComparison.Ordinal) => value.Path[8..],
        SourceItemExpressionSyntax value => $"$.{value.Path}",
        _ => null
    };

    IEnumerable<PropertySource> ExpressionSources(ExpressionSyntax expression, WorkspaceSyntaxEntry use) =>
        _entries.Where(entry => ReferenceEquals(entry.Node, expression) || Parents(entry).Any(parent => ReferenceEquals(parent.Node, expression)))
            .Select(entry => (Entry: entry, Path: SourcePath(entry.Node))).Where(item => item.Path is not null)
            .Select(item => new PropertySource(item.Entry, item.Path!, SourceMembers(use, item.Path!), NestedSource(use, item.Path!)));

    bool NestedSource(WorkspaceSyntaxEntry use, string path)
    {
        if (path.StartsWith("$context.command.", StringComparison.Ordinal)) path = path[17..];
        var parts = path.Split('.');
        if (parts.Length == 1) return false;

        return parts.Length > 2 || SourceMembers(use, parts[0]).Length > 0 || SourceMembers(use, path).Length != 1;
    }

    AuthoredDeclaration[] SourceMembers(WorkspaceSyntaxEntry use, string path)
    {
        if (path.StartsWith("$context.command.", StringComparison.Ordinal)) path = path[17..];
        var command = new[] { use }.Concat(Parents(use)).FirstOrDefault(entry => entry.Node is CommandSyntax);
        if (command is not null && Owner(command) is { } declared) return CommandMembers(use, path, command, declared);
        var trigger = Parents(use).Select(entry => entry.Node).OfType<ReactionTriggerSyntax>().FirstOrDefault();
        if (trigger?.Source is NamedTriggerSourceSyntax named) return Members(Resolve(named.Name, ["Event"], use), path);

        return OwnerSourceMembers(use, path);
    }

    AuthoredDeclaration[] CommandMembers(WorkspaceSyntaxEntry use, string path, WorkspaceSyntaxEntry command, AuthoredDeclaration declared)
    {
        var local = Members([declared], path);
        if (local.Length > 0 || !path.Contains('.')) return local;
        var reads = ((CommandSyntax)command.Node).Reads ?? [];
        var parts = path.Split('.', 2);
        var read = reads.Where(read => read.ReadModel == parts[0]).ToArray();

        return read.Length == 1 ? Members(Resolve(read[0].ReadModel, ["ReadModel"], use), parts[1]) : [];
    }

    AuthoredDeclaration[] OwnerSourceMembers(WorkspaceSyntaxEntry use, string path)
    {
        var owner = Owner(use);
        if (owner?.Syntax is ReactionSyntax reaction)
        {
            return Members(reaction.Triggers.Select(clause => clause.Source).OfType<NamedTriggerSourceSyntax>().SelectMany(source => Resolve(source.Name, ["Event"], use)), path);
        }
        if (owner?.Syntax is not PolicySyntax) return [];
        var callers = _index.Incoming(owner).Where(reference => reference.Reference.Role == "authorizes")
            .SelectMany(reference => _index.Declarations.Where(declaration => declaration.Owner == reference.Reference.Owner && declaration.Kind == "Command"));

        return Members(callers, path.Replace("$context.command.", string.Empty, StringComparison.Ordinal));
    }

    sealed record PropertySource(WorkspaceSyntaxEntry Entry, string Path, AuthoredDeclaration[] Candidates, bool Incomplete);
}
