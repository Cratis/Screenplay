// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_removing_duplicate_compliance_markers : Specification
{
    const string Source = "// café\r\nconcept Café : String pii personal pii secret secret // keep\r\n  pii reason \"pii personal secret\"\r\n  secret scope namespace";
    ScreenplayWorkspace _workspace;
    WorkspaceDiagnosticRepair _repair;
    WorkspaceAuthoringResult _result;

    void Establish()
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Source)]);
        _workspace = ScreenplayWorkspace.Create("Model", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));
        var index = WorkspaceSyntaxIndex.Create(_workspace);
        var diagnostic = index.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateComplianceMarker);
        _repair = WorkspaceDiagnosticRepairs.Find(index, _workspace.Revision, diagnostic).Single();
    }

    void Because() => _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, Request());

    [Fact] void should_accept_the_verified_line_repair() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_keep_first_markers_notes_comments_bom_and_line_endings() => _result.Workspace!.Documents.Single().Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Source.Replace("pii personal pii secret secret", "pii secret", StringComparison.Ordinal))]);
    [Fact] void should_remove_the_warning() => _result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateComplianceMarker).ShouldBeFalse();
    [Fact] void should_not_change_the_original_snapshot() => _workspace.Documents.Single().Text.ShouldEqual(Source);
    [Fact] void should_refuse_stale_revisions() => WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, Request() with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_require_formatting_consent() => WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveExactSource }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);
    [Fact] void should_refuse_a_forged_expected_concept() => WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair with { Operations = [((RemoveDuplicateComplianceMarkers)_repair.Operations[0]) with { Expected = ((RemoveDuplicateComplianceMarkers)_repair.Operations[0]).Expected with { Name = "Forged" } }] }, Request()).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnknownRepair);

    WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
    };
}
