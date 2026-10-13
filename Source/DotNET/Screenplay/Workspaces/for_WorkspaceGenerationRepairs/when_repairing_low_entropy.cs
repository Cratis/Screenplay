// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceGenerationRepairs;

public class when_repairing_low_entropy
{
    [Theory]
    [InlineData("nanoid 1", "nanoid 11", true)]
    [InlineData("nanoid 1 alphabet \"abc\"", "nanoid 41 alphabet \"abc\"", false)]
    [InlineData("nanoid 2 alphabet \"ab\" prefix \"ref_\"", "nanoid 64 alphabet \"ab\" prefix \"ref_\"", true)]
    [InlineData("bytes 7 hex", "bytes 8 hex", true)]
    [InlineData("bytes 1 base32 case lower", "bytes 8 base32 case lower", false)]
    void should_raise_the_concept_in_another_document(string strategy, string expected, bool identifier)
    {
        var workspace = Create(strategy, identifier);
        var diagnostic = workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == Code(identifier));
        var repair = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).Single(repair => repair.Title!.StartsWith("Raise", StringComparison.Ordinal));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Find(repair.Subject)!.Node.ShouldBeOfExactType<ConceptSyntax>();
        index.Find(repair.Subject)!.Location.Path.ShouldEqual("concepts.play");
        var result = Propose(workspace, repair);
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single(document => document.Path.Value == "concepts.play").Text.ShouldContain($"generate {expected}");
        result.Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == Code(identifier)).ShouldBeFalse();
        workspace.Documents.Single(document => document.Path.Value == "concepts.play").Text.ShouldContain($"generate {strategy}");
    }

    [Fact]
    void should_offer_a_unique_constraint_for_one_mapping()
    {
        var workspace = Create("nanoid 1", false, "        produces event Created\n          reference Code = code\n");
        var diagnostic = workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LowGenerationEntropy);
        var repairs = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic);
        repairs.Length.ShouldEqual(2);
        var repair = repairs.Single(repair => repair.Title!.StartsWith("Add", StringComparison.Ordinal));
        repair.CanFixAll.ShouldBeFalse();
        var result = Propose(workspace, repair);
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single(document => document.Path.Value == "commands.play").Text.ShouldContain("unique reference on Created");
        result.Workspace.Compilation.Diagnostics.Any(item => item.Code == DiagnosticCodes.LowGenerationEntropy).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("        produces event Created\n          first Code = code\n          second Code = code\n")]
    [InlineData("        produces event Created\n          reference Code = code\n        produces event Another\n          reference Code = code\n")]
    void should_not_guess_a_unique_target(string productions)
    {
        var workspace = Create("nanoid 1", false, productions);
        var diagnostic = workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LowGenerationEntropy);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).Length.ShouldEqual(1);
    }

    [Fact]
    void should_refuse_a_stale_repair()
    {
        var workspace = Create("bytes 1 hex", true);
        var repair = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LowGeneratedIdentifierEntropy)).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, new()
        {
            ExpectedRevision = default,
            Validation = WorkspaceAuthoringValidation.Authoring,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = repair.RequiredFormatting
        });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.ShouldContain(conflict => conflict.Kind == WorkspaceConflictKind.StaleWorkspaceRevision);
    }

    static string Code(bool identifier) => identifier ? DiagnosticCodes.LowGeneratedIdentifierEntropy : DiagnosticCodes.LowGenerationEntropy;

    static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceDiagnosticRepair repair) => WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair.DiagnosticCode, repair.Subject, new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Formatting = repair.RequiredFormatting,
        Validation = WorkspaceAuthoringValidation.Authoring
    });

    static ScreenplayWorkspace Create(string strategy, bool identifier, string productions = "")
    {
        var concepts = WorkspaceDocument.Create("concepts", PortablePlayPath.Parse("concepts.play"), Encoding.UTF8.GetBytes($"concept Code : String\n  generate {strategy}\n"));
        var commands = WorkspaceDocument.Create("commands", PortablePlayPath.Parse("commands.play"), Encoding.UTF8.GetBytes($"module M\n  feature F\n    slice StateChange S\n      command Create\n        code Code generated{(identifier ? " identifier" : string.Empty)}\n{productions}        returns code\n"));

        return ScreenplayWorkspace.Create("Generation", [concepts, commands], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Generation")));
    }
}
