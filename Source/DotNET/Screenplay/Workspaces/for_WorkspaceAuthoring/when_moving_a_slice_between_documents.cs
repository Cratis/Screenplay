// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_moving_a_slice_between_documents : Specification
{
    WorkspaceAuthoringResult _result;

    void Because()
    {
        const string header = "module Projects\n  feature Naming\n";
        var documents = new[]
        {
            WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(header + "    slice StateChange Moved\n      event Moved // moved header\n        id \"Original\" // moved pin\n        value String // moved field\n")),
            WorkspaceDocument.Create("destination", PortablePlayPath.Parse("destination.play"), Encoding.UTF8.GetBytes(header + "    slice StateChange Existing\n      event Existing\n    feature Later\n      slice StateView List\n"))
        };
        var workspace = ScreenplayWorkspace.Create("Projects", [.. documents], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var declaration = index.Entries.Single(entry => entry.Node is SliceSyntax && entry.Handle.Document == documents[0].Id);
        var feature = index.Entries.Single(entry => entry.Node is FeatureSyntax named && named.Name == "Naming" && entry.Handle.Document == documents[1].Id);
        _result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Operations = [new MoveWorkspaceNode(declaration.Handle, declaration.Node, feature.Handle, feature.Node, "slices")]
        });
    }

    [Fact] void should_accept() => _result.Conflicts.ShouldBeEmpty();

    [Fact]
    void should_keep_the_destinations_authored_member_order()
    {
        var text = _result.Workspace!.Documents.Single(document => document.StableKey == "destination").Text;
        text.IndexOf("event Existing", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("event Moved", StringComparison.Ordinal));
        text.IndexOf("event Moved", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("feature Later", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("// moved header")]
    [InlineData("// moved pin")]
    [InlineData("// moved field")]
    void should_move_each_comment_once(string comment) => _result.Workspace!.Documents
        .SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens)
        .Count(token => token.Kind == WorkspaceSourceTokenKind.Comment && token.Text == comment).ShouldEqual(1);
}
