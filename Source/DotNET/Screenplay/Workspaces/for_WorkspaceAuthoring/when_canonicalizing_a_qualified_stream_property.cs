// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_canonicalizing_a_qualified_stream_property
{
    [Fact]
    void should_not_manufacture_routing_when_a_typed_command_edit_has_no_source_escape_metadata()
    {
        const string Source = "import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        @stream Account.Transactions";
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is CommandSyntax);
        var replacement = (CommandSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(entry.Node));
        replacement.Properties.Single().NameWasEscaped.ShouldBeFalse();
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, replacement with { Description = "Edited" })]
        });
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldContain("@stream Account.Transactions");
        var command = WorkspaceSyntaxIndex.Create(result.Workspace).Entries.Select(entry => entry.Node).OfType<CommandSyntax>().Single();
        command.Stream.ShouldBeNull();
        SyntaxJson.StructurallyEqual(replacement with { Description = "Edited" }, command).ShouldBeTrue();
    }
}
