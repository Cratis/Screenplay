// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_moving_unbound_noted_containers : for_McpConnection.given.a_connection
{
    const string UnboundSource = """
        module Orders
          feature Fulfilment
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId String identifier
                reads Stock by orderId
                produces OrderPlaced
              event OrderPlaced
              readmodel Stock
                orderId String key
        module Shipping
          feature Pending
        """;

    McpWorkspaces _workspaces = null!;
    McpManagedFiles _files = null!;
    ScreenplayWorkspace _workspace = null!;
    JsonElement _opened;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), UnboundSource);
        _workspaces = new(Root);
        _files = new(Root);
        _opened = Result(_workspaces.Open(Arguments(new { applicationName = "Acme" })));
        _workspace = ScreenplayWorkspace.Create("Acme", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
    }

    [Theory]
    [InlineData(SemanticKind.Slice, SemanticKind.Feature)]
    [InlineData(SemanticKind.Feature, SemanticKind.Module)]
    public void should_carry_identity_and_notes_through_move_reopen_and_first_binding(SemanticKind kind, SemanticKind parentKind)
    {
        var assignment = _workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == kind && assignment.Address.Name == (kind == SemanticKind.Slice ? "PlaceOrder" : "Fulfilment"));
        var note = Result(_workspaces.ProposeNotes(Arguments(new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(), expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(), expectedNotesRevision = _opened.GetProperty("notesRevision").GetString(),
            operations = new[] { new { operation = "add", kind = "note", text = "Ready?", author = "team", anchor = new { semanticId = assignment.Id.ToString() } } }
        })));
        Apply(note);
        var notes = _files.Read(ModelNotesStore.FileName);
        var parent = _workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == parentKind && assignment.Address.Name == (parentKind == SemanticKind.Feature ? "Pending" : "Shipping"));
        var moved = Result(_workspaces.Move(Arguments(new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(), expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            target = McpSemanticAddresses.Describe(assignment.Address), newParent = McpSemanticAddresses.Describe(parent.Address), formatting = "CanonicalizeTouchedDocuments"
        })));
        Assert.True(moved.GetProperty("success").GetBoolean(), moved.GetRawText());
        moved.GetProperty("orphanedNotes").GetArrayLength().ShouldEqual(0);
        Apply(moved);
        _workspace.Compilation.Success.ShouldBeFalse();
        var carried = _workspace.IdentityCatalog.Semantics.Single(current => current.Id == assignment.Id);
        carried.Address.Parts.Any(part => part.Key == "Shipping").ShouldBeTrue();
        carried.Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        var source = _workspace.Documents.Single().Text.Replace("reads Stock by orderId", string.Empty, StringComparison.Ordinal);
        var fixedModel = Result(_workspaces.ProposeAst(Arguments(new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(), expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "replace-document", documentId = _workspace.Documents.Single().Id.ToString(), source } }
        }),
            source: true));
        Assert.True(fixedModel.GetProperty("success").GetBoolean(), fixedModel.GetRawText());
        Apply(fixedModel);
        _workspace.Compilation.Success.ShouldBeTrue();
        _workspace.IdentityCatalog.Semantics.ShouldContain(carried);
        McpManagedFiles.Equal(_files.Read(ModelNotesStore.FileName), notes).ShouldBeTrue();
        ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(notes, _workspace.IdentityCatalog.Application).Document!, _workspace).Single().Address!.Parts.Any(part => part.Key == "Shipping").ShouldBeTrue();
    }

    static JsonElement Arguments(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);
    static JsonElement Result(object value) => JsonSerializer.SerializeToElement(value).GetProperty("structuredContent");

    void Apply(JsonElement proposal)
    {
        var applied = Result(_workspaces.Apply(Arguments(new
        {
            proposalId = proposal.GetProperty("proposalId").GetString(), expectedRevision = _opened.GetProperty("revision").GetString(), expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString()
        })));
        Assert.True(applied.GetProperty("success").GetBoolean(), applied.GetRawText());
        _opened = Result(_workspaces.Open(McpJson.Empty));
        _workspace = McpState.Deserialize(_files.Read(McpState.FileName)!).Open(Root);
    }
}
