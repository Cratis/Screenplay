// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes.given;

public class a_repeated_split_notes_workspace : a_split_notes_workspace
{
    void Establish()
    {
        OpenSplit(unbound: true);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            domain Acme
            concept OrderId : String
            import "Orders/*.play"
            """);
        File.WriteAllText(Path.Combine(RootPath, "Orders/Orders.play"), """
            module Orders
              feature Fulfilment
                slice StateChange Begin
                  command Begin
                    produces Begun
                  event Begun
            """);
        File.WriteAllText(Path.Combine(RootPath, "Orders/PlaceOrder.play"), """
            module Orders
              feature Fulfilment
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId OrderId identifier
                    reads Stock by orderId
                    produces OrderPlaced
                  event OrderPlaced
                  readmodel Stock
                    orderId OrderId key
                    quantity Int
            """);
        Split = ScreenplayWorkspace.Create("Acme", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
        Index = WorkspaceSyntaxIndex.Create(Split);
        Opened = Result(Workspaces.Open(McpJson.Empty));
    }
}
