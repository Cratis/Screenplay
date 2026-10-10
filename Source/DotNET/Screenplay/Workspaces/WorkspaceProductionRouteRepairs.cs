// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceProductionRouteRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        var routes = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location && entry.Node is CommandStreamSyntax).ToArray();
        if (routes is not [var route] || route.Parent is null || index.Find(route.Parent) is not { Node: ProducesSyntax produced } production) return [];
        var owner = index.Find(production.Parent!);
        if (owner?.Node is not CommandSyntax { Stream: { } inherited } || produced.Stream is null || !EventSourceValidator.SameRoute(produced.Stream, inherited)) return [];

        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, route.Handle, [new RemoveWorkspaceNode(route.Handle, route.Node)]) { Title = "Remove redundant production route" };

        return WorkspaceRepairVerification.Discover(index, repair, verifyRepair);
    }
}
