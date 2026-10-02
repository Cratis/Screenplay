// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_migrating_document_optionality : Specification
{
    const string Source = "// café?\r\ntype Details\r\n  note      String? // keep?\r\n  lines     String[]?";
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
    void should_preview_all_splices_in_one_transaction_without_mutating_the_original()
    {
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyOptionalSuffix, _root, Request());
        result.Accepted.ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(_workspace).ShouldEqual(1);
        _workspace.Documents.Single().Text.ShouldEqual(Source);
        var expected = Source.Replace("String?", "String optional", StringComparison.Ordinal).Replace("String[]?", "String[] optional", StringComparison.Ordinal);
        result.Workspace!.Documents.Single().Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(expected)]);
        result.WritePlan!.Entries.Length.ShouldEqual(1);
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldBeFalse();
        var original = new ScreenplayCompiler().Parse(Source).Value!;
        var candidate = new ScreenplayCompiler().Parse(result.Workspace.Documents.Single().Text).Value!;
        SyntaxJson.StructurallyEqual(original, candidate).ShouldBeTrue();
    }

    [Fact]
    void should_offer_the_same_verified_document_proposal_on_repeated_discovery()
    {
        var repair = WorkspaceDiagnosticRepairs.FindDocumentOptionality(_index, _root).Single();
        repair.RequiredFormatting.ShouldEqual(WorkspaceAuthoringFormatting.PreserveTrivia);
        repair.Operations.Length.ShouldEqual(2);
        WorkspaceDiagnosticRepairs.FindDocumentOptionality(_index, _root).Single().Subject.ShouldEqual(repair.Subject);
        WorkspaceProductionRepairs.TransactionCount(_workspace).ShouldEqual(1);
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request()).Accepted.ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(_workspace).ShouldEqual(2);
    }

    [Fact]
    void should_migrate_only_the_selected_occurrence()
    {
        var diagnostic = _index.Diagnostics.First(value => value.Code == DiagnosticCodes.LegacyOptionalSuffix);
        var repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, diagnostic).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request());
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldEqual(Source.Replace("String?", "String optional", StringComparison.Ordinal));
        result.AuthoringDiagnostics.Count(value => value.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldEqual(1);
    }

    [Fact]
    void should_refuse_stale_workspace_revisions() =>
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyOptionalSuffix, _root, Request() with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);

    [Fact]
    void should_refuse_stale_catalog_revisions() =>
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyOptionalSuffix, _root, Request() with { ExpectedCatalogRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);

    [Fact]
    void should_require_explicit_permission_to_edit_spelling() =>
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyOptionalSuffix, _root, Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveExactSource }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);

    [Fact]
    void should_refuse_forged_type_expectations()
    {
        var repair = WorkspaceDiagnosticRepairs.FindDocumentOptionality(_index, _root).Single();
        var operation = (MigrateOptionalTypeSpelling)repair.Operations[0];
        var forged = repair with { Operations = [operation with { Expected = operation.Expected with { Name = "Uuid" } }] };
        WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, forged, Request()).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnknownRepair);
    }

    [Fact]
    void should_allow_canonical_printing_only_when_explicitly_requested()
    {
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, DiagnosticCodes.LegacyOptionalSuffix, _root, Request() with { Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments });
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldContain("note String optional");
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeTrue();
    }

    WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
    };
}
