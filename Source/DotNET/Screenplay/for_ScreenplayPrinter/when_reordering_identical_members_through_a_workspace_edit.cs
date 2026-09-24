// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_reordering_identical_members_through_a_workspace_edit : Specification
{
    const string Source =
        """
        module Shop
          feature Orders
            slice StateChange Place
              command Place
                produces Placed
                  // first tag
                  tag "same" // first tag end
                  // second tag
                  tag "same" // second tag end
              event Placed
        """;

    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        byte[] bytes = [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(Source)];
        var document = WorkspaceDocument.Create("order", PortablePlayPath.Parse("Shop/Orders.play"), bytes);
        var workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(candidate => candidate.Node is ProducesSyntax);
        var produces = (ProducesSyntax)entry.Node;
        _result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, produces, produces with { Tags = [.. produces.Tags.Reverse()] })]
        });
    }

    [Fact] void should_accept_the_ast_edit() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_keep_the_second_tag_with_its_comments() => _result.WritePlan!.Entries.Single().After!.Text.ShouldContain("// second tag\n          tag same // second tag end");
    [Fact] void should_keep_the_first_tag_with_its_comments() => _result.WritePlan!.Entries.Single().After!.Text.ShouldContain("// first tag\n          tag same // first tag end");
    [Fact] void should_print_the_reordered_members() => _result.WritePlan!.Entries.Single().After!.Text.IndexOf("// second tag", StringComparison.Ordinal).ShouldBeLessThan(_result.WritePlan!.Entries.Single().After!.Text.IndexOf("// first tag", StringComparison.Ordinal));
}
