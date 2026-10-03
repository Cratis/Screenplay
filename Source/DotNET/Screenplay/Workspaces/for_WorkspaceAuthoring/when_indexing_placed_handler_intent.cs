// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_indexing_placed_handler_intent
{
    const string Slice = "slice StateChange S\n  command C\n    value String\n    handler\n";

    [Theory]
    [InlineData("scoped", "      implementation", "pending")]
    [InlineData("barrels", "      implementation\n        file C.cs", "file")]
    [InlineData("transitive", "      implementation\n        ```csharp\n        return null;\n        ```", "inline")]
    [InlineData("scoped", "      file C.cs", "file")]
    [InlineData("barrels", "      implementation", "pending")]
    [InlineData("transitive", "      implementation\n        file C.cs", "file")]
    public void should_index_original_sources_once_at_their_actual_import_placement(string layout, string body, string state)
    {
        var workspace = Workspace(layout, body);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.ShouldBeEmpty();
        index.Entries.Count(entry => entry.Parent is null).ShouldEqual(workspace.Documents.Length);
        var handler = index.Entries.Single(entry => entry.Node is HandlerSyntax);
        var document = workspace.Documents.Single(document => document.Path.Value == "elsewhere/slice.play");
        handler.Handle.Document.ShouldEqual(document.Id);
        handler.Location.Path.ShouldEqual(document.Path.Value);
        handler.Location.Line.ShouldEqual(4);
        handler.Location.Column.ShouldEqual(5);
        index.Find(handler.Handle).ShouldEqual(handler);
        var intent = WorkspaceImplementationInventory.Create(index).Entries.Single();
        intent.Handle.ShouldEqual(handler.Handle);
        intent.Owner.ShouldEqual(Owner());
        intent.OwnerId.ShouldEqual(SemanticId.Create(Owner()));
        intent.State.ShouldEqual(state);
        intent.IsAmbiguous.ShouldBeFalse();
        var directWorkspace = ScreenplayWorkspace.Create(
            "A",
            [Document("direct.play", "module M\n  feature F\n" + string.Join('\n', (Slice + body).Split('\n').Select(line => "    " + line)))],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var direct = WorkspaceImplementationInventory.Create(directWorkspace).Entries.Single();
        intent.RequirementId.ShouldEqual(direct.RequirementId);
        if (state != "pending") workspace.Compilation.ImplementationRequirements.Single().RequirementId.ShouldEqual(intent.RequirementId);
    }

    [Theory]
    [InlineData("scoped", WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData("barrels", WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData("transitive", WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    public void should_preserve_catalog_identity_and_placed_source_through_typed_rename(string layout, WorkspaceAuthoringFormatting formatting)
    {
        var provisional = Workspace(layout, "      implementation\n        hint \"Keep\"");
        var catalog = SemanticIdentityCatalog.Create(ApplicationIdentity.Create("A"), [], [new(Owner(), SemanticId.Create(Owner()), SemanticIdentityOrigin.Persisted)], []);
        var workspace = ScreenplayWorkspace.Create("A", provisional.Documents, catalog);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var before = WorkspaceImplementationInventory.Create(index).Entries.Single();
        var command = index.Entries.Single(entry => entry.Node is CommandSyntax);
        var renamed = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = catalog.Revision,
            Target = command.Handle,
            ExpectedName = "C",
            NewName = "Renamed",
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = formatting
        });
        renamed.Accepted.ShouldBeTrue();
        var after = WorkspaceImplementationInventory.Create(renamed.Workspace!).Entries.Single();
        after.Owner.Name.ShouldEqual("Renamed");
        after.RequirementId.ShouldEqual(before.RequirementId);
        after.IdentityOrigin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        var source = renamed.Workspace!.Documents.Single(document => document.Path.Value == "elsewhere/slice.play").Text;
        source.StartsWith("slice StateChange S", StringComparison.Ordinal).ShouldBeTrue();
        source.Contains("module M", StringComparison.Ordinal).ShouldBeFalse();
        WorkspaceImplementationInventory.Create(ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(renamed.Workspace))).Entries.Single().RequirementId.ShouldEqual(before.RequirementId);
    }

    [Fact]
    public void should_edit_placed_hint_and_payload_with_original_structural_handles()
    {
        var workspace = Workspace("barrels", "      implementation\n        hint \"Before\"\n        file C.cs");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var hint = index.Entries.Single(entry => entry.Node is ImplementationHintSyntax);
        var file = index.Entries.Single(entry => entry.Node is FileReferenceSyntax);
        var intent = WorkspaceImplementationInventory.Create(index).Entries.Single();
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [
                new ReplaceWorkspaceNode(hint.Handle, hint.Node, ((ImplementationHintSyntax)hint.Node) with { Text = "After" }),
                new ReplaceWorkspaceNode(file.Handle, file.Node, ((FileReferenceSyntax)file.Node) with { Path = "D.cs" })]
        });
        result.Accepted.ShouldBeTrue();
        var after = WorkspaceImplementationInventory.Create(result.Workspace!).Entries.Single();
        after.RequirementId.ShouldEqual(intent.RequirementId);
        after.Hints.Single().ShouldEqual("After");
        after.File.ShouldEqual("D.cs");
        foreach (var original in workspace.Documents.Where(document => document.Id != hint.Handle.Document))
        {
            result.Workspace!.Documents.Single(document => document.Id == original.Id).Bytes.SequenceEqual(original.Bytes).ShouldBeTrue();
        }
    }

    [Fact]
    public void should_not_guess_placement_from_paths_or_index_malformed_imported_documents()
    {
        var workspace = Workspace("scoped", "      implementation\n        hint \" \"");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0493" && diagnostic.Location.Path == "elsewhere/slice.play").ShouldBeTrue();
        WorkspaceImplementationInventory.Create(index).Entries.ShouldBeEmpty();
        var malformed = workspace.Documents.Single(document => document.Path.Value == "elsewhere/slice.play");
        index.Entries.Any(entry => entry.Handle.Document == malformed.Id).ShouldBeFalse();
        var refused = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new RemoveWorkspaceNode(new(workspace.Revision, malformed.Id, "/modules/0/features/0/slices/0/commands/0/handler"), new HandlerSyntax(null, null, SourceLocation.Start))]
        });
        refused.Accepted.ShouldBeFalse();
        var orphan = ScreenplayWorkspace.Create("A", [Document("M/F/orphan.play", Slice + "      implementation")], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        WorkspaceSyntaxIndex.Create(orphan).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0001").ShouldBeTrue();
        WorkspaceImplementationInventory.Create(orphan).Entries.ShouldBeEmpty();
    }

    static SemanticAddress Owner() => SemanticAddress.ForCommand(SemanticAddress.ForSlice(ApplicationIdentity.Create("A"), "M", ["F"], "S"), "C");

    static ScreenplayWorkspace Workspace(string layout, string body)
    {
        WorkspaceDocument[] documents = layout switch
        {
            "scoped" => [Document("root.play", "module M\n  feature F\n    import \"elsewhere/slice.play\"")],
            "barrels" => [
                Document("root.play", "import \"barrels/module.play\""),
                Document("barrels/module.play", "module M\n  import \"feature.play\""),
                Document("barrels/feature.play", "feature F\n  import \"../elsewhere/slice.play\"")],
            _ => [
                Document("root.play", "module M\n  feature F\n    import \"barrels/index.play\""),
                Document("barrels/index.play", "import \"../elsewhere/slice.play\"")]
        };

        return ScreenplayWorkspace.Create("A", [.. documents, Document("elsewhere/slice.play", Slice + body)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(Path.GetFileNameWithoutExtension(path), PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
