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
            foreach (var target in Targets(kind, typed: false))
            {
                _items.Add(Result(Workspaces.ListNotes(Arguments(new { anchor = target }))).GetProperty("page").GetProperty("items"));
            }
        }
    }

    [Fact] void should_list_all_notes_stored_under_the_substituted_anchor() => _items.TrueForAll(items => items.GetArrayLength() == 3).ShouldBeTrue();
    [Fact] void should_include_notes_added_for_every_container() => _items.TrueForAll(items => items.EnumerateArray().Select(item => item.GetProperty("note").GetProperty("text").GetString()).Order().SequenceEqual(["Feature", "Module", "Slice"])).ShouldBeTrue();
    [Fact] void should_resolve_dotted_and_handle_filters_identically() => _items.Select(items => string.Join(',', items.EnumerateArray().Select(item => item.GetProperty("note").GetProperty("id").GetString()).Order())).Distinct().Count().ShouldEqual(1);
}
