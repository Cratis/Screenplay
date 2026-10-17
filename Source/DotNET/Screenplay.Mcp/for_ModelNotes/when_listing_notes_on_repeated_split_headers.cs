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

    [Fact] void should_select_both_notes_at_the_substituted_application_anchor() => _items.TrueForAll(items => items.GetArrayLength() == 2).ShouldBeTrue();
    [Fact] void should_include_notes_added_for_both_container_addresses() => _items.TrueForAll(items => items.EnumerateArray().Select(item => item.GetProperty("note").GetProperty("text").GetString()).Order().SequenceEqual(["Feature", "Module"])).ShouldBeTrue();
}
