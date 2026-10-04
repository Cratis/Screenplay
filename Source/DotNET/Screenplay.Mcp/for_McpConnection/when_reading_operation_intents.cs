// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_operation_intents : given.a_connection
{
    JsonElement _inventory;
    JsonElement _details;
    JsonElement _productions;
    JsonElement _stale;
    JsonElement _wrongKind;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      event Recorded\n      operation Send\n        uses Mailer\n        recipient String optional\n        execute\n          implementation\n            hint \"First\"\n            hint \"Second\"\n            file Send.cs\n        compensate\n          description \"Undo\"\n      command C\n        send Bool\n        produces Recorded\n        produces when send == true\n          Send\n        produces Recorded\n");
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        _inventory = Call("read-workspace", new { expectedRevision = revision, view = "operation-intents", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var key = _inventory.GetProperty("page").GetProperty("items")[0].GetProperty("authoringKey").GetString();
        _details = Call("read-workspace", new { expectedRevision = revision, view = "operation-intent-details", authoringKey = key, limit = 200 }).GetProperty("result").GetProperty("structuredContent");
        _productions = Call("read-workspace", new { expectedRevision = revision, view = "ordered-productions" }).GetProperty("result").GetProperty("structuredContent");
        _stale = Call("read-workspace", new { expectedRevision = "stale", view = "operation-intents", offset = 1 }).GetProperty("result");
        _wrongKind = Call("read-workspace", new { expectedRevision = revision, view = "system-intent-details", authoringKey = key }).GetProperty("result");
    }

    [Fact] void should_work_without_an_executable_model() => _inventory.GetProperty("executionAvailable").GetBoolean().ShouldBeFalse();
    [Fact] void should_expose_authoring_keys_without_fabricating_semantic_ids()
    {
        var entry = _inventory.GetProperty("page").GetProperty("items")[0];
        entry.GetProperty("kind").GetString().ShouldEqual("Operation");
        entry.GetProperty("scope").GetArrayLength().ShouldEqual(3);
        entry.TryGetProperty("semanticId", out _).ShouldBeFalse();
        entry.TryGetProperty("requirementId", out _).ShouldBeFalse();
        entry.GetProperty("execute").GetProperty("sourceHandle").GetProperty("path").GetString().ShouldNotBeEmpty();
        entry.GetProperty("compensate").GetProperty("state").GetString().ShouldEqual("pending");
    }
    [Fact] void should_page_typed_inputs_and_ordered_hints()
    {
        var items = _details.GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        items.Single(item => item.GetProperty("kind").GetString() == "input").GetProperty("type").GetProperty("isOptional").GetBoolean().ShouldBeTrue();
        items.Where(item => item.GetProperty("kind").GetString() == "hint").Select(item => item.GetProperty("text").GetString()).SequenceEqual(["First", "Second"]).ShouldBeTrue();
    }
    [Fact] void should_preserve_all_ordered_production_occurrences()
    {
        var items = _productions.GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        items.Select(item => item.GetProperty("targetKind").GetString()).SequenceEqual(["Event", "Operation", "Event"]).ShouldBeTrue();
        items.Select(item => item.GetProperty("order").GetInt32()).SequenceEqual([0, 1, 2]).ShouldBeTrue();
        items[1].GetProperty("when").GetProperty("left").GetString().ShouldEqual("send");
    }
    [Fact] void should_refuse_stale_revision_pages() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_refuse_conflicting_declaration_kind() => _wrongKind.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
