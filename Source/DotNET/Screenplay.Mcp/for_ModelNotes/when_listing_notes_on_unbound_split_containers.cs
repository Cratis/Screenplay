// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_listing_notes_on_unbound_split_containers : given.a_split_notes_workspace
{
    readonly List<JsonElement> _items = [];

    void Establish()
    {
        OpenSplit(unbound: true);
        Apply(ProposeNotes([.. new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice }.Select(kind => (object)new
        {
            operation = "add", kind = "question", text = kind.ToString(), author = "team", target = Targets(kind, typed: false)[0]
        })]));
    }

    void Because()
    {
        foreach (var kind in new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice })
        {
            foreach (var target in Targets(kind))
            {
                _items.Add(Result(Workspaces.ListNotes(Arguments(new { anchor = target }))).GetProperty("page").GetProperty("items"));
            }
        }
    }

    [Fact] void should_list_only_the_selected_containers_note() => _items.TrueForAll(items => items.GetArrayLength() == 1).ShouldBeTrue();
    [Fact] void should_include_each_container_in_all_three_forms() => _items.Select(items => items[0].GetProperty("note").GetProperty("text").GetString()).GroupBy(text => text).All(group => group.Count() == 3).ShouldBeTrue();
    [Fact] void should_resolve_each_note_to_its_own_container() => _items.TrueForAll(items => items[0].GetProperty("address").GetProperty("kind").GetString() == items[0].GetProperty("note").GetProperty("text").GetString()).ShouldBeTrue();
}
