// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_editing_the_first_position_of_a_placed_fragment : Specification
{
    const string Slices = "slice StateChange First // first header\n  event FirstHappened\n// between slices\nslice StateChange Second // second header\n  event SecondHappened\n";

    [Fact]
    void should_print_a_moved_slice_first()
    {
        var text = Fragment(Propose(index => Move(index, "Second")));
        text.IndexOf("slice StateChange Second", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("slice StateChange First", StringComparison.Ordinal));
    }

    [Fact]
    void should_print_an_inserted_slice_first()
    {
        var text = Fragment(Propose(Insert));
        text.IndexOf("slice StateChange Added", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("slice StateChange First", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("// first header")]
    [InlineData("// between slices")]
    [InlineData("// second header")]
    void should_keep_every_comment_once_through_a_move(string comment) => Propose(index => Move(index, "Second")).Workspace!.Documents
        .SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens)
        .Count(token => token.Kind == WorkspaceSourceTokenKind.Comment && token.Text == comment).ShouldEqual(1);

    static ScreenplayWorkspace Create() => ScreenplayWorkspace.Create("Placed",
        [
            WorkspaceDocument.Create("root", PortablePlayPath.Parse("root.play"), Encoding.UTF8.GetBytes("module M\n  feature F\n    import \"slices.play\"\n")),
            WorkspaceDocument.Create("slices", PortablePlayPath.Parse("slices.play"), Encoding.UTF8.GetBytes(Slices))
        ],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Placed")));

    static string Fragment(WorkspaceAuthoringResult result)
    {
        result.Conflicts.ShouldBeEmpty();
        return result.Workspace!.Documents.Single(document => document.StableKey == "slices").Text;
    }

    static WorkspaceAuthoringResult Propose(Func<WorkspaceSyntaxIndex, WorkspaceAstOperation> operation)
    {
        var workspace = Create();
        return workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [operation(WorkspaceSyntaxIndex.Create(workspace))]
        });
    }

    static WorkspaceSyntaxEntry Owner(WorkspaceSyntaxIndex index) => index.Entries.Single(entry => entry.Node is FeatureSyntax { IsPlacement: true });

    static WorkspaceAstOperation Move(WorkspaceSyntaxIndex index, string name)
    {
        var slice = index.Entries.Single(entry => entry.Node is SliceSyntax named && named.Name == name);
        var owner = Owner(index);
        return new MoveWorkspaceNode(slice.Handle, slice.Node, owner.Handle, owner.Node, "slices", 0);
    }

    static WorkspaceAstOperation Insert(WorkspaceSyntaxIndex index)
    {
        var owner = Owner(index);
        var existing = (SliceSyntax)index.Entries.Single(entry => entry.Node is SliceSyntax named && named.Name == "Second").Node;
        return new AddWorkspaceNode(owner.Handle, owner.Node, "slices", existing with { Name = "Added", Events = [] }, 0);
    }
}
