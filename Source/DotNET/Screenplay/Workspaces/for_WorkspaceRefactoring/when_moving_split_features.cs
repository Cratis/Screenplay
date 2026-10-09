// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_split_features : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Workspace = ScreenplayWorkspace.Create("Projects",
            [Document("one", "One.play", "module Projects\n  feature Registration\n    slice StateChange One\n      command CreateOne"),
             Document("two", "Two.play", "module Projects\n  feature Registration\n    slice StateChange Two\n      command CreateTwo"),
             Document("destination", "Archive.play", "module Archive\n  feature Old")],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Registration"),
        NewParent = SemanticAddress.ForModule(Workspace.IdentityCatalog.Application, "Archive"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_accept_every_fragment_together() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_report_both_fragments() => _result.MoveReport.FragmentsMoved.Length.ShouldEqual(2);
    [Fact] void should_preserve_every_assigned_identity() => _result.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
    [Fact] void should_preserve_both_physical_files() => _result.Workspace.Documents.Select(document => document.Path).ShouldContainOnly(Workspace.Documents.Select(document => document.Path));
    [Fact] void should_move_both_logical_feature_fragments() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Count(entry => entry.Address?.Equals(SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Archive", "Registration")) == true).ShouldEqual(2);
    [Fact] void should_remove_the_redundant_empty_wrapper_fragment() => _result.Workspace.Documents.Single(document => document.Path.Value == "One.play").Text.ShouldNotContain("module Projects");
    [Fact] void should_keep_the_old_logical_module() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<ModuleSyntax>().Any(module => module.Name == "Projects").ShouldBeTrue();
}
