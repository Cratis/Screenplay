// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_handler_intents : given.a_connection
{
    JsonElement _page;
    JsonElement _details;
    JsonElement _stale;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("        produces ProjectRegistered\n          for projectId\n          projectId = projectId\n          name = name", "        handler\n          implementation\n            hint \"Keep the name\"\n            hint \"Keep the identity\"", StringComparison.Ordinal));
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        _page = Call("read-workspace", new { expectedRevision = revision, view = "handler-intents", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var id = _page.GetProperty("page").GetProperty("items")[0].GetProperty("requirementId").GetString();
        _details = Call("read-workspace", new { expectedRevision = revision, view = "handler-intent-details", requirementId = id, limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _stale = Call("read-workspace", new { expectedRevision = revision, view = "handler-intents", offset = 1, expectedCatalogRevision = "stale" }).GetProperty("result");
    }

    [Fact] void should_disclose_handler_only_coverage() => _page.GetProperty("coverage").GetString().ShouldEqual("CommandHandler");
    [Fact] void should_expose_pending_intent_without_esm() => _page.GetProperty("page").GetProperty("items")[0].GetProperty("state").GetString().ShouldEqual("pending");
    [Fact] void should_expose_an_authoring_handle()
    {
        var handle = _page.GetProperty("page").GetProperty("items")[0].GetProperty("handle");
        handle.GetProperty("revision").GetString().ShouldNotBeEmpty();
        handle.GetProperty("documentId").GetString().ShouldNotBeEmpty();
        handle.GetProperty("path").GetString().ShouldNotBeEmpty();
        McpAstHandles.Read(handle).Path.ShouldEqual(handle.GetProperty("path").GetString());
    }

    [Fact] void should_not_claim_execution() => _page.GetProperty("page").GetProperty("items")[0].GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_page_ordered_hints() => _details.GetProperty("page").GetProperty("items")[0].GetString().ShouldEqual("Keep the name");
    [Fact] void should_count_all_hints() => _details.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(2);
    [Fact] void should_refuse_stale_catalog_continuation() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
