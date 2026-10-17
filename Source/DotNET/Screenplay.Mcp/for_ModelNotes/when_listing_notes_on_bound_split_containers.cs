// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_listing_notes_on_bound_split_containers : given.a_split_notes_workspace
{
    readonly List<(SemanticKind Kind, JsonElement Items)> _results = [];

    void Establish()
    {
        OpenSplit();
        Apply(ProposeNotes([.. new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice }.Select(kind => (object)new
        {
            operation = "add", kind = "question", text = kind.ToString(), author = "team", target = Targets(kind)[0]
        })]));
    }

    void Because()
    {
        foreach (var kind in new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice })
        {
            foreach (var target in Targets(kind))
            {
                var result = Result(Workspaces.ListNotes(Arguments(new { anchor = target })));
                _results.Add((kind, result.GetProperty("page").GetProperty("items")));
            }
        }
    }

    [Fact] void should_select_one_note_for_every_target_form() => _results.TrueForAll(result => result.Items.GetArrayLength() == 1).ShouldBeTrue();
    [Fact] void should_select_only_the_requested_container_note() => _results.TrueForAll(result => result.Items[0].GetProperty("note").GetProperty("text").GetString() == result.Kind.ToString()).ShouldBeTrue();
    [Fact] void should_select_the_same_note_for_dotted_typed_and_handle_targets() => _results.GroupBy(result => result.Kind).All(group => group.Select(result => result.Items[0].GetProperty("note").GetProperty("id").GetString()).Distinct().Count() == 1).ShouldBeTrue();
}
