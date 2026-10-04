// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_unresolved_operation_placements : given.a_connection
{
    JsonElement _inventory;
    JsonElement _details;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "system Mailer\nmodule M\n  feature F\n    import \"slice.play\"\n  feature G\n    import \"slice.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "slice.play"), "slice StateChange S\n  operation Send\n    uses Mailer\n");
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        _inventory = Call("read-workspace", new { expectedRevision = revision, view = "operation-intents" }).GetProperty("result").GetProperty("structuredContent");
        _details = Call("read-workspace", new { expectedRevision = revision, view = "operation-intent-details", authoringKey = "unknown" }).GetProperty("result");
    }

    [Fact] void should_disclose_unresolved_placement_without_inventing_an_owner()
    {
        _inventory.GetProperty("unresolvedPlacementCount").GetInt32().ShouldEqual(1);
        var item = _inventory.GetProperty("page").GetProperty("items")[0];
        item.GetProperty("kind").GetString().ShouldEqual("unresolved-placement");
        item.TryGetProperty("authoringKey", out _).ShouldBeFalse();
        item.TryGetProperty("semanticId", out _).ShouldBeFalse();
    }
    [Fact] void should_return_a_typed_conflict_instead_of_an_internal_selection_exception()
    {
        _details.GetProperty("isError").GetBoolean().ShouldBeTrue();
        _details.GetProperty("content")[0].GetProperty("text").GetString()!.ShouldContain("UnresolvedPlacement:");
    }
}
