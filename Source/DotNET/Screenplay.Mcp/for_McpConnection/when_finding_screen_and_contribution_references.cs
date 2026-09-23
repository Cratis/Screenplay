// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_screen_and_contribution_references : given.a_connection
{
    JsonElement _view;
    JsonElement _point;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Sales
              screen template Shell
                navbar contributes Navigation
                main
              contribute to Navigation
                navigate to OrdersList
              feature Orders
                slice StateView List
                  readmodel Orders
                    title String
                  query GetOrders => Orders[]
                  screen OrdersList
                    data Orders via query GetOrders
            """);
        Initialize();
    }

    void Because()
    {
        _view = Call("find-references", new { address = "Sales.Orders.List.Orders", kind = "ReadModel" }).GetProperty("result").GetProperty("structuredContent");
        _point = Call("find-references", new { address = "Sales.Navigation", kind = "ContributionPoint" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_compile_the_source() => _view.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_include_both_query_return_and_screen_data_references() => _view.GetProperty("references").GetArrayLength().ShouldEqual(2);
    [Fact] void should_resolve_the_contribution_point() => _point.GetProperty("references").GetArrayLength().ShouldEqual(1);
}
