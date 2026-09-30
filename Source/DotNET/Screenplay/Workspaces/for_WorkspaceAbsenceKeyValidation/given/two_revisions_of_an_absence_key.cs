// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation.given;

/// <summary>
/// Validates a candidate revision against an original one whose occurrences correspond position by position, as they do
/// for in-place edits and trailing removals that the generic reference engine would not otherwise admit.
/// </summary>
public class two_revisions_of_an_absence_key : for_WorkspaceRefactoring.given.a_workspace_with_an_absent_read_model_key
{
    protected ImmutableArray<Diagnostic>.Builder Diagnostics { get; } = ImmutableArray.CreateBuilder<Diagnostic>();

    protected static SemanticAddress AddressOf(WorkspaceSyntaxIndex index, Func<WorkspaceSyntaxEntry, bool> matches) => index.Entries.Single(matches).Address!;

    protected Exception? Validate(
        string original,
        string candidate,
        WorkspaceAuthoringReferencePolicy policy,
        Func<WorkspaceSyntaxIndex, WorkspaceSyntaxIndex, Dictionary<SemanticAddress, SemanticAddress>>? migrations = null)
    {
        CreateWith(original);
        var before = WorkspaceSyntaxIndex.Create(Workspace);
        CreateWith(candidate);
        var after = WorkspaceSyntaxIndex.Create(Workspace);
        var provenance = new WorkspaceEditProvenance();
        provenance.Subtree(before, (Invoice.Id, string.Empty), (Invoice.Id, string.Empty));
        var renames = migrations?.Invoke(before, after) ?? [];
        return Catch.Exception(() => WorkspaceAbsenceKeyValidation.Validate(before, after, false, provenance, policy, renames, Diagnostics));
    }

    protected IEnumerable<string> Debt => Diagnostics.Select(diagnostic => diagnostic.Message).Where(message => message.Contains("unresolved absence key", StringComparison.Ordinal));
}
