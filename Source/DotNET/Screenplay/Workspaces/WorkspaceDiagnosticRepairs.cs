// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A compiler-authored, revision-bound repair. Operations address original syntax occurrences and are
/// previewed through <see cref="ScreenplayWorkspace.ProposeAuthoring"/>, never applied by discovery.
/// </summary>
public sealed record WorkspaceDiagnosticRepair(
    string DiagnosticCode,
    WorkspaceNodeHandle Subject,
    ImmutableArray<WorkspaceAstOperation> Operations);

/// <summary>
/// Discovers deterministic repairs by diagnostic code and original syntax occurrence, not message text.
/// </summary>
public static class WorkspaceDiagnosticRepairs
{
    /// <summary>
    /// Finds repairs for a diagnostic from this exact workspace revision. Unknown or ambiguous subjects
    /// and diagnostics not produced by this workspace have no repair.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="revision">The revision that supplied the diagnostic.</param>
    /// <param name="diagnostic">The original diagnostic.</param>
    /// <returns>Zero or more typed repair proposals.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> Find(ScreenplayWorkspace workspace, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return revision == workspace.Revision ? Find(WorkspaceSyntaxIndex.Create(workspace), revision, diagnostic) : [];
    }

    /// <summary>
    /// Finds repairs using a previously built occurrence index for the expected revision.
    /// The diagnostic must be present in that index.
    /// </summary>
    /// <param name="index">The original workspace occurrence index.</param>
    /// <param name="revision">The expected workspace revision.</param>
    /// <param name="diagnostic">A diagnostic reported by the index.</param>
    /// <returns>Zero or more typed repair proposals.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        if (diagnostic is null || diagnostic.Code != DiagnosticCodes.LegacyInlineCodeFence)
        {
            return [];
        }

        // PLAY0397 also covers bare description fences and legacy handler language lines. Only the
        // 'validate csharp' form has a unique CodeValidateSyntax subject at the warning's position.
        if (!index.Diagnostics.Any(item => item.Code == diagnostic.Code && item.Location == diagnostic.Location))
        {
            return [];
        }

        var matching = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is CodeValidateSyntax && entry.Location == diagnostic.Location).ToArray();
        if (matching.Length != 1)
        {
            return [];
        }

        var subject = matching[0];
        return [new WorkspaceDiagnosticRepair(
            diagnostic.Code,
            subject.Handle,
            [new ReplaceWorkspaceNode(subject.Handle, subject.Node, subject.Node)])];
    }
}
