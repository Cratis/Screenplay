// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_targeting_duplicate_split_declarations : given.a_split_notes_workspace
{
    Exception _listing = null!;
    Exception _proposing = null!;

    void Establish()
    {
        OpenSplit(unbound: true);
        File.Copy(Path.Combine(RootPath, "Orders/PlaceOrder.play"), Path.Combine(RootPath, "Orders/Duplicate.play"));
        File.AppendAllText(Path.Combine(RootPath, "Orders/Orders.play"), "\n    import \"Duplicate.play\"\n");
        Opened = Result(Workspaces.Open(McpJson.Empty));
    }

    void Because()
    {
        var target = Arguments(new { address = "Orders.Fulfilment.PlaceOrder", kind = "Slice" });
        _listing = Catch.Exception(() => Workspaces.ListNotes(Arguments(new { anchor = target })));
        _proposing = Catch.Exception(() => ProposeNote(target));
    }

    [Fact] void should_refuse_ambiguous_listing() => _listing.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_refuse_ambiguous_proposals() => _proposing.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_explain_the_listing_ambiguity() => _listing.Message.ShouldContain("ambiguous");
    [Fact] void should_explain_the_proposal_ambiguity() => _proposing.Message.ShouldContain("ambiguous");
}
