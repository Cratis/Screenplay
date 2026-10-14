// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceGenerationRepairs;

public class when_repairing_phase_three_rules : Specification
{
    [Theory]
    [InlineData("INV-#{4}", null)]
    [InlineData("INV-#{4}", "luhn")]
    [InlineData("INV-{yyyy}-#{4}", "mod97")]
    [InlineData("[AB]{1,2}-#{2}", "mod11")]
    void should_replace_matches_and_add_declared_checksum_in_one_reviewable_proposal(string pattern, string? checksum)
    {
        var workspace = Create(pattern, checksum);
        var diagnostic = workspace.Compilation.Diagnostics.First(item => item.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        var repair = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).Single();
        repair.Operations.Length.ShouldEqual(1);
        repair.CanFixAll.ShouldBeFalse();
        repair.Title.ShouldContain("inputs and stored values");
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        Assert.True(result.Accepted, string.Join('\n', result.Conflicts.Select(conflict => conflict.Message)));
        var source = result.Workspace!.Documents.Single().Text;
        source.ShouldContain("matches");
        source.ShouldContain("severity warning message \"Keep this message\"");
        source.ShouldContain("not empty");
        if (checksum is not null) source.ShouldContain("checksum " + checksum);
        result.Workspace.Compilation.Diagnostics.ShouldNotContain(item => item.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        workspace.Documents.Single().Text.ShouldContain("max 1");
    }

    [Fact]
    void should_explain_why_an_ambiguous_derived_expression_is_not_a_repair()
    {
        var workspace = Create("[AB]{1,64}[AB]{1,64}", null);
        var diagnostic = workspace.Compilation.Diagnostics.First(item => item.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ConceptSyntax).Handle;
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, diagnostic.Code, subject, Request(workspace));
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.ShouldContain("ambiguous");
    }

    [Fact]
    void should_not_duplicate_an_existing_checksum_rule()
    {
        var workspace = Create("#{4}", "luhn", true);
        var diagnostic = workspace.Compilation.Diagnostics.First(item => item.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        var repair = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        result.Accepted.ShouldBeTrue();
        var concept = result.Workspace!.Compilation.Value!.Model.Application.Concepts.Single();
        concept.Validations.Count(rule => rule.Kind == SemanticValidationRuleKind.Checksum).ShouldEqual(1);
    }

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring
    };

    static ScreenplayWorkspace Create(string pattern, string? checksum, bool existingChecksum = false)
    {
        var modifier = checksum is null ? "" : " checksum " + checksum;
        var validation = existingChecksum ? "\n    checksum " + checksum : "";
        var source = $"concept Code : String\n  generate pattern \"{pattern}\"{modifier}\n  validate\n    max 1 severity warning message \"Keep this message\"\n    not empty{validation}\n";

        return ScreenplayWorkspace.Create("Generation", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Generation")));
    }
}
