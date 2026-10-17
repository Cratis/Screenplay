// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes.given;

public class an_unbound_persisted_notes_workspace : a_split_notes_workspace
{
    internal McpManagedFiles Files = null!;
    internal byte[] EarlierState = [];

    void Establish()
    {
        OpenSplit(unbound: true);
        Files = new(Root);
        EarlierState = new McpState("Acme", SemanticIdentityCatalog.Empty(Split.IdentityCatalog.Application), Split.Documents).Serialize();
        McpManagedFiles.WritePrivate(Files.PathFor(McpState.FileName, create: true), EarlierState);
        Workspaces = new(Root);
        Opened = Result(Workspaces.Open(McpJson.Empty));
        Split = McpState.Deserialize(McpState.Serialize(Split)).Open(Root);
        Index = WorkspaceSyntaxIndex.Create(Split);
    }

    internal void Refresh()
    {
        Opened = Result(Workspaces.Open(McpJson.Empty));
        Split = McpState.Deserialize(Files.Read(McpState.FileName)!).Open(Root);
        Index = WorkspaceSyntaxIndex.Create(Split);
    }

    internal JsonElement ReplaceSlice(string source, object[]? retirements = null) => Result(Workspaces.ProposeAst(Arguments(new
    {
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        validation = "Authoring",
        documents = new[] { new { operation = "replace-document", documentId = Split.Documents.Single(document => document.Path.Value == "Orders/PlaceOrder.play").Id.ToString(), source } },
        retiredSemanticAddresses = retirements ?? []
    }),
        source: true));
}
