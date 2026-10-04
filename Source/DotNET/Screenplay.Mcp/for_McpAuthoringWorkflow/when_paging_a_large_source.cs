// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_paging_a_large_source : given.an_authoring_connection
{
    [Fact]
    void should_disclose_oversized_metadata_without_dumping_or_truncating_it()
    {
        var description = string.Join('\n', Enumerable.Repeat(new string('x', 720), 800));
        var pin = new string('p', 6000);
        var fenced = description.Replace("\n", "\n    ", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), $"eventsource Account\n  description\n    ```text\n    {fenced}\n    ```\n  stream Transactions\n    id \"{pin}\"\n");
        Initialize();
        var revision = Open().GetProperty("revision").GetString();
        var source = Page("event-sources", revision)[0];
        var key = source.GetProperty("authoringKey").GetString();
        var header = Result("read-workspace", new { expectedRevision = revision, view = "event-source-details", authoringKey = key, limit = 1 }).GetProperty("page").GetProperty("items")[0];
        header.GetProperty("description").ValueKind.ShouldEqual(System.Text.Json.JsonValueKind.Null);
        header.GetProperty("declaration").GetProperty("metadata").GetProperty("descriptionBytes").GetInt32().ShouldEqual(Encoding.UTF8.GetByteCount(description));
        header.GetProperty("originalSource").GetProperty("tool").GetString().ShouldEqual("read-document");
        var child = Page("event-streams", revision)[0];
        child.GetProperty("id").ValueKind.ShouldEqual(System.Text.Json.JsonValueKind.Null);
        child.GetProperty("metadata").GetProperty("idBytes").GetInt32().ShouldEqual(6000);
        var bytes = Result("read-document", new { path = "application.play", limit = 64 });
        bytes.GetRawText().Length.ShouldBeLessThan(4096);
    }

    [Fact]
    void should_page_compact_headers_and_every_child_once_with_a_bounded_wire_response()
    {
        var source = "eventsource Account\n" + string.Concat(Enumerable.Range(0, 1500).Select(index => $"  stream S{index:D4}\n    description \"{new string('x', 720)}\"\n"));
        Encoding.UTF8.GetByteCount(source).ShouldBeLessThan(2 * 1024 * 1024);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var catalogRevision = opened.GetProperty("catalogRevision").GetString();
        var key = Page("event-sources", revision)[0].GetProperty("authoringKey").GetString();
        var seen = new List<string>();
        var offset = 0;
        do
        {
            var response = Call("read-workspace", new { expectedRevision = revision, view = "event-source-details", authoringKey = key, expectedCatalogRevision = catalogRevision, offset, limit = offset == 0 ? 1 : 200 });
            Encoding.UTF8.GetByteCount(response.GetRawText()).ShouldBeLessThan(McpJson.MaximumStructuredResponseBytes);
            response.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
            var page = response.GetProperty("result").GetProperty("structuredContent").GetProperty("page");
            page.GetProperty("revision").GetString().ShouldEqual(revision);
            foreach (var item in page.GetProperty("items").EnumerateArray())
            {
                if (item.GetProperty("kind").GetString() == "stream") seen.Add(item.GetProperty("declaration").GetProperty("name").GetString()!);
                else item.TryGetProperty("syntax", out _).ShouldBeFalse();
            }
            offset = page.GetProperty("nextOffset").ValueKind == System.Text.Json.JsonValueKind.Null ? -1 : page.GetProperty("nextOffset").GetInt32();
        }
        while (offset >= 0);
        seen.ToArray().ShouldEqual([.. Enumerable.Range(0, 1500).Select(index => $"S{index:D4}")]);
    }
}
