// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

[PerFactSpecificationLifecycle]
public class when_migrating_a_document : given.a_connection
{
    const string Screenplay5 =
        """
        layout AppShell
          category Shell
          navigation contributes Navigation
          content
          arrangement flow
            row
              navigation width 240
              content grow

        module Shop
          feature Orders
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
              screen OrderList
                data OrderRow[] via query AllOrders

        """;

    JsonElement _response;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Screenplay5);
        Initialize();
    }

    void Because() => _response = Call("migrate", new { from = "5", document = "application.play" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_migrate_to_the_compilers_version() => _response.GetProperty("to").GetInt32().ShouldEqual(6);
    [Fact] void should_report_the_outcome() => _response.GetProperty("outcome").GetString().ShouldEqual("migrated");
    [Fact] void should_return_the_migrated_source() => _response.GetProperty("files")[0].GetProperty("source").GetString().ShouldContain("  outlet Navigation of navigation in navigation\n");
    [Fact] void should_write_nothing() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Screenplay5);

    [Fact]
    void should_refuse_a_missing_version()
    {
        var response = Call("migrate", new { document = "application.play" });
        response.TryGetProperty("error", out _).ShouldBeTrue();
    }
}
