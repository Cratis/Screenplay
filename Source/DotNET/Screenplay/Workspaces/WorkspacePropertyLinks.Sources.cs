// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

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
        if (path.StartsWith("$.", StringComparison.Ordinal)) return path[2..].Contains('.');
        if (LegacyReadSource(use, path)) return true;
        var parts = path.Split('.');
        if (parts.Length == 1) return false;

        return parts.Length > 2 || SourceMembers(use, parts[0]).Length > 0 || SourceMembers(use, path).Length != 1;
    }

    AuthoredDeclaration[] SourceMembers(WorkspaceSyntaxEntry use, string path)
    {
        if (path.StartsWith("$context.command.", StringComparison.Ordinal)) path = path[17..];
        if (path.StartsWith("$.", StringComparison.Ordinal)) return CaptureMembers(use, path[2..]);
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
        var reads = CommandReadReferences.Paths(((CommandSyntax)command.Node).Reads ?? [], path).ToArray();
        if (reads.Length == 0) return QualifiedReadMembers(use, path, (CommandSyntax)command.Node);

        return [.. reads.SelectMany(read => Members(Resolve(read.Read.ReadModel, ["ReadModel"], use), read.Path)).Distinct()];
    }

    AuthoredDeclaration[] OwnerSourceMembers(WorkspaceSyntaxEntry use, string path)
    {
        var owner = Owner(use);
        if (owner?.Syntax is ReactionSyntax reaction)
        {
            return Members(reaction.Triggers.Select(clause => clause.Source).OfType<NamedTriggerSourceSyntax>().SelectMany(source => Resolve(source.Name, ["Event"], use)), path);
        }
        if (owner?.Syntax is not PolicySyntax) return [];
        var callers = PolicyCallers(owner);

        return Members(callers, path.Replace("$context.command.", string.Empty, StringComparison.Ordinal));
    }

    AuthoredDeclaration[] QualifiedReadMembers(WorkspaceSyntaxEntry use, string path, CommandSyntax command)
    {
        var parts = path.Split('.');
        var views = (command.Reads ?? []).SelectMany(read => Resolve(read.ReadModel, ["ReadModel"], use)).ToArray();
        var qualified = Enumerable.Range(1, parts.Length - 1).Reverse()
            .Select(count => (Owners: Resolve(string.Join('.', parts.Take(count)), ["ReadModel"], use).Where(views.Contains).ToArray(), Path: string.Join('.', parts.Skip(count))))
            .FirstOrDefault(prefix => prefix.Owners.Length > 0);

        return qualified.Owners is null ? [] : Members(qualified.Owners, qualified.Path);
    }

    bool LegacyReadSource(WorkspaceSyntaxEntry use, string path)
    {
        var command = Parents(use).Select(entry => entry.Node).OfType<CommandSyntax>().FirstOrDefault();

        return command is not null && CommandReadReferences.Paths(command.Reads ?? [], path).Any();
    }

    AuthoredDeclaration[] CaptureMembers(WorkspaceSyntaxEntry use, string path)
    {
        var capture = Parents(use).Select(entry => entry.Node).OfType<CaptureSyntax>().FirstOrDefault();
        if (capture?.Source is not { } source) return [];
        var events = CaptureEventsSource.Events(source).SelectMany(setting => Resolve(setting.Value, ["Event"], use));

        return Members(events, path);
    }

    sealed record PropertySource(WorkspaceSyntaxEntry Entry, string Path, AuthoredDeclaration[] Candidates, bool Incomplete);
}
