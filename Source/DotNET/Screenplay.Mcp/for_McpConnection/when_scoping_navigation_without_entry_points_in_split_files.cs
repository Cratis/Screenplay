// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_navigation_without_entry_points_in_split_files : given.a_connection
{
    JsonElement _response;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module Other\n  description \"First document\"");
        File.WriteAllText(Path.Combine(RootPath, "b.play"), """
            module M
              feature F
                slice StateView View
                  screen Home
                    title "Home"
            """);
        Initialize();
    }

    void Because() => _response = Call("diagnostics", new { scope = "M.F.View", checks = "navigation" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_retain_the_global_warning_in_the_scope() => _response.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldEqual(1);
    [Fact] void should_include_the_finding_in_the_page() => _response.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_report_the_navigation_code() => _response.GetProperty("page").GetProperty("items")[0].GetProperty("code").GetString().ShouldEqual("PLAY0537");
}
