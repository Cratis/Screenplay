// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_all_feature_fragments : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var first = Document("first", "first.play", "module Projects\n  feature Registration\n    slice StateChange One\n      command CreateOne");
        var second = Document("second", "second.play", "module Projects\n  feature Registration\n    slice StateChange Two\n      command CreateTwo");
        var third = Document("third", "third.play", "module Projects\n  feature Registration\n    slice StateChange Three\n      command CreateThree");
        Workspace = ScreenplayWorkspace.Create("Projects", [first, second, third], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<FeatureSyntax>("Registration", "Enrollment"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_every_fragment() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<FeatureSyntax>().Count(feature => feature.Name == "Enrollment").ShouldEqual(3);
    [Fact] void should_write_three_documents() => _result.WritePlan.Entries.Length.ShouldEqual(3);
    [Fact] void should_preserve_every_assigned_descendant_identity() => _result.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ToHashSet().SetEquals(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id)).ShouldBeTrue();
}
