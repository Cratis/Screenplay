// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_source_and_container_notes_compete : given.an_unbound_persisted_notes_workspace
{
    JsonElement _notes;
    Exception _failure = null!;

    void Establish()
    {
        _notes = ProposeNote(Targets(SemanticKind.Slice)[0]);
        var source = Split.Documents.Single(document => document.Path.Value == "Orders/PlaceOrder.play").Text.Replace("reads Stock by orderId", string.Empty, StringComparison.Ordinal);
        var changed = ReplaceSlice(source);
        Assert.True(changed.GetProperty("success").GetBoolean(), changed.GetRawText());
        Apply(changed);
    }

    void Because() => _failure = Catch.Exception(() => Apply(_notes));

    [Fact] void should_refuse_the_stale_notes_proposal() => _failure.Message.ShouldContain("StaleRevision");
    [Fact] void should_not_write_notes() => Files.Read("notes.json").ShouldBeNull();
}
