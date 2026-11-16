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
static class WorkspaceProvideRefactorings
{
    internal static WorkspaceAuthoringResult Rename(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry target, WorkspaceRenameRequest request)
    {
        RequireRenameable(workspace, index, target, request);

        var provides = index.Entries.Where(entry => entry.Node is ProvideSyntax).ToArray();
        var scopes = new WorkspaceProvideScopes(workspace, index, provides);
        var chain = scopes.RequireChain(target, request.ExpectedName, request.NewName);
        var reads = WorkspaceProvideContextRead.In(index).ToArray();
        new WorkspaceProvideRenameGuard(workspace, scopes, chain, request).RequireNothingUnprovable(index, reads);

        var resolved = reads.Where(read => read.Root == request.ExpectedName && scopes.Resolves(read.Entry, chain)).ToArray();
        return Rewrite(workspace, index, request, new RewritePlan(chain, resolved));
    }

    internal static string Position(ScreenplayWorkspace workspace, WorkspaceSyntaxEntry entry) =>
        $"{workspace.Documents.Single(document => document.Id == entry.Handle.Document).Path.Value}({entry.Location.Line},{entry.Location.Column})";

    static void RequireRenameable(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry target, WorkspaceRenameRequest request)
    {
        var replacement = request.NewName;
        if (((ProvideSyntax)target.Node).Name != request.ExpectedName || !WorkspaceRefactoring.Identifier(replacement))
        {
            throw new InvalidWorkspaceAuthoring("The expected name must match exactly and the new name must be one unqualified identifier (letters, digits, underscore).");
        }

        if (index.Entries.Count(entry => entry.Parent is null) != workspace.Documents.Length)
        {
            throw new InvalidWorkspaceAuthoring("Every document must parse before references can be proven safe.");
        }

        if (IsBuiltIn(replacement))
        {
            throw new InvalidWorkspaceAuthoring($"'{replacement}' is a built-in $context value and cannot be provided - no level can change who a request is. Choose another name.");
        }
    }

    static bool IsBuiltIn(string name) =>
        ProvideValidator.ReservedNames.Contains(name) || ContextExpressionSyntax.KnownRoots.Contains(name) || ContextPathValidator.ReservedRoots.Contains(name);

    static WorkspaceAuthoringResult Rewrite(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceRenameRequest request, RewritePlan plan)
    {
        var replacement = request.NewName;
        var roots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
        var touched = new HashSet<DocumentId>();
        foreach (var entry in plan.Chain)
        {
            WorkspaceSyntaxMutation.Set(roots[entry.Handle.Document], $"{entry.Handle.Path}/name", replacement);
            touched.Add(entry.Handle.Document);
        }

        foreach (var read in plan.Reads)
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

    sealed record RewritePlan(WorkspaceSyntaxEntry[] Chain, WorkspaceProvideContextRead[] Reads);
}
