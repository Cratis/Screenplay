// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_repairs_for_a_legacy_model : given.an_authoring_connection
{
    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    void should_page_verified_occurrences_and_one_complete_document_migration(int count)
    {
        var types = "type Details\n" + string.Join('\n', Enumerable.Range(0, count).Select(number => $"  value{number} String?"));
        File.WriteAllText(Path.Combine(RootPath, "application.play"), types + "\n" + Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var watch = Stopwatch.StartNew();
        var first = Result("read-workspace", new { expectedRevision = revision, view = "repairs", limit = 1 });
        var cold = watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        var last = Result("read-workspace", new { expectedRevision = revision, view = "repairs", offset = count, limit = 1 });
        var warm = watch.Elapsed.TotalMilliseconds;
        Console.WriteLine($"MCP repairs: {count} legacy occurrences; cold {cold:F2} ms; cached {warm:F2} ms");
        first.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(count + 1);
        var document = last.GetProperty("page").GetProperty("items").EnumerateArray().Single();
        document.GetProperty("scope").GetString().ShouldEqual("document");
        document.GetProperty("operations").GetArrayLength().ShouldEqual(count);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(types + "\n" + Source);
    }
}
