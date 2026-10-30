// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_repairing_implicit_authorization_conjunctions
{
    const string Head = "policy Staff\n  require authenticated\npolicy Managers\n  require authenticated\npolicy Auditors\n  require authenticated\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Uuid identifier\n";

    [Theory]
    [InlineData("        authorize Staff Managers // both\n", "        authorize Staff and Managers // both\n")]
    [InlineData("        authorize (Staff or Managers) Auditors\n", "        authorize (Staff or Managers) and Auditors\n")]
    [InlineData("        authorize Staff and Managers\n          // between\n          Auditors // last\n", "        authorize Staff and Managers\n          // between\n          and Auditors // last\n")]
    [InlineData("        authorize Staff Managers\n          Auditors\n", "        authorize Staff and Managers\n          and Auditors\n")]
    [InlineData("        authorize Staff\r\n          Managers\r\n", "        authorize Staff\r\n          and Managers\r\n")]
    void should_write_the_and_and_prove_the_syntax_unchanged(string source, string expected)
    {
        var workspace = Workspace(Head + source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.Diagnostics.Single(value => value.Code == DiagnosticCodes.ImplicitAuthorizationConjunction);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Information);
        diagnostic.Location.Line.ShouldEqual(12);
        var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        repair.RequiredFormatting.ShouldEqual(WorkspaceAuthoringFormatting.PreserveTrivia);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldEqual(Head + expected);
        result.AuthoringDiagnostics.Any(value => value.Code == DiagnosticCodes.ImplicitAuthorizationConjunction).ShouldBeFalse();
        SyntaxJson.StructurallyEqual(new ScreenplayCompiler().Parse(Head + source).Value!, new ScreenplayCompiler().Parse(Head + expected).Value!).ShouldBeTrue();
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
