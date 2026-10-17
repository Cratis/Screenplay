// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_listing_notes_on_repeated_split_headers : given.a_repeated_split_notes_workspace
{
    readonly List<JsonElement> _items = [];

    void Establish() => Apply(ProposeNotes([.. new[] { SemanticKind.Module, SemanticKind.Feature }.Select(kind => (object)new
    {
        operation = "add", kind = "question", text = kind.ToString(), author = "team", target = Targets(kind, typed: false)[0]
    })]));

    void Because()
    {
        foreach (var kind in new[] { SemanticKind.Module, SemanticKind.Feature })
        {
            var target = Targets(kind, typed: false)[0];
            _items.Add(Result(Workspaces.ListNotes(Arguments(new { anchor = target }))).GetProperty("page").GetProperty("items"));
        }
    }

    // Decision 0093 items 1 and 8: each logical container owns its note; repeated headers are not application fallbacks.
    [Fact] void should_select_only_the_logical_containers_note() => _items.TrueForAll(items => items.GetArrayLength() == 1).ShouldBeTrue();
    [Fact] void should_keep_each_note_on_its_own_container() => _items.TrueForAll(items => items[0].GetProperty("note").GetProperty("text").GetString() == items[0].GetProperty("address").GetProperty("kind").GetString()).ShouldBeTrue();
}
