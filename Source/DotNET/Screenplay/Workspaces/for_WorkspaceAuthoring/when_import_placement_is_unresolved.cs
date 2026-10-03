// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_import_placement_is_unresolved
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_not_choose_a_semantic_owner_from_conflicting_sibling_imports(bool transitive)
    {
        var application = ApplicationIdentity.Create("A");
        var owner = SemanticAddress.ForCommand(SemanticAddress.ForSlice(application, "M", ["F"], "S"), "C");
        var catalog = SemanticIdentityCatalog.Create(application, [], [new(owner, SemanticId.Create(owner), SemanticIdentityOrigin.Persisted)], []);
        var target = transitive ? "barrel.play" : "slice.play";
        var documents = new List<WorkspaceDocument>
        {
            Document("root.play", $"module M\n  feature F\n    import \"{target}\"\n  feature G\n    import \"{target}\"\n"),
            Document("slice.play", "slice StateChange S\n  command C\n    handler\n      implementation\n        hint \"Keep\"\n"),
            Document("valid.play", "module Other\n  feature F\n    slice StateChange S\n      command C\n        handler\n          implementation\n")
        };
        if (transitive) documents.Add(Document("barrel.play", "import \"slice.play\""));
        var workspace = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(ScreenplayWorkspace.Create("A", [.. documents], catalog)));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ConflictingImportPlacement).ShouldBeTrue();
        var slice = workspace.Documents.Single(document => document.Path.Value == "slice.play");
        index.Entries.Any(entry => entry.Handle.Document == slice.Id).ShouldBeFalse();
        index.UnresolvedPlacementDocuments.Any(document => document.Id == slice.Id).ShouldBeTrue();
        var inventory = WorkspaceImplementationInventory.Create(index);
        inventory.UnresolvedPlacementDocuments.Any(document => document.Id == slice.Id).ShouldBeTrue();
        inventory.Entries.Single().Owner.Parts.Single(part => part.Kind == SemanticAddressPartKind.Module).Key.ShouldEqual("Other");
        inventory.Entries.Any(entry => entry.Owner.Equals(owner) || entry.OwnerId == SemanticId.Create(owner)).ShouldBeFalse();
        workspace.IdentityCatalog.Semantics.Single().Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        workspace.Compilation.Success.ShouldBeFalse();
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
