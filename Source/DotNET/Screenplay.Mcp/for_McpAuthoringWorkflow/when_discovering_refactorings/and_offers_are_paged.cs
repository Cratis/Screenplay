// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_offers_are_paged : given.a_refactoring_workspace
{
    JsonElement _first;
    JsonElement _second;

    void Because()
    {
        _first = Result("read-workspace", new { expectedRevision = CurrentRevision, view = "refactorings", limit = 1 }).GetProperty("page");
        _second = Result("read-workspace", new { expectedRevision = CurrentRevision, view = "refactorings", offset = 1, limit = 1 }).GetProperty("page");
    }

    [Fact] void should_return_one_offer_per_page() => _first.GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_continue_from_the_next_offset() => _first.GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_pin_the_page_to_the_workspaceCurrentRevision() => _first.GetProperty("revision").GetString().ShouldEqual(CurrentRevision);
    [Fact] void should_return_a_different_offer_on_the_next_page() => _second.GetProperty("items")[0].GetRawText().ShouldNotEqual(_first.GetProperty("items")[0].GetRawText());
    [Fact] void should_report_every_offer_in_the_total() => _first.GetProperty("totalCount").GetInt32().ShouldEqual(Page("refactorings", CurrentRevision).GetArrayLength());
}
