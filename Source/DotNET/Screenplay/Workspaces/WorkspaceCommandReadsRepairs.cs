// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Opts the composed application into protected reads after read-specific admission, independently of unrelated executable blockers.
/// </summary>
internal static class WorkspaceCommandReadsRepairs
{
    internal const string UnrelatedExecutableBlockers = "The model is not executable-ready for reasons unrelated to command reads";

    static readonly HashSet<string> _readRefusalCodes = new(StringComparer.Ordinal)
    {
        DiagnosticCodes.UnprotectableDecisionRead,
        DiagnosticCodes.DecisionReadWithoutIdentifier,
        DiagnosticCodes.DecisionReadsWithConcurrency,
        DiagnosticCodes.UnknownReadPath,
        DiagnosticCodes.AmbiguousReadPath
    };

    // These closed diagnostics belong to command reads and their paths, including the reads/concurrency ambiguity.
    internal static bool IsReadRefusal(Diagnostic diagnostic) => diagnostic.Severity == DiagnosticSeverity.Error && _readRefusalCodes.Contains(diagnostic.Code);

    internal static bool IsLegacyReadDiagnostic(WorkspaceSyntaxIndex index, Diagnostic diagnostic)
    {
        if (diagnostic.Code != DiagnosticCodes.PreservedLegacySemanticSyntax ||
            index.RepairableDiagnostics.Any(value => value.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable))
        {
            return false;
        }
        var first = index.RepairableDiagnostics.FirstOrDefault(value => value.Code == DiagnosticCodes.PreservedLegacySemanticSyntax &&
            index.Entries.Any(entry => entry.Node is ReadsSyntax && entry.Location == value.Location));

        return diagnostic == first;
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location && entry.Node is ReadsSyntax).ToArray();
        if (subjects.Length != 1 || Recipe(index, subjects[0]) is not { } repair) return [];

        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }

    internal static string? UnavailableReason(WorkspaceSyntaxIndex index, WorkspaceNodeHandle subject)
    {
        if (index.Find(subject) is not { Node: ReadsSyntax } entry) return null;
        if (Recipe(index, entry) is not { } repair)
            return "Protected reads require one application root document (declaring the domain), or a single document, with no existing command reads mode.";
        var result = WorkspaceRepairVerification.Propose(index.Workspace, new()
        {
            ExpectedRevision = index.Workspace.Revision,
            ExpectedCatalogRevision = index.Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = repair.RequiredFormatting,
            Operations = repair.Operations
        });
        var reasons = result.ExecutableDiagnostics.Where(IsReadRefusal).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ToArray();
        if (reasons.Length > 0) return string.Join("; ", reasons);
        if (result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) return UnrelatedExecutableBlockers;
        var conflicts = result.Conflicts.Select(conflict => conflict.Message).ToArray();

        return conflicts.Length > 0 ? string.Join("; ", conflicts) : null;
    }

    static WorkspaceDiagnosticRepair? Recipe(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry subject)
    {
        var roots = index.Entries.Where(entry => entry.Node is ApplicationSyntax).ToArray();
        if (roots.Any(entry => ((ApplicationSyntax)entry.Node).CommandReadMode is not null) || index.UnresolvedPlacementDocuments.Length > 0) return null;
        var declared = roots.Where(entry => ((ApplicationSyntax)entry.Node).Domain is not null).ToArray();
        var candidates = declared;
        if (declared.Length == 0 && roots.Length == 1) candidates = roots;
        if (candidates.Length != 1) return null;
        var root = candidates[0];
        var original = (ApplicationSyntax)root.Node;

        return new(DiagnosticCodes.LegacyCommandReadsRepairAvailable, subject.Handle, [new ReplaceWorkspaceNode(root.Handle, original, original with { CommandReadMode = CommandReadMode.Protected })])
        {
            Title = "Opt the application into protected command reads (changes absence and conflict behavior)",
            CanFixAll = false
        };
    }
}
