// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_renaming_a_structured_key;

public class and_the_key_is_declared : given.an_authoring_connection
{
    const string Application = """
        domain Repro
        type Line
          sku String
          quantity Int
        """;
    const string Order = """
        module Shop
          feature Orders
            slice StateChange Place
              command Place
                lines Line[]
              specification Placing
                when Place
                  lines = [{"sku":"A-1","quantity":2}]
        """;

    JsonElement _proposal;
    JsonElement _applied;
    string _orderPath = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Application, new UTF8Encoding(false));
        _orderPath = Path.Combine(RootPath, "order.play");
        File.WriteAllText(_orderPath, Order, new UTF8Encoding(false));
        Initialize();
    }

    void Because()
    {
        var opened = Result("open-workspace", new { applicationName = "Repro" });
        var revision = opened.GetProperty("revision").GetString();
        var target = Result("read-ast", new { expectedRevision = revision, kind = "PropertySyntax", name = "sku" })
            .GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("handle");
        _proposal = Result("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target,
            expectedName = "sku",
            newName = "stockCode",
            validation = "Authoring"
        });
        _applied = Apply(opened, _proposal);
    }

    [Fact] void should_propose_the_rename() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_apply_the_rename() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_rewrite_the_key() => File.ReadAllText(_orderPath).Contains("\"stockCode\":\"A-1\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_unrelated_keys() => File.ReadAllText(_orderPath).Contains("\"quantity\":2", StringComparison.Ordinal).ShouldBeTrue();
}
