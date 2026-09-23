// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class an_order_model_with_comments : an_authoring_connection
{
    internal const string Application = """
        domain Repro

        concept OrderId : Uuid
        concept Channel : Enum
          web
          store
        """;

    internal const string Order = """
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              // @public command PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                channel Channel
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
                  channel = "web"  // @owner sales
                then OrderPlaced
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  channel = "web"
        """;

    internal string OrderPath = null!;
    internal JsonElement Opened;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Application, new UTF8Encoding(false));
        OrderPath = Path.Combine(RootPath, "Shop", "Orders", "PlaceOrder", "PlaceOrder.play");
        Directory.CreateDirectory(Path.GetDirectoryName(OrderPath)!);
        File.WriteAllBytes(OrderPath, Bytes(Order));
        Initialize();
    }

    internal static byte[] Bytes(string source) => [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(source.Replace("\n", "\r\n", StringComparison.Ordinal))];

    internal JsonElement ProposeChannels(string formatting)
    {
        Opened = Result("open-workspace", new { applicationName = "Repro" });
        var revision = Opened.GetProperty("revision").GetString();
        var channels = Result("read-ast", new { expectedRevision = revision, kind = "PropertyMappingSyntax", includeContent = true, limit = 200 })
            .GetProperty("page").GetProperty("items").EnumerateArray()
            .Where(item => item.GetProperty("node").GetProperty("property").GetString() == "channel" &&
                item.GetProperty("node").GetProperty("source").GetProperty("kind").GetString() == "LiteralExpressionSyntax")
            .ToArray();
        return Result("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
            formatting,
            operations = channels.Select(channel =>
            {
                var node = JsonNode.Parse(channel.GetProperty("node").GetRawText())!;
                node["source"]!["value"] = "store";
                return new { operation = "replace", target = channel.GetProperty("handle"), node };
            }).ToArray()
        });
    }
}
