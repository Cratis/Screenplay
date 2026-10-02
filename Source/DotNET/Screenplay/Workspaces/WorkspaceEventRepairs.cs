// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceEventRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision &&
            entry.Node is EventSyntax { Id: not null } declaration && declaration.Id == declaration.Name &&
            declaration.DirectiveLocations.GetValueOrDefault("id") == diagnostic.Location).ToArray();
        if (subjects.Length != 1)
        {
            return [];
        }

        var subject = subjects[0];
        var original = (EventSyntax)subject.Node;
        return WorkspaceRepairVerification.Discover(index, new(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, original, original with { Id = null })]), verifyRepair);
    }
}
