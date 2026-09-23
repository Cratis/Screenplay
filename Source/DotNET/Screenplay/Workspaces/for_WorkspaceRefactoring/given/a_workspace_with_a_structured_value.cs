// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_workspace_with_a_structured_value : a_refactoring_workspace
{
    protected const string StructuredValue = "[{\"sku\":\"A-1\",\"quantity\":2}]";

    protected const string ApplicationSource =
        """
        domain Repro

        concept OrderId : Uuid
        concept Channel : Enum
          web
          store
        type Line
          sku String
          quantity Int
        """;

    protected const string OrderSource =
        """
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              // @public command PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                channel Channel
                lines Line[]
                produces OrderPlaced
                  for orderId
                  orderId = orderId
                  channel = channel
                  lines = lines
              event OrderPlaced
                orderId OrderId
                channel Channel
                lines Line[]
              specification PlacingAnOrder
                when PlaceOrder
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  channel = "web"
                  lines = {value}
                then OrderPlaced
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  channel = "web"
                  lines = [{"sku":"A-1","quantity":2}]
        """;

    protected WorkspaceDocument Order = null!;

    void Establish() => CreateWith(StructuredValue);

    protected void CreateWith(string value)
    {
        Concepts = Document("application", "application.play", ApplicationSource);
        Order = Document("order", "Shop/Orders/PlaceOrder/PlaceOrder.play", OrderSource.Replace("{value}", value, StringComparison.Ordinal));
        Workspace = ScreenplayWorkspace.Create("Repro", [Concepts, Order], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Repro")));
    }

    protected WorkspaceAuthoringResult RenameChannel() => Workspace.ProposeRename(Rename<ConceptSyntax>("Channel", "SalesChannel"));
}
