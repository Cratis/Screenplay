// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_in_a_model_with_structured_values : given.an_authoring_connection
{
    const string Application = """
        domain Repro

        concept OrderId : Uuid
        concept Channel : Enum
          web
          store
        type Line
          sku String
          quantity Int
        """;

    const string Order = """
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
                  lines = [{"sku":"A-1","quantity":2}]
                then OrderPlaced
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  channel = "web"
                  lines = [{"sku":"A-1","quantity":2}]
        """;

    string _orderPath = null!;
    JsonElement _proposal;
    JsonElement _applied;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Application, new UTF8Encoding(false));
        _orderPath = Path.Combine(RootPath, "Shop", "Orders", "PlaceOrder", "PlaceOrder.play");
        Directory.CreateDirectory(Path.GetDirectoryName(_orderPath)!);
        File.WriteAllText(_orderPath, Order, new UTF8Encoding(false));
        Initialize();
    }

    void Because()
    {
        var opened = Result("open-workspace", new { applicationName = "Repro" });
        var revision = opened.GetProperty("revision").GetString();
        var target = Result("read-ast", new { expectedRevision = revision, kind = "ConceptSyntax", name = "Channel" })
            .GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("handle");
        _proposal = Result("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target,
            expectedName = "Channel",
            newName = "SalesChannel",
            validation = "Authoring"
        });
        _applied = Apply(opened, _proposal);
    }

    [Fact] void should_propose_the_rename() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_apply_the_rename() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_rename_the_declaration() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Application.Replace("concept Channel", "concept SalesChannel", StringComparison.Ordinal));
    [Fact] void should_repair_references_and_keep_structured_values_and_comments() => File.ReadAllText(_orderPath).ShouldEqual(Order.Replace("channel Channel", "channel SalesChannel", StringComparison.Ordinal));
}
