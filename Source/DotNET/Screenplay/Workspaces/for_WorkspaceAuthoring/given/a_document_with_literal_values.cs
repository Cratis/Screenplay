// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.given;

public class a_document_with_literal_values : Specification
{
    protected const string OrderSource =
        """
        // Orders keep their channel: channel = "web"
        concept OrderId : Uuid
        concept Channel : String
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              // @public command PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                channel Channel
                quantity Int
                express Bool
                note String?
                remark String?
                comment String?
                produces OrderPlaced
                  for orderId
                  orderId = orderId
                  channel = channel
              event OrderPlaced
                orderId OrderId
                channel Channel
              specification PlacingAnOrder
                when PlaceOrder
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  channel = "web"  // web is the default channel
                  quantity =   2
                  express = true
                  note = null
                  remark = "fragile"  // fragile goods ship separately
                then OrderPlaced
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  channel = "web"
        """;

    protected ScreenplayWorkspace Workspace = null!;
    protected WorkspaceDocument Order = null!;
    protected WorkspaceAuthoringResult Result = null!;

    void Establish() => Create(OrderSource);

    protected void Create(string source)
    {
        Order = WorkspaceDocument.Create("order", PortablePlayPath.Parse("Shop/Orders.play"), Bytes(source));
        Workspace = ScreenplayWorkspace.Create("Shop", [Order], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    }

    protected static byte[] Bytes(string source) =>
        [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(source.Replace("\n", "\r\n", StringComparison.Ordinal))];

    protected WorkspaceSyntaxEntry Value(string property, int occurrence = 0) =>
        WorkspaceSyntaxIndex.Create(Workspace).Entries
            .Where(entry => entry.Node is PropertyMappingSyntax mapping && mapping.Property == property && entry.Handle.Path.Contains("/specifications/", StringComparison.Ordinal))
            .ElementAt(occurrence);

    protected WorkspaceAuthoringResult Propose(params WorkspaceAstOperation[] operations) => Workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
        Operations = [.. operations]
    });

    protected WorkspaceAstOperation ReplaceSource(string property, ExpressionSyntax source, int occurrence = 0)
    {
        var entry = Value(property, occurrence);
        return new ReplaceWorkspaceNode(entry.Handle, entry.Node, ((PropertyMappingSyntax)entry.Node) with { Source = source });
    }

    protected byte[] Candidate() => [.. Result.Workspace!.Documents.Single().Bytes];
}
