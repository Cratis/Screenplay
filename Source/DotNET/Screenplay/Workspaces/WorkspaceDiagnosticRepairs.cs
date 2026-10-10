// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A compiler-authored, revision-bound repair. Operations address original syntax occurrences and are
/// previewed through <see cref="WorkspaceDiagnosticRepairs.ProposeRepair"/>, never applied by discovery.
/// The PLAY0397 operation replaces its subject with itself: canonical printing migrates the entire touched
/// document, potentially normalizing other legacy forms. A repair is refused if any comment would be lost.
/// </summary>
public sealed record WorkspaceDiagnosticRepair(
    string DiagnosticCode,
    WorkspaceNodeHandle Subject,
    ImmutableArray<WorkspaceAstOperation> Operations)
{
    /// <summary>
    /// Gets the least disruptive formatting that makes this repair effective. Optionality spelling repairs preserve trivia.
    /// </summary>
    public WorkspaceAuthoringFormatting RequiredFormatting { get; init; } = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments;

    /// <summary>
    /// Gets the human-readable action label, including any contract-changing consequence.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets whether a host may include the repair in fix-all. Contract-changing repairs require individual review.
    /// </summary>
    public bool CanFixAll { get; init; } = true;

    /// <summary>
    /// Gets semantic addresses deliberately retired by this repair.
    /// </summary>
    public ImmutableArray<SemanticAddress> RetiredSemanticAddresses { get; init; } = [];
}

/// <summary>
/// Discovers deterministic repairs by diagnostic code and original syntax occurrence, not message text.
/// </summary>
public static class WorkspaceDiagnosticRepairs
{
    /// <summary>
    /// Previews a revision-bound repair without writing files. Canonical printing can change other legacy forms
    /// and whitespace in the touched document. Refuses any dropped comment,
    /// even outside the repair subject. Optionality spelling repairs also accept explicit canonical formatting consent.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="repair">A repair discovered for this workspace revision.</param>
    /// <param name="request">The authoring request with expected revisions and explicit formatting consent.</param>
    /// <returns>An accepted candidate and write plan, or typed conflicts without a partial candidate.</returns>
    public static WorkspaceAuthoringResult ProposeRepair(ScreenplayWorkspace workspace, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringRequest request)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(repair);
        ArgumentNullException.ThrowIfNull(request);

