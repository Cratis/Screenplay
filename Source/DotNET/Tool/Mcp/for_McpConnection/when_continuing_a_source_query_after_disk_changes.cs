// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_continuing_a_source_query_after_disk_changes : given.a_connection
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _missingRevision;
    JsonElement _stale;

    void Establish()
    {
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n    slice StateView Browse\n      screen List\n");
        Initialize();
    }

    void Because()
    {
        _first = Call("find-assertion-gaps", new { limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var expectedSourceRevision = _first.GetProperty("sourceRevision").GetString();
        _missingRevision = Call("find-assertion-gaps", new { offset = 1, limit = 1 });
        _next = Call("find-assertion-gaps", new { offset = 1, limit = 1, expectedSourceRevision }).GetProperty("result").GetProperty("structuredContent");
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// External source edit\n");
        _stale = Call("find-assertion-gaps", new { offset = 1, limit = 1, expectedSourceRevision }).GetProperty("result");
    }

    [Fact] void should_have_two_matching_slices() => _first.GetProperty("page").GetProperty("matched").GetInt32().ShouldEqual(2);
    [Fact] void should_return_the_second_page_for_the_same_source() => _next.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_require_revision_on_continuation() => _missingRevision.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_continuation_after_source_drift() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
