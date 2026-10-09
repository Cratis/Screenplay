// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_migrating_compliance_spelling : Specification
{
    const string Source = "// café @pii\r\nconcept Value : String @pii @sensitive // keep sensitive\r\n  sensitive reason \"Keep @pii, sensitive and legal text verbatim\"\r\n  pii reason \"Personal note\"";
    ScreenplayWorkspace _workspace;
    WorkspaceSyntaxIndex _index;
    WorkspaceNodeHandle _root;

    void Establish()
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Source)]);
        _workspace = ScreenplayWorkspace.Create("Model", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));
        _index = WorkspaceSyntaxIndex.Create(_workspace);
        _root = _index.Entries.Single(entry => entry.Node is ApplicationSyntax).Handle;
    }

    [Fact]
    void should_preserve_notes_comments_bom_and_line_endings_in_the_document_repair()
    {
        var repair = WorkspaceDiagnosticRepairs.FindDocumentCompliance(_index, _root).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request());
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Source.Replace("String @pii @sensitive", "String pii secret", StringComparison.Ordinal).Replace("  sensitive reason", "  secret reason", StringComparison.Ordinal))]);
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyComplianceMarker).ShouldBeFalse();
        _workspace.Documents.Single().Text.ShouldEqual(Source);
    }

    [Fact]
    void should_migrate_only_the_selected_header_line()
    {
        var diagnostic = _index.Diagnostics.First(value => value.Code == DiagnosticCodes.LegacyComplianceMarker);
        var repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, diagnostic).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request());
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldEqual(Source.Replace("String @pii @sensitive", "String pii secret", StringComparison.Ordinal));
        result.AuthoringDiagnostics.Count(value => value.Code == DiagnosticCodes.LegacyComplianceMarker).ShouldEqual(1);
    }

    [Fact]
    void should_migrate_only_the_selected_reason_line()
    {
        var diagnostic = _index.Diagnostics.Last(value => value.Code == DiagnosticCodes.LegacyComplianceMarker);
        var repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, diagnostic).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request());
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldEqual(Source.Replace("  sensitive reason", "  secret reason", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_a_forged_line()
    {
        var repair = WorkspaceDiagnosticRepairs.FindDocumentCompliance(_index, _root).Single();
        var operation = (MigrateComplianceMarkerSpelling)repair.Operations[0];
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair with { Operations = [operation with { Line = 1 }] }, Request()).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnknownRepair);
    }

    [Fact]
    void should_refuse_a_stale_revision() =>
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyComplianceMarker, _root, Request() with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);

    [Fact]
    void should_require_spelling_edit_consent() =>
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyComplianceMarker, _root, Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveExactSource }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);

    [Fact]
    void should_offer_a_document_root_repair_by_code_and_subject() =>
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyComplianceMarker, _root, Request()).Accepted.ShouldBeTrue();

    WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
    };
}
