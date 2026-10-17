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
        Apply(ProposeNote(Targets(SemanticKind.Slice, typed: false)[0]));
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

    [Fact] void should_list_the_note_for_each_unidentified_container() => _items.TrueForAll(items => items.GetArrayLength() == 1).ShouldBeTrue();
    [Fact] void should_resolve_dotted_and_handle_filters_identically() => _items.Select(items => items[0].GetProperty("note").GetProperty("id").GetString()).Distinct().Count().ShouldEqual(1);
}
