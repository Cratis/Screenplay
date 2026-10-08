// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_scoped_completeness_warnings : given.a_connection
{
    JsonElement _first;
    JsonElement _second;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module M
              feature F
                slice StateView View
                  readmodel R
                    value String
                  query Q => R[]
                  query Q2 => R[]
                  screen S
                    data R via query Q
                    data R via query Q2
            """);
        Initialize();
    }

    void Because()
    {
        _first = Call("diagnostics", new { scope = "M.F.View", checks = "data-bindings", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _second = Call("diagnostics", new { scope = "M.F.View", checks = "data-bindings", offset = 1, limit = 1, expectedSourceRevision = _first.GetProperty("sourceRevision").GetString() }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_count_all_findings_before_paging() => _first.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldEqual(3);
    [Fact] void should_bound_the_first_page() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_keep_the_source_revision() => _second.GetProperty("sourceRevision").GetString().ShouldEqual(_first.GetProperty("sourceRevision").GetString());
    [Fact] void should_page_the_merged_findings() => _second.GetProperty("page").GetProperty("offset").GetInt32().ShouldEqual(1);
}
