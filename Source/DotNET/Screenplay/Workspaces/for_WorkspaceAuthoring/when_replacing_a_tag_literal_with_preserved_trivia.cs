// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_replacing_a_tag_literal_with_preserved_trivia : Specification
{
    const string Source = "// Café 🍰\r\nmodule Shop\r\n  feature Orders\r\n    slice Register\r\n      event Registered\r\n        tag audit  // keep this\r\n";
    ScreenplayWorkspace _workspace = null!;
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("orders", PortablePlayPath.Parse("Shop/Orders.play"), Bytes(Source));
        _workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    }

    void Because()
    {
        var entry = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Node is LiteralExpressionSyntax { Value: "audit" });
        _result = _workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, ((LiteralExpressionSyntax)entry.Node) with { Value = "billing" })]
        });
    }

    [Fact] void should_accept_the_edit() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_preserve_the_bom_comments_and_other_bytes() => _result.Workspace!.Documents.Single().Bytes.AsSpan().SequenceEqual(Bytes(Source.Replace("tag audit", "tag billing", StringComparison.Ordinal))).ShouldBeTrue();

    static byte[] Bytes(string text) => [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(text)];
}
