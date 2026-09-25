// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_paging_at_the_limit_bounds : given.an_export
{
    JsonElement _schema;
    JsonElement _page;
    JsonElement _documents;
    string _oversizedBytes = null!;
    string _oversizedItems = null!;

    void Establish()
    {
        var largeSource = new StringBuilder("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n");
        for (var index = 0; index < 1100; index++) largeSource.Append("        property").Append(index).Append(" String\n");
        File.WriteAllText(Path.Combine(RootPath, "application.play"), largeSource.ToString());
        Initialize();
        var revision = Content("open-workspace", new { }).GetProperty("revision").GetString();
        var available = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 1 });
        if (!available.GetProperty("available").GetBoolean()) throw new InvalidOperationException(Content("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" }).GetRawText());
        Revision = revision!;
    }

    string Revision = null!;

    void Because()
    {
        _schema = ReadSchema();
        _page = Content("read-workspace", new { expectedRevision = Revision, view = "executable-model", limit = 192 * 1024 });
        _oversizedBytes = Refusal(new { expectedRevision = Revision, view = "executable-model", limit = (192 * 1024) + 1 });
        _oversizedItems = Refusal(new { expectedRevision = Revision, view = "documents", limit = 201 });
        _documents = Content("read-workspace", new { expectedRevision = Revision, view = "documents", limit = 200 });
    }

    [Fact] void should_advertise_byte_limit() => _schema.GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(192 * 1024);
    [Fact] void should_advertise_model_byte_limit() => _schema.GetProperty("then").GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(192 * 1024);
    [Fact] void should_advertise_item_limit() => _schema.GetProperty("else").GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(200);
    [Fact] void should_return_maximum_byte_page() => _page.GetProperty("page").GetProperty("byteCount").GetInt32().ShouldEqual(192 * 1024);
    [Fact] void should_keep_maximum_page_under_cap() => Encoding.UTF8.GetByteCount(_page.GetRawText()).ShouldBeLessThan(McpJson.MaximumStructuredResponseBytes);
    [Fact] void should_refuse_oversized_byte_limit() => _oversizedBytes.ShouldContain("'limit'");
    [Fact] void should_refuse_oversized_item_limit() => _oversizedItems.ShouldContain("'limit'");
    [Fact] void should_allow_maximum_item_limit() => _documents.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
}
