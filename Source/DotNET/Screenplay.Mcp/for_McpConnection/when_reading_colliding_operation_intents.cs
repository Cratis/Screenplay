// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_colliding_operation_intents : given.a_connection
{
    [Theory]
    [InlineData("operations", false)]
    [InlineData("operations", true)]
    [InlineData("slice", false)]
    [InlineData("slice", true)]
    public void should_keep_physical_inventory_handles_and_refuse_a_collapsed_production(string scenario, bool reverse)
    {
        const string intent = "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        recipient String\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateChange T\n      command Ask\n        produces S.Send\n");
        File.WriteAllText(Path.Combine(RootPath, reverse ? "b.play" : "a.play"), intent);
        File.WriteAllText(Path.Combine(RootPath, reverse ? "a.play" : "b.play"), scenario == "operations" ? intent : "module M\n  feature F\n    slice StateChange S\n      command Other\n");
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var inventory = Call("read-workspace", new { expectedRevision = revision, view = "operation-intents" }).GetProperty("result").GetProperty("structuredContent");
        var entries = inventory.GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        entries.Length.ShouldEqual(scenario == "operations" ? 2 : 1);
        entries.Select(entry => entry.GetProperty("handle").GetProperty("documentId").GetString()).Distinct().Count().ShouldEqual(entries.Length);
        foreach (var entry in entries)
        {
            entry.TryGetProperty("semanticId", out _).ShouldBeFalse();
            var details = Call("read-workspace", new { expectedRevision = revision, view = "operation-intent-details", authoringKey = entry.GetProperty("authoringKey").GetString() }).GetProperty("result");
            details.GetProperty("isError").GetBoolean().ShouldBeTrue();
        }
        var productions = Call("read-workspace", new { expectedRevision = revision, view = "ordered-productions" }).GetProperty("result").GetProperty("structuredContent");
        var production = productions.GetProperty("page").GetProperty("items")[0];
        production.GetProperty("targetKind").GetString().ShouldEqual("Ambiguous");
        var candidates = production.GetProperty("candidates").EnumerateArray().ToArray();
        candidates.Length.ShouldEqual(scenario == "operations" ? 2 : 3);
        candidates.Select(candidate => candidate.GetProperty("location").GetProperty("path").GetString()).Distinct().Order(StringComparer.Ordinal).SequenceEqual(["a.play", "b.play"]).ShouldBeTrue();
        candidates.Any(candidate => candidate.TryGetProperty("semanticId", out _)).ShouldBeFalse();
        production.GetProperty("executionAvailable").GetBoolean().ShouldBeFalse();
    }
}
