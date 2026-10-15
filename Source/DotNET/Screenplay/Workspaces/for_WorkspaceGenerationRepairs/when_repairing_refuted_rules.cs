// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceGenerationRepairs;

public class when_repairing_refuted_rules : Specification
{
    [Theory]
    [InlineData("max 20")]
    [InlineData("matches \"^INV-.*$\"")]
    [InlineData("length == 20 severity warning message \"Keep this message\"")]
    void should_replace_only_the_refuted_rule_with_a_derived_pattern(string rule)
    {
        var workspace = Create(rule);
        var diagnostic = workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        var repair = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).Single();
        repair.Title.ShouldContain("also validates inputs");
        repair.CanFixAll.ShouldBeFalse();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair.DiagnosticCode, repair.Subject, new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = repair.RequiredFormatting,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
        Assert.True(result.Accepted, string.Join('\n', result.Conflicts.Select(conflict => conflict.ToString())));
        var source = result.Workspace!.Documents.Single().Text;
        source.ShouldContain("matches \"^[A-Za-z0-9_-]{21}$\"");
        source.ShouldContain("not empty");
        result.Workspace.Compilation.Diagnostics.ShouldNotContain(item => item.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        result.Workspace.Compilation.Success.ShouldBeTrue();
        workspace.Documents.Single().Text.ShouldContain(rule);
        if (rule.Contains("Keep this message", StringComparison.Ordinal)) source.ShouldContain("severity warning message \"Keep this message\"");
    }

    [Fact]
    void should_not_repair_an_unproven_rule()
    {
        var workspace = Create("matches \"^(?<code>.{21})$\"");
        var diagnostic = workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
    }

    [Fact]
    void should_not_offer_an_entropy_raise_that_refutes_a_length_rule()
    {
        const string Source = "concept Code : String\n  generate nanoid 1\n  validate\n    length == 1\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        code Code generated identifier\n        returns code";
        var workspace = ScreenplayWorkspace.Create("Generation", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Generation")));
        var diagnostic = workspace.Compilation.Diagnostics.Single(item => item.Code == DiagnosticCodes.LowGeneratedIdentifierEntropy);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
    }

    static ScreenplayWorkspace Create(string rule)
    {
        var source = $"concept InviteCode : String\n  generate nanoid\n  validate\n    {rule}\n    not empty\n";

        return ScreenplayWorkspace.Create("Generation", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Generation")));
    }
}
