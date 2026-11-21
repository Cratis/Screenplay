// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Refuses a <c>provide</c> rename whenever something the rename cannot rewrite or prove would be left behind: text the
/// workspace does not bind, reads outside the supported members, and a new name some read already uses.
/// </summary>
/// <param name="workspace">The workspace being renamed in.</param>
/// <param name="scopes">The provide scopes of the workspace.</param>
/// <param name="chain">The declarations the rename rewrites.</param>
/// <param name="request">The rename request.</param>
sealed class WorkspaceProvideRenameGuard(ScreenplayWorkspace workspace, WorkspaceProvideScopes scopes, WorkspaceSyntaxEntry[] chain, WorkspaceRenameRequest request)
{
    internal void RequireNothingUnprovable(WorkspaceSyntaxIndex index, WorkspaceProvideContextRead[] reads)
    {
        foreach (var entry in index.Entries)
        {
            RequireBound(entry);
            RequireSupported(entry);
        }

        RequireNewNameUnread(reads);
        RequireUnambiguous(reads);
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
            .FirstOrDefault(property => property.PropertyType == typeof(string) && property.Name is not ("Description" or "Documentation") && property.GetIndexParameters().Length == 0
                && property.GetValue(entry.Node) is string text && WorkspaceProvideContextRead.Mentions(text, name))?.Name;
    }

    string Position(WorkspaceSyntaxEntry entry) => WorkspaceProvideRefactorings.Position(workspace, entry);

    void RequireBound(WorkspaceSyntaxEntry entry)
    {
        if (WorkspaceOpaqueText.Named(entry.Node, request.ExpectedName, request.NewName) is { } named)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot prove reference safety through {entry.Kind} at '{Position(entry)}', which names '{named}'. Use explicit coordinated typed edits; canonical formatting does not make opaque references safe.");
        }
    }

    void RequireSupported(WorkspaceSyntaxEntry entry)
    {
        if (scopes.Resolves(entry, chain) && UnsupportedRead(entry, request.ExpectedName) is { } member)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot rename '{request.ExpectedName}': {entry.Kind} '{member}' at '{Position(entry)}' reads it as $context.{request.ExpectedName}, which rename does not rewrite. Repair it with a typed edit first.");
        }
    }

    void RequireNewNameUnread(WorkspaceProvideContextRead[] reads)
    {
        foreach (var read in reads.Where(read => read.Root == request.NewName && scopes.Resolves(read.Entry, chain)))
        {
            throw new InvalidWorkspaceAuthoring($"'{request.NewName}' is already read as $context.{read.Path} at '{Position(read.Entry)}'; renaming would make that read resolve to the renamed value. Choose another name.");
        }
    }

    void RequireUnambiguous(WorkspaceProvideContextRead[] reads)
    {
        var ambiguous = reads.FirstOrDefault(read => read.Root == request.ExpectedName && scopes.IsReusable(read.Entry) && scopes.Chains(request.ExpectedName) > 1);
        if (ambiguous is not null)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot prove a rename: the reusable availability read $context.{ambiguous.Path} at '{Position(ambiguous.Entry)}' could resolve to any of {scopes.Chains(request.ExpectedName)} separate declarations of '{request.ExpectedName}'. Provide it once, or repair the availability with a typed edit first.");
        }
    }
}
