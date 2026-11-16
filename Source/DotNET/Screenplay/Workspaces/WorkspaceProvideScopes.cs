// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Answers which declarations and reads a <c>provide</c> covers, and resolves the chain of declarations a rename affects.
/// </summary>
sealed class WorkspaceProvideScopes
{
    readonly ScreenplayWorkspace _workspace;
    readonly WorkspaceSyntaxIndex _index;
    readonly WorkspaceSyntaxEntry[] _provides;

    internal WorkspaceProvideScopes(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry[] provides)
    {
        _workspace = workspace;
        _index = index;
        _provides = provides;
    }

    internal int Chains(string name) => _provides.Count(provide => Name(provide) == name && !_provides.Any(other => other != provide && Name(other) == name && Covers(other, provide)));

    internal bool IsReusable(WorkspaceSyntaxEntry read) => Ancestors(read).Any(ancestor => ancestor.Node is AvailabilitySyntax);

    // A read resolves to a chain when a provide of that chain covers it; a reusable availability reads whichever chain provides the name anywhere.
    internal bool Resolves(WorkspaceSyntaxEntry read, WorkspaceSyntaxEntry[] chain) =>
        chain.Any(provide => Covers(provide, read)) || IsReusable(read);

    internal WorkspaceSyntaxEntry[] RequireChain(WorkspaceSyntaxEntry target, string expected, string replacement)
    {
        RequireNotOverride(target, expected);
        RequireNoClash(target, expected, replacement);

        var chain = _provides.Where(provide => Name(provide) == expected && (provide == target || Covers(target, provide))).ToArray();
        foreach (var level in chain.Select(provide => Container(provide)!).Where(level => level.Node is not ApplicationSyntax).DistinctBy(level => level.Handle))
        {
            RequireOnePhysicalDeclaration(level);
        }

        return chain;
    }

    static string? Name(WorkspaceSyntaxEntry provide) => ((ProvideSyntax)provide.Node).Name;

    void RequireNotOverride(WorkspaceSyntaxEntry target, string expected)
    {
        if (_provides.FirstOrDefault(other => IsOuterOverride(other, target, expected)) is { } outer)
        {
            var outerLevel = Level(Container(outer)!);
            throw new InvalidWorkspaceAuthoring($"'{expected}' at '{WorkspaceProvideRefactorings.Position(_workspace, target)}' overrides the value provided at {outerLevel}. Rename the declaration at {outerLevel}; its overrides and reads follow.");
        }
    }

    bool IsOuterOverride(WorkspaceSyntaxEntry other, WorkspaceSyntaxEntry target, string expected) =>
        other != target && Name(other) == expected && Covers(other, target);

    void RequireNoClash(WorkspaceSyntaxEntry target, string expected, string replacement)
    {
        var container = Container(target)!;
        if (_provides.FirstOrDefault(other => Name(other) == replacement && Overlaps(other, target, container)) is { } clash)
        {
            throw new InvalidWorkspaceAuthoring($"'{replacement}' is already provided at {Level(Container(clash)!)}; renaming '{expected}' to it would merge or shadow two different values. Choose another name.");
        }
    }

    bool Overlaps(WorkspaceSyntaxEntry provide, WorkspaceSyntaxEntry target, WorkspaceSyntaxEntry targetContainer) =>
        targetContainer.Node is ApplicationSyntax || Container(provide)?.Node is ApplicationSyntax || Covers(provide, target) || Covers(target, provide);

    // A provide covers its own level and everything beneath it; an application-level provide covers every document.
    bool Covers(WorkspaceSyntaxEntry provide, WorkspaceSyntaxEntry entry) => Container(provide) is { } level && IsWithin(level, entry);

    // Modules and features merge across files, so a level covers every fragment that declares the same logical level.
    bool IsWithin(WorkspaceSyntaxEntry level, WorkspaceSyntaxEntry entry) =>
        level.Node is ApplicationSyntax || IsSameLevel(level, entry) || Ancestors(entry).Any(ancestor => IsSameLevel(level, ancestor));

    bool IsSameLevel(WorkspaceSyntaxEntry level, WorkspaceSyntaxEntry entry) =>
        level.Handle == entry.Handle ||
        (WorkspaceReferenceBindings.Name(level.Node) is { } name && IsSameDeclaration(entry, level, name, WorkspaceReferenceBindings.Scope(level, _index).Segments.ToArray()));

    WorkspaceSyntaxEntry? Container(WorkspaceSyntaxEntry provide) => provide.Parent is { } parent ? _index.Find(parent) : null;

    IEnumerable<WorkspaceSyntaxEntry> Ancestors(WorkspaceSyntaxEntry entry)
    {
        for (var current = entry.Parent is { } parent ? _index.Find(parent) : null; current is not null; current = current.Parent is { } next ? _index.Find(next) : null)
        {
            yield return current;
        }
    }

    string Level(WorkspaceSyntaxEntry level) => level.Node is ApplicationSyntax
        ? "the application level"
        : $"{level.Kind.Replace("Syntax", string.Empty, StringComparison.Ordinal).ToLowerInvariant()} '{WorkspaceReferenceBindings.Name(level.Node)}' ({WorkspaceProvideRefactorings.Position(_workspace, level)})";

    bool IsSameDeclaration(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry level, string? name, string[] scope) =>
        entry.Node.GetType() == level.Node.GetType() && WorkspaceReferenceBindings.Name(entry.Node) == name && WorkspaceReferenceBindings.Scope(entry, _index).Segments.SequenceEqual(scope);

    void RequireOnePhysicalDeclaration(WorkspaceSyntaxEntry level)
    {
        var scope = WorkspaceReferenceBindings.Scope(level, _index).Segments.ToArray();
        var name = WorkspaceReferenceBindings.Name(level.Node);
        if (_index.Entries.Count(entry => IsSameDeclaration(entry, level, name, scope)) > 1)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot prove a rename while {Level(level)} is declared in more than one place. Rename requires one physical declaration of the level that provides the value.");
        }
    }
}
