// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_completing_a_partial_key : Specification
{
    internal const string Source = "module M\n  feature F\n    slice StateChange S\n      readmodel Row\n        id String key\n        period Int key\n      command C\n        id String\n        period Int\n        reads Row by id // lookup\n";
    ScreenplayWorkspace _workspace;
    WorkspaceDiagnosticRepair _repair;
    WorkspaceAuthoringResult _result;

    void Establish()
    {
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(Source));
        _workspace = ScreenplayWorkspace.Create("Keys", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Keys")));
        var index = WorkspaceSyntaxIndex.Create(_workspace);
        var diagnostic = index.RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey);
        _repair = WorkspaceDiagnosticRepairs.Find(index, _workspace.Revision, diagnostic).Single();
    }

    void Because() => _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring
    });

    [Fact] void should_accept_the_authoring_repair() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_leave_execution_unadmitted() => _result.ExecutableReady.ShouldBeFalse();
    [Fact] void should_keep_comments() => WorkspaceDroppedComments.In(_result.WritePlan!).ShouldBeEmpty();
    [Fact] void should_supply_every_part() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<ReadsSyntax>().Single().ByParts.Select(part => part.Property).ShouldEqual("id", "period");

    [Theory]
    [InlineData("period Int optional")]
    [InlineData("period String")]
    void should_offer_no_repair_for_an_incompatible_source(string field)
    {
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(Source.Replace("        period Int\n", "        " + field + "\n", StringComparison.Ordinal)));
        var workspace = ScreenplayWorkspace.Create("Keys", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Keys")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey);
        WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).ShouldBeEmpty();
    }
}
