// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_repairing_action_subjects : Specification
{
    const string Source = """
        concept ItemId : Uuid
        module Work
          feature Items
            slice StateView List
              readmodel Row
                itemId ItemId key
              command Retry
                itemId ItemId
              query Rows => Row[]
              screen List
                data Row[] via query Rows
                action Retry
        """;
    ScreenplayWorkspace _workspace;
    WorkspaceSyntaxIndex _index;

    void Establish()
    {
        _workspace = ScreenplayWorkspace.Create("Model", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));
        _index = WorkspaceSyntaxIndex.Create(_workspace);
    }

    [Fact]
    void should_offer_both_exact_migration_choices()
    {
        var diagnostic = _index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.ChangedActionSubject);
        var repairs = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, diagnostic);
        repairs.Length.ShouldEqual(3);
        repairs.Select(repair => repair.Title).ShouldContain("Keep legacy row bindings explicitly");
    }

    [Fact]
    void should_preview_an_explicit_subject_without_mutating_the_snapshot()
    {
        var repair = Inference();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request());
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldContain("action Retry for screen");
        _workspace.Documents.Single().Text.ShouldEqual(Source);
        result.AuthoringDiagnostics.Any(value => value.Code is DiagnosticCodes.InferredActionSubject or DiagnosticCodes.ChangedActionSubject).ShouldBeFalse();
    }

    [Fact]
    void should_encode_the_legacy_row_mapping_explicitly()
    {
        var diagnostic = _index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.ChangedActionSubject);
        var repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, diagnostic).Single(value => value.Title == "Keep legacy row bindings explicitly");
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, Request());
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldContain("action Retry for item");
        result.Workspace.Documents.Single().Text.ShouldContain("with itemId from item.itemId");
        result.AuthoringDiagnostics.Any(value => value.Code == DiagnosticCodes.ChangedActionSubject).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_stale_revisions() => WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, Inference(), Request() with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);

    [Fact]
    void should_require_formatting_consent() => WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, Inference(), Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveExactSource }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);

    WorkspaceDiagnosticRepair Inference() => WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, _index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.InferredActionSubject)).Single();

    WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
}
