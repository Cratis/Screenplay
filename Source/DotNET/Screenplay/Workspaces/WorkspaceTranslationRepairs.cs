// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Proposes the explicit translation direction a Translate slice using public events must declare (PLAY0603),
/// only when exactly one direction is consistent with the slice's events and constructs.
/// </summary>
internal static class WorkspaceTranslationRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location &&
            entry.Node is SliceSyntax { Type: SliceType.Translate, Direction: null }).ToArray();
        if (subjects.Length != 1)
        {
            return [];
        }

        var subject = subjects[0];
        var original = (SliceSyntax)subject.Node;

        // Direction is a contract decision, so each candidate is verified in its own transaction: a direction the slice's
        // events and constructs contradict is rejected by the authoring transaction and never offered. The shared
        // per-subject verdict cache is not used because both candidates have the same subject and code.
        var candidates = new[] { TranslationDirection.Inbound, TranslationDirection.Outbound }.Select(direction =>
            new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, original, original with { Direction = direction })])
            {
                Title = $"Declare 'direction {(direction == TranslationDirection.Inbound ? "inbound" : "outbound")}'",
                CanFixAll = false
            });
        var verified = candidates.Where(candidate => WorkspaceRepairVerification.RequireComments(WorkspaceRepairVerification.Propose(index.Workspace, new WorkspaceAuthoringRequest
        {
            ExpectedRevision = index.Workspace.Revision,
            ExpectedCatalogRevision = index.Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = candidate.RequiredFormatting,
            Operations = candidate.Operations
        })).Accepted).ToArray();

        // When both directions are consistent the choice is the author's; offering neither keeps code-and-subject repair unambiguous.
        return verified.Length == 1 ? [verified[0]] : [];
    }
}
