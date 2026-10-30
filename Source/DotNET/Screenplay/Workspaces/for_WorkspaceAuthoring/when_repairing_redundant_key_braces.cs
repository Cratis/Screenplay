// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_repairing_redundant_key_braces
{
    const string Head = "module M\n  feature F\n    slice StateChange P\n      event E\n        id Uuid\n        other Uuid\n    slice StateView C\n      readmodel V\n        id Uuid\n      query FindC => V optional\n        by id Uuid\n      projection CProjection => V\n        from E key id\n";

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    void should_remove_the_braces_keep_comments_and_prove_the_syntax_unchanged(string newline)
    {
        var source = (Head + "          key Composite { // keep\n            id = id // part\n            other = other\n          } \n          // trailing\n").Replace("\n", newline, StringComparison.Ordinal);
        var expected = (Head + "          key Composite // keep\n            id = id // part\n            other = other\n          // trailing\n").Replace("\n", newline, StringComparison.Ordinal);
        var workspace = Workspace(source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.Diagnostics.Single(value => value.Code == DiagnosticCodes.RedundantCompositeKeyBraces);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Information);
        diagnostic.Location.Line.ShouldEqual(14);
        var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        repair.RequiredFormatting.ShouldEqual(WorkspaceAuthoringFormatting.PreserveTrivia);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldEqual(expected);
        result.AuthoringDiagnostics.Any(value => value.Code == DiagnosticCodes.RedundantCompositeKeyBraces).ShouldBeFalse();
        SyntaxJson.StructurallyEqual(new ScreenplayCompiler().Parse(source).Value!, new ScreenplayCompiler().Parse(result.Workspace.Documents.Single().Text).Value!).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_closing_brace_that_carries_a_comment()
    {
        var workspace = Workspace(Head + "          key {\n            id = id\n          } // keep\n");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.Diagnostics.Single(value => value.Code == DiagnosticCodes.RedundantCompositeKeyBraces);
        WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).ShouldBeEmpty();
    }

    [Fact]
    void should_not_offer_a_repair_for_a_key_without_braces()
    {
        var workspace = Workspace(Head + "          key Composite\n            id = id\n");
        WorkspaceSyntaxIndex.Create(workspace).Diagnostics.Any(value => value.Code == DiagnosticCodes.RedundantCompositeKeyBraces).ShouldBeFalse();
    }

    static ScreenplayWorkspace Workspace(string source) => ScreenplayWorkspace.Create(
        "Model",
        [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
    };
}
