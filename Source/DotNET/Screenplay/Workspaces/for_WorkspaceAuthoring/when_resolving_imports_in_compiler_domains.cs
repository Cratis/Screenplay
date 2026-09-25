// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_resolving_imports_in_compiler_domains
{
    [Theory]
    [InlineData("type Details\n  value Missing")]
    [InlineData("module App\n  feature F\n    slice StateChange S\n      command Create\n        produces Missing")]
    [InlineData("module App\n  feature F\n    slice Automation S\n      reaction React\n        when Missing")]
    [InlineData("module App\n  feature F\n    slice StateChange S\n      command Create\n        reads Missing")]
    [InlineData("module App\n  feature F\n    slice Automation S\n      event Created\n      reaction React\n        when Created\n          invokes Missing")]
    public void should_admit_imports_where_the_compiler_accepts_them(string addition)
    {
        var result = Propose(addition, WorkspaceAuthoringReferencePolicy.Safe);
        Assert.True(result.Accepted, string.Join(Environment.NewLine, result.Conflicts.Select(conflict => conflict.Message)));
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousReference).ShouldBeFalse();
    }

    [Theory]
    [InlineData("module App\n  feature F\n    slice StateView S\n      screen Actions\n        action Missing")]
    [InlineData("module App\n  feature F\n    slice StateView S\n      screen Actions\n        navigate to Missing")]
    [InlineData("module App\n  feature F\n    slice StateView S\n      readmodel View\n        id Uuid\n      screen Browse\n        data View via query Missing")]
    public void should_refuse_imports_outside_the_compiler_domain(string addition)
    {
        var result = Propose(addition, WorkspaceAuthoringReferencePolicy.Safe);
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.Contains("unresolved", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_an_unknown_persona_policy_with_safe_authoring()
    {
        var result = Propose("persona User\n  policy Missing", WorkspaceAuthoringReferencePolicy.Safe);
        result.Accepted.ShouldBeFalse();
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    public void should_admit_a_draft_policy_with_explicit_reference_debt()
    {
        var result = Propose("persona User\n  policy Missing", WorkspaceAuthoringReferencePolicy.Draft);
        result.Accepted.ShouldBeTrue();
        result.ExecutableReady.ShouldBeFalse();
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy).ShouldBeTrue();
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.Contains("Reference debt: unresolved Policy", StringComparison.Ordinal)).ShouldBeTrue();
        result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_executable_validation_even_with_draft_references()
    {
        var result = Propose("persona User\n  policy Missing", WorkspaceAuthoringReferencePolicy.Draft, WorkspaceAuthoringValidation.Executable);
        result.Accepted.ShouldBeFalse();
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    static WorkspaceAuthoringResult Propose(string addition, WorkspaceAuthoringReferencePolicy policy, WorkspaceAuthoringValidation validation = WorkspaceAuthoringValidation.Authoring)
    {
        const string source = "import External.Missing";
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var syntax = new ScreenplayCompiler().Parse($"{source}\n{addition}").Value!;
        return workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = validation,
            ReferencePolicy = policy,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, syntax)]
        });
    }
}
