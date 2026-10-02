// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_discovering_legacy_optionality_repairs
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    void should_verify_once_per_document_independent_of_occurrence_count(int count)
    {
        var source = "type Details\n" + string.Join('\n', Enumerable.Range(0, count).Select(number => $"  value{number} String?"));
        var workspace = Workspace(source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostics = index.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ToArray();
        diagnostics.Length.ShouldEqual(count);
        var repairs = diagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic)).ToArray();
        repairs.Length.ShouldEqual(count);
        var root = index.Entries.Single(entry => entry.Node is ApplicationSyntax).Handle;
        WorkspaceDiagnosticRepairs.FindDocumentOptionality(index, root).Single().Operations.Length.ShouldEqual(count);

        // A new request/index for the same snapshot reuses the compact verdict, never a candidate.
        var secondIndex = WorkspaceSyntaxIndex.Create(workspace);
        WorkspaceDiagnosticRepairs.FindDocumentOptionality(secondIndex, root).Length.ShouldEqual(1);
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(1);
        foreach (var repair in new[] { repairs[0], repairs[^1] })
        {
            WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace)).Accepted.ShouldBeTrue();
        }

        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(3);
        var newer = Workspace(source);
        WorkspaceDiagnosticRepairs.FindDocumentOptionality(WorkspaceSyntaxIndex.Create(newer), root).Length.ShouldEqual(1);
        WorkspaceProductionRepairs.TransactionCount(newer).ShouldEqual(1);
    }

    [Fact]
    void should_exclude_the_only_unambiguous_observable_spelling_from_all_recipes()
    {
        var workspace = Workspace("module M\n  feature F\n    slice StateView S\n      query Q => observable?");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldBeFalse();
        var root = index.Entries.Single(entry => entry.Node is ApplicationSyntax).Handle;
        WorkspaceDiagnosticRepairs.FindDocumentOptionality(index, root).ShouldBeEmpty();
        var subject = index.Entries.Single(entry => entry.Node is TypeRefSyntax).Handle;
        var proposal = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.LegacyOptionalSuffix, subject, Request(workspace));
        proposal.Accepted.ShouldBeFalse();
        proposal.Workspace.ShouldBeNull();
        proposal.WritePlan.ShouldBeNull();
        proposal.Conflicts.ShouldNotBeEmpty();
        proposal.Conflicts.All(conflict => conflict.Kind == WorkspaceConflictKind.UnknownRepair).ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(0);
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_migrate_other_types_without_changing_the_ambiguous_observable_return(WorkspaceAuthoringFormatting formatting)
    {
        const string source = "module M\n  feature F\n    slice StateView S\n      query Q => observable?\n        filter note String?";
        var workspace = Workspace(source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix);
        diagnostic.Location.Line.ShouldEqual(5);
        var occurrence = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        var root = index.Entries.Single(entry => entry.Node is ApplicationSyntax).Handle;
        var document = WorkspaceDiagnosticRepairs.FindDocumentOptionality(index, root).Single();
        foreach (var repair in new[] { occurrence, document })
        {
            repair.Operations.Length.ShouldEqual(1);
            var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace) with { Formatting = formatting });
            result.Accepted.ShouldBeTrue();
            var text = result.Workspace!.Documents.Single().Text;
            text.ShouldContain("query Q => observable?");
            text.ShouldContain("filter note String optional");
            result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldBeFalse();
        }
    }

    [Fact]
    void should_migrate_both_reaction_values_when_one_is_named_csharp()
    {
        const string source = "module M\n  feature F\n    slice Automation S\n      reaction R\n        when T\n          csharp String?\n          note String?";
        var workspace = Workspace(source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var root = index.Entries.Single(entry => entry.Node is ApplicationSyntax).Handle;
        var repair = WorkspaceDiagnosticRepairs.FindDocumentOptionality(index, root).Single();
        repair.Operations.Length.ShouldEqual(2);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldEqual(source.Replace("String?", "String optional", StringComparison.Ordinal));
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldBeFalse();
    }

    [Fact]
    void should_explain_shadowed_query_keys_instead_of_offering_a_partial_document_migration()
    {
        var workspace = Workspace("module M\n  feature F\n    slice StateView S\n      query Q => View\n        by first Uuid?\n        by second Uuid?");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ShouldEqual(2);
        index.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidQueryParameter && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
        foreach (var diagnostic in index.Diagnostics)
        {
            WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).ShouldBeEmpty();
        }

        var root = new WorkspaceNodeHandle(workspace.Revision, workspace.Documents.Single().Id, string.Empty);
        WorkspaceDiagnosticRepairs.FindDocumentOptionality(index, root).ShouldBeEmpty();
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
