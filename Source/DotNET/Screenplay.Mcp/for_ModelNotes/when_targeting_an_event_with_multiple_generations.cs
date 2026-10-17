// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_targeting_an_event_with_multiple_generations : given.a_split_notes_workspace
{
    Exception _listing = null!;
    Exception _proposing = null!;

    void Establish()
    {
        OpenSplit();
        File.WriteAllText(Path.Combine(RootPath, "Orders/PlaceOrder.play"), """
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                produces OrderPlaced
              event OrderPlaced generation 1
              event OrderPlaced generation 2
            """);
        Split = ScreenplayWorkspace.Create("Acme", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
        Index = WorkspaceSyntaxIndex.Create(Split);
        Opened = Result(Workspaces.Open(McpJson.Empty));
    }

    void Because()
    {
        var target = Arguments(new { address = "Orders.Fulfilment.PlaceOrder.OrderPlaced", kind = "Event" });
        _listing = Catch.Exception(() => Workspaces.ListNotes(Arguments(new { anchor = target })));
        _proposing = Catch.Exception(() => ProposeNote(target));
    }

    [Fact] void should_bind_both_generations() => Split.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_existing_dotted_listing_refusal() => _listing.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_preserve_the_existing_dotted_proposal_refusal() => _proposing.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_explain_the_dotted_ambiguity() => _proposing.Message.ShouldEqual("Unknown or ambiguous note address.");
    [Fact] void should_retain_both_event_occurrences() => Index.Entries.Count(entry => entry.Address?.Kind == SemanticKind.EventContract).ShouldEqual(2);
}
