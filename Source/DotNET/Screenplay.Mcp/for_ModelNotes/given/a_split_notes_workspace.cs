// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes.given;

public class a_split_notes_workspace : for_McpConnection.given.a_connection
{
    internal McpWorkspaces Workspaces = null!;
    internal ScreenplayWorkspace Split = null!;
    internal JsonElement Opened;
    internal WorkspaceSyntaxIndex Index = null!;

    internal void OpenSplit(bool unbound = false)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            domain Acme
            concept OrderId : String
            import "Orders/Orders.play"
            """);
        Directory.CreateDirectory(Path.Combine(RootPath, "Orders"));
        File.WriteAllText(Path.Combine(RootPath, "Orders/Orders.play"), """
            module Orders
              feature Fulfilment
                import "PlaceOrder.play"
            """);
        File.WriteAllText(Path.Combine(RootPath, "Orders/PlaceOrder.play"), $$"""
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                {{(unbound ? "reads Stock by orderId" : string.Empty)}}
                produces OrderPlaced
              event OrderPlaced
              readmodel Stock
                orderId OrderId key
                quantity Int
            """);
        Split = ScreenplayWorkspace.Create("Acme", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
        Index = WorkspaceSyntaxIndex.Create(Split);
        Workspaces = new(Root);
        Opened = Result(Workspaces.Open(Arguments(new { applicationName = "Acme" })));
    }

    internal JsonElement[] Targets(SemanticKind kind, bool typed = true)
    {
        var entry = Index.Entries.First(entry => entry.Address?.Kind == kind);
        var dotted = string.Join('.', entry.Address!.Parts.Skip(1).Select(part => part.Key));
        var targets = new List<JsonElement> { Arguments(new { address = dotted, kind = kind.ToString() }), Arguments(McpAstHandles.Describe(entry.Handle)) };
        if (typed) targets.Add(Arguments(McpSemanticAddresses.Describe(entry.Address)));
        return [.. targets];
    }

    internal JsonElement ProposeNotes(object[] operations) => Result(Workspaces.ProposeNotes(Arguments(new
    {
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        expectedNotesRevision = Opened.GetProperty("notesRevision").GetString(),
        operations
    })));

    internal JsonElement ProposeNote(JsonElement target) => ProposeNotes([new { operation = "add", kind = "question", text = "Should placing an order require stock?", author = "team", target }]);

    internal JsonElement Review(JsonElement proposal) => Result(Workspaces.ReadProposal(Arguments(new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "notes" }))).GetProperty("result").GetProperty("page").GetProperty("items")[0].GetProperty("after");

    internal void Apply(JsonElement proposal) => _ = Workspaces.Apply(Arguments(new
    {
        proposalId = proposal.GetProperty("proposalId").GetString(),
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString()
    }));

    internal static JsonElement Arguments(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);
    internal static JsonElement Result(object value) => JsonSerializer.SerializeToElement(value).GetProperty("structuredContent");
}
