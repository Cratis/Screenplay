// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Renames a <c>provide</c> declaration together with the <c>$context.&lt;name&gt;</c> reads that resolve to it.
/// </summary>
/// <remarks>
/// A provided value has no semantic identity. The rename target is the outermost declaration of a chain - the application,
/// module, feature or slice level that first provides the name - and the rename rewrites that declaration, every override
/// beneath it and every read the chain resolves. Only the first segment of a read changes. A read the workspace cannot prove
/// bound to exactly this chain refuses the rename instead of guessing.
/// </remarks>
static partial class WorkspaceProvideRefactorings
{
    const string ContextPrefix = "$context.";

    internal static WorkspaceAuthoringResult Rename(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry target, WorkspaceRenameRequest request)
    {
        var expected = request.ExpectedName;
        var replacement = request.NewName;
        if (((ProvideSyntax)target.Node).Name != expected || !WorkspaceRefactoring.Identifier(replacement))
        {
            throw new InvalidWorkspaceAuthoring("The expected name must match exactly and the new name must be one unqualified identifier (letters, digits, underscore).");
        }

        if (index.Entries.Count(entry => entry.Parent is null) != workspace.Documents.Length)
        {
            throw new InvalidWorkspaceAuthoring("Every document must parse before references can be proven safe.");
        }

        if (ProvideValidator.ReservedNames.Contains(replacement) || ContextExpressionSyntax.KnownRoots.Contains(replacement) || ContextPathValidator.ReservedRoots.Contains(replacement))
        {
            throw new InvalidWorkspaceAuthoring($"'{replacement}' is a built-in $context value and cannot be provided - no level can change who a request is. Choose another name.");
        }

        var provides = index.Entries.Where(entry => entry.Node is ProvideSyntax).ToArray();
        var scopes = new Scopes(workspace, index, provides);
        var chain = scopes.RequireChain(target, expected, replacement);
        var reads = Reads(index).ToArray();
        RequireNothingUnprovable(workspace, index, reads, scopes, chain, request);

        var roots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
        var touched = new HashSet<DocumentId>();
        foreach (var entry in chain)
        {
            WorkspaceSyntaxMutation.Set(roots[entry.Handle.Document], $"{entry.Handle.Path}/name", replacement);
            touched.Add(entry.Handle.Document);
        }

        foreach (var read in reads.Where(read => read.Root == expected && scopes.Resolves(read.Entry, chain)))
        {
            WorkspaceSyntaxMutation.Set(roots[read.Entry.Handle.Document], $"{read.Entry.Handle.Path}/{read.Member}", read.Renamed(replacement));
            touched.Add(read.Entry.Handle.Document);
        }

        return new WorkspaceAuthoringTransaction(workspace, new Dictionary<SemanticAddress, SemanticAddress>(), touched).Propose(new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            ReleaseBaseline = request.ReleaseBaseline,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = [.. touched.Select(document => new ReplaceWorkspaceSyntaxDocument(document, WorkspaceSyntaxMutation.Syntax(roots[document])))]
        });
    }

    static IEnumerable<ContextRead> Reads(WorkspaceSyntaxIndex index)
    {
        foreach (var entry in index.Entries)
        {
            switch (entry.Node)
            {
                case ContextExpressionSyntax expression:
                    yield return new(entry, "path", expression.Path, string.Empty);
                    break;
                case ComparisonConditionSyntax comparison when comparison.Left.StartsWith(ContextPrefix, StringComparison.Ordinal):
                    yield return new(entry, "left", comparison.Left[ContextPrefix.Length..], ContextPrefix);
                    break;
            }
        }
    }

    // Everything the rename cannot rewrite or prove has to refuse: text the workspace does not bind, reads outside the supported members,
    // and a new name some read already uses.
    static void RequireNothingUnprovable(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, ContextRead[] reads, Scopes scopes, WorkspaceSyntaxEntry[] chain, WorkspaceRenameRequest request)
    {
        foreach (var entry in index.Entries)
        {
            if (WorkspaceOpaqueText.Named(entry.Node, request.ExpectedName, request.NewName) is { } named)
            {
                throw new InvalidWorkspaceAuthoring($"Cannot prove reference safety through {entry.Kind} at '{Position(workspace, entry)}', which names '{named}'. Use explicit coordinated typed edits; canonical formatting does not make opaque references safe.");
            }

            if (scopes.Resolves(entry, chain) && UnsupportedRead(entry, request.ExpectedName) is { } member)
            {
                throw new InvalidWorkspaceAuthoring($"Cannot rename '{request.ExpectedName}': {entry.Kind} '{member}' at '{Position(workspace, entry)}' reads it as $context.{request.ExpectedName}, which rename does not rewrite. Repair it with a typed edit first.");
            }
        }

        foreach (var read in reads.Where(read => read.Root == request.NewName && scopes.Resolves(read.Entry, chain)))
        {
            throw new InvalidWorkspaceAuthoring($"'{request.NewName}' is already read as $context.{read.Path} at '{Position(workspace, read.Entry)}'; renaming would make that read resolve to the renamed value. Choose another name.");
        }

        var ambiguous = reads.FirstOrDefault(read => read.Root == request.ExpectedName && scopes.IsReusable(read.Entry) && scopes.Chains(request.ExpectedName) > 1);
        if (ambiguous is not null)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot prove a rename: the reusable availability read $context.{ambiguous.Path} at '{Position(workspace, ambiguous.Entry)}' could resolve to any of {scopes.Chains(request.ExpectedName)} separate declarations of '{request.ExpectedName}'. Provide it once, or repair the availability with a typed edit first.");
        }
    }

    static string? UnsupportedRead(WorkspaceSyntaxEntry entry, string name)
    {
        if (entry.Node is CommandCalendarSyntax { ContextMember: { } calendar } && calendar.Split('.')[0] == name)
        {
            return "contextMember";
        }

        if (entry.Node is LiteralExpressionSyntax or ComparisonConditionSyntax)
        {
            return null;
        }

        return entry.Node.GetType().GetProperties()
            .Where(property => property.PropertyType == typeof(string) && property.Name is not ("Description" or "Documentation") && property.GetIndexParameters().Length == 0)
            .FirstOrDefault(property => property.GetValue(entry.Node) is string text && Reads(text, name))?.Name;
    }

    static bool Reads(string text, string name)
    {
        var token = ContextPrefix + name;
        for (var at = text.IndexOf(token, StringComparison.Ordinal); at >= 0; at = text.IndexOf(token, at + 1, StringComparison.Ordinal))
        {
            var end = at + token.Length;
            if (end == text.Length || !(char.IsLetterOrDigit(text[end]) || text[end] == '_'))
            {
                return true;
            }
        }

        return false;
    }

    static string Position(ScreenplayWorkspace workspace, WorkspaceSyntaxEntry entry) =>
        $"{workspace.Documents.Single(document => document.Id == entry.Handle.Document).Path.Value}({entry.Location.Line},{entry.Location.Column})";

    sealed record ContextRead(WorkspaceSyntaxEntry Entry, string Member, string Path, string Prefix)
    {
        internal string Root => Path.Split('.')[0];

        internal string Renamed(string replacement) => $"{Prefix}{replacement}{Path[Root.Length..]}";
    }

    sealed class Scopes
    {
        readonly ScreenplayWorkspace _workspace;
        readonly WorkspaceSyntaxIndex _index;
        readonly WorkspaceSyntaxEntry[] _provides;

        internal Scopes(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry[] provides)
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
            if (_provides.FirstOrDefault(other => other != target && Name(other) == expected && Covers(other, target)) is { } outer)
            {
                throw new InvalidWorkspaceAuthoring($"'{expected}' at '{Position(_workspace, target)}' overrides the value provided at {Level(Container(outer)!)}. Rename the declaration at {Level(Container(outer)!)}; its overrides and reads follow.");
            }

            var container = Container(target)!;
            if (_provides.FirstOrDefault(other => Name(other) == replacement && (container.Node is ApplicationSyntax || Container(other)?.Node is ApplicationSyntax || Covers(other, target) || Covers(target, other))) is { } clash)
            {
                throw new InvalidWorkspaceAuthoring($"'{replacement}' is already provided at {Level(Container(clash)!)}; renaming '{expected}' to it would merge or shadow two different values. Choose another name.");
            }

            var chain = _provides.Where(provide => Name(provide) == expected && (provide == target || Covers(target, provide))).ToArray();
            foreach (var level in chain.Select(provide => Container(provide)!).Where(level => level.Node is not ApplicationSyntax).DistinctBy(level => level.Handle))
            {
                RequireOnePhysicalDeclaration(level);
            }

            return chain;
        }

        static string? Name(WorkspaceSyntaxEntry provide) => ((ProvideSyntax)provide.Node).Name;

        // A provide covers its own level and everything beneath it; an application-level provide covers every document.
        bool Covers(WorkspaceSyntaxEntry provide, WorkspaceSyntaxEntry entry) => Container(provide) is { } level &&
            (level.Node is ApplicationSyntax || level.Handle == entry.Handle || Ancestors(entry).Any(ancestor => ancestor.Handle == level.Handle));

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
            : $"{level.Kind.Replace("Syntax", string.Empty, StringComparison.Ordinal).ToLowerInvariant()} '{WorkspaceReferenceBindings.Name(level.Node)}' ({Position(_workspace, level)})";

        void RequireOnePhysicalDeclaration(WorkspaceSyntaxEntry level)
        {
            var scope = WorkspaceReferenceBindings.Scope(level, _index).Segments.ToArray();
            var name = WorkspaceReferenceBindings.Name(level.Node);
            if (_index.Entries.Count(entry => entry.Node.GetType() == level.Node.GetType() && WorkspaceReferenceBindings.Name(entry.Node) == name && WorkspaceReferenceBindings.Scope(entry, _index).Segments.SequenceEqual(scope)) > 1)
            {
                throw new InvalidWorkspaceAuthoring($"Cannot prove a rename while {Level(level)} is declared in more than one place. Rename requires one physical declaration of the level that provides the value.");
            }
        }
    }
}