        // Preserve the transaction's typed stale-revision result before checking the subject.
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return workspace.ProposeAuthoring(request with { Operations = repair.Operations });
        }

        if (!PermitsFormatting(repair.DiagnosticCode, request.Formatting))
        {
            return Refuse(WorkspaceConflictKind.FormattingConsentRequired, "Canonical formatting consent is required for this diagnostic repair.");
        }

        var index = WorkspaceSyntaxIndex.Create(workspace);
        var matches = ForSubject(index, workspace.Revision, repair.DiagnosticCode, repair.Subject)
            .Where(candidate => Matches(candidate, repair)).ToArray();
        if (matches.Length != 1 || repair.Operations.IsDefaultOrEmpty)
        {
            return Refuse(WorkspaceConflictKind.UnknownRepair, "The diagnostic repair does not match the original workspace subject.");
        }

        return ProposeSelected(workspace, index, matches[0], request);
    }

    /// <summary>
    /// Previews the repair for one code and original subject, verifying only that subject in one transaction.
    /// No discovery transactions are run for other diagnostics in the workspace.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="diagnosticCode">The diagnostic code to repair.</param>
    /// <param name="subject">The original revision-bound syntax occurrence.</param>
    /// <param name="request">The expected revisions and explicit formatting consent.</param>
    /// <returns>An accepted candidate and write plan, or typed conflicts without a partial candidate.</returns>
    public static WorkspaceAuthoringResult ProposeRepair(ScreenplayWorkspace workspace, string diagnosticCode, WorkspaceNodeHandle subject, WorkspaceAuthoringRequest request)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(diagnosticCode);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(request);

        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return workspace.ProposeAuthoring(request);
        }

        if (!PermitsFormatting(diagnosticCode, request.Formatting))
        {
            return Refuse(WorkspaceConflictKind.FormattingConsentRequired, "Canonical formatting consent is required for this diagnostic repair.");
        }

        var index = WorkspaceSyntaxIndex.Create(workspace);
        var matches = ForSubject(index, workspace.Revision, diagnosticCode, subject).ToArray();
        if (matches.Length != 1)
        {
            return Refuse(WorkspaceConflictKind.UnknownRepair, "The diagnostic repair does not match the original workspace subject.");
        }

        return ProposeSelected(workspace, index, matches[0], request);
    }

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
    /// <remarks>PLAY0166, PLAY0478, PLAY0469, PLAY0471, PLAY0479 and PLAY0516 verdicts (acceptance and conflicts only) are cached on the immutable workspace snapshot for discovery, never shared with a newer revision. PLAY0479 verifies once per document; occurrence discovery conservatively requires that document migration to pass. Proposals always run one fresh transaction and return its full diagnostics.</remarks>
    /// <returns>Zero or more typed repair proposals.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic) =>
        Find(index, revision, diagnostic, true);

    /// <summary>
    /// Finds a verified, document-wide optionality migration as one typed proposal.
    /// </summary>
    /// <param name="index">The original occurrence index.</param>
    /// <param name="subject">The revision-bound document root.</param>
    /// <returns>A single proposal containing every legacy type splice, or none when it cannot be verified.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> FindDocumentOptionality(WorkspaceSyntaxIndex index, WorkspaceNodeHandle subject) =>
        WorkspaceOptionalityRepairs.ForDocument(index, subject, true);

    /// <summary>
    /// Finds a verified document-wide compliance spelling migration.
    /// </summary>
    /// <param name="index">The original occurrence index.</param>
    /// <param name="subject">The revision-bound document root.</param>
    /// <returns>A proposal preserving all concept notes and trivia, or none.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> FindDocumentCompliance(WorkspaceSyntaxIndex index, WorkspaceNodeHandle subject) =>
        WorkspaceComplianceRepairs.ForDocument(index, subject, true);

    static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (diagnostic is null)
        {
            return [];
        }

        // PLAY0397 also covers bare description fences and legacy handler language lines. Only the
        // 'validate csharp' form has a unique CodeValidateSyntax subject at the warning's position.
        if (!index.RepairableDiagnosticSet.Contains(diagnostic))
        {
            return [];
        }

        if (diagnostic.Code == DiagnosticCodes.UnknownEvent || diagnostic.Code == DiagnosticCodes.OmittedProductionDestination)
        {
            return WorkspaceProductionRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.EventFromLaterSlice)
        {
            return WorkspaceTimelineRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.DuplicateComplianceMarker)
        {
            return WorkspaceDuplicateComplianceRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.LegacyComplianceMarker)
        {
            return WorkspaceComplianceRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix)
        {
            return WorkspaceOptionalityRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.RedundantProductionRoute)
        {
            return WorkspaceProductionRouteRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.RedundantEventId || diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload)
        {
            return WorkspaceEventRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.LegacyInteractionWhere || diagnostic.Code == DiagnosticCodes.InlineInteractionAlternative)
        {
            return WorkspaceInteractionRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey)
        {
            return WorkspaceReadModelKeyRepairs.Find(index, revision, diagnostic, verifyRepair);
        }

        if (diagnostic.Code == DiagnosticCodes.PublicTranslationRequiresDirection)
        {
            return WorkspaceTranslationRepairs.Find(index, revision, diagnostic);
        }

        if (diagnostic.Code != DiagnosticCodes.LegacyInlineCodeFence)
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

    static IEnumerable<WorkspaceDiagnosticRepair> ForSubject(WorkspaceSyntaxIndex index, WorkspaceRevision revision, string code, WorkspaceNodeHandle subject)
    {
        if (index.Find(subject) is not { } entry)
        {
            return [];
        }

        if (code == DiagnosticCodes.LegacyComplianceMarker && entry.Node is ApplicationSyntax)
        {
            return WorkspaceComplianceRepairs.ForDocument(index, subject, false);
        }

        if (code == DiagnosticCodes.LegacyOptionalSuffix && entry.Node is ApplicationSyntax)
        {
            return WorkspaceOptionalityRepairs.ForDocument(index, subject, false);
        }

        // Filter before building or verifying recipes. In particular, PLAY0478 can occur on
        // every plain production in a workspace, but only the selected occurrence is relevant.
        return index.RepairableDiagnostics.Where(diagnostic => diagnostic.Code == code && (diagnostic.Location == entry.Location ||
            (code == DiagnosticCodes.RedundantEventId && entry.Node.DirectiveLocations.GetValueOrDefault("id") == diagnostic.Location) ||
            (code == DiagnosticCodes.LegacyComplianceMarker && entry.Node.DirectiveLocations.Values.Contains(diagnostic.Location)) ||
            (code == DiagnosticCodes.LegacyInteractionWhere && entry.Node.DirectiveLocations.GetValueOrDefault("where") == diagnostic.Location)))
            .SelectMany(diagnostic => Find(index, revision, diagnostic, false))
            .Where(repair => repair.Subject == subject);
    }

    static WorkspaceAuthoringResult ProposeSelected(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringRequest request) =>
        WorkspaceRepairVerification.Verify(index, repair, request);

    static bool Matches(WorkspaceDiagnosticRepair candidate, WorkspaceDiagnosticRepair selected)
    {
        if (candidate.RequiredFormatting != selected.RequiredFormatting || candidate.Title != selected.Title || candidate.CanFixAll != selected.CanFixAll ||
            selected.RetiredSemanticAddresses.IsDefault || !candidate.RetiredSemanticAddresses.SequenceEqual(selected.RetiredSemanticAddresses) ||
            selected.Operations.IsDefault || candidate.Operations.Length != selected.Operations.Length)
        {
            return false;
        }

        try
        {
            return candidate.Operations.Zip(selected.Operations).All(pair => (pair.First, pair.Second) switch
            {
                (RemoveDuplicateComplianceMarkers left, RemoveDuplicateComplianceMarkers right) => left.Target == right.Target && SyntaxJson.StructurallyEqual(left.Expected, right.Expected),
                (MigrateComplianceMarkerSpelling left, MigrateComplianceMarkerSpelling right) => left.Target == right.Target && left.Line == right.Line && SyntaxJson.StructurallyEqual(left.Expected, right.Expected),
                (MigrateOptionalTypeSpelling left, MigrateOptionalTypeSpelling right) => left.Target == right.Target && SyntaxJson.StructurallyEqual(left.Expected, right.Expected),
                (AddWorkspaceNode left, AddWorkspaceNode right) => left.Parent == right.Parent && left.Member == right.Member && left.Index == right.Index &&
                    SyntaxJson.StructurallyEqual(left.ExpectedParent, right.ExpectedParent) && SyntaxJson.StructurallyEqual(left.Node, right.Node),
                (ReplaceWorkspaceNode left, ReplaceWorkspaceNode right) => left.Target == right.Target &&
                    SyntaxJson.StructurallyEqual(left.Expected, right.Expected) && SyntaxJson.StructurallyEqual(left.Node, right.Node),
                (RemoveWorkspaceNode left, RemoveWorkspaceNode right) => left.Target == right.Target && SyntaxJson.StructurallyEqual(left.Expected, right.Expected),
                (MoveWorkspaceNode left, MoveWorkspaceNode right) => left.Target == right.Target && left.Parent == right.Parent && left.Member == right.Member && left.Index == right.Index &&
                    SyntaxJson.StructurallyEqual(left.Expected, right.Expected) && SyntaxJson.StructurallyEqual(left.ExpectedParent, right.ExpectedParent),
                _ => false
            });
        }
        catch (InvalidSyntaxJson)
        {
            return false;
        }
    }

    static bool PermitsFormatting(string code, WorkspaceAuthoringFormatting formatting) =>
        formatting == WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments ||
        ((code == DiagnosticCodes.LegacyOptionalSuffix || code == DiagnosticCodes.LegacyComplianceMarker || code == DiagnosticCodes.DuplicateComplianceMarker) && formatting == WorkspaceAuthoringFormatting.PreserveTrivia);

    static WorkspaceAuthoringResult Refuse(WorkspaceConflictKind kind, string message) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }]
    };
}
