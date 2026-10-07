// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_commands_are_guarded_screen_actions : given.a_graph_query
{
    JsonElement[] _edges;

    void Establish() => _snapshot = Snapshot(
        """
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                itemId Uuid
                status String
              query ItemDetails => Item
              screen Details
                data Item via query ItemDetails
                action "Continue"
                  when item.status == "failed" execute Retry
                    with itemId from item.itemId
                  when item.status == "new" execute Start
                    with itemId from item.itemId
                  otherwise execute Close
                    with itemId from item.itemId
                action "Retry"
                  when item.status == "failed" execute Retry
                  otherwise hidden
          feature Commands
            slice StateChange RetryItem
              command Retry
                itemId Uuid
            slice StateChange StartItem
              command Start
                itemId Uuid
            slice StateChange CloseItem
              command Close
                itemId Uuid
        """);

    void Because()
    {
        _result = Read(_snapshot, new { from = "slice", to = "slice", kinds = new[] { "asks" }, evidenceLimit = 20 });
        _edges = [.. _result.GetProperty("page").GetProperty("items").EnumerateArray()];
    }

    [Fact] void should_resolve_alternatives_and_execute_fallback_as_command_edges() => _edges.Select(edge => edge.GetProperty("target").GetProperty("address").GetString()).ShouldContainOnly("Work.Commands.RetryItem", "Work.Commands.StartItem", "Work.Commands.CloseItem");
    [Fact] void should_use_the_screen_slice_as_consumer() => _edges.All(edge => edge.GetProperty("source").GetProperty("address").GetString() == "Work.Items.Details").ShouldBeTrue();
    [Fact] void should_classify_every_edge_like_a_plain_action() => _edges.All(edge => edge.GetProperty("byKind").GetProperty("asks").GetInt32() == edge.GetProperty("references").GetInt32()).ShouldBeTrue();
    [Fact] void should_retain_every_guarded_command_reference() => _edges.Sum(edge => edge.GetProperty("references").GetInt32()).ShouldEqual(4);
    [Fact] void should_preserve_the_index_roles() => _edges.SelectMany(edge => edge.GetProperty("evidence").EnumerateArray()).Select(item => $"{item.GetProperty("name").GetString()}:{item.GetProperty("role").GetString()}").ShouldContainOnly("Retry:actionAlternative", "Retry:actionAlternative", "Start:actionAlternative", "Close:actionOtherwise");
    [Fact] void should_keep_every_edge_evidence_in_the_reference_index() => _edges.SelectMany(edge => edge.GetProperty("evidence").EnumerateArray()).All(item => _snapshot.Index.References.Any(reference => reference.Name == item.GetProperty("name").GetString() && reference.Role == item.GetProperty("role").GetString())).ShouldBeTrue();
    [Fact] void should_not_treat_hidden_fallbacks_or_item_bindings_as_unresolved_dependencies() => _result.GetProperty("coverage").GetProperty("unresolvedCount").GetInt32().ShouldEqual(0);
}
