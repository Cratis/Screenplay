// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_canonicalizing_order_model_with_interleaved_members : given.an_order_model_with_comments
{
    JsonElement _proposal;
    string _after = string.Empty;

    void Establish() => File.WriteAllBytes(OrderPath, Bytes(Order + "\n" +
        """
              constraint UniqueOrder
                unique event OrderPlaced
            slice StateView List
              query Find => OrderId[]
              screen List
                table Find
                  column orderId
              query Other => OrderId[]
        """));

    void Because()
    {
        _proposal = ProposeChannels("CanonicalizeTouchedDocuments");
        _after = Candidate(_proposal).Documents.Single(document => document.Path.Value.EndsWith("PlaceOrder.play", StringComparison.Ordinal)).Text;
    }

    [Fact] void should_accept_the_proposal() => _proposal.GetProperty("proposalId").GetString().ShouldNotBeNullOrEmpty();
    [Fact] void should_print_the_changed_values() => _after.ShouldContain("channel = \"store\"");
    [Fact] void should_keep_constraint_after_specifications() => InOrder("specification PlacingAnOrder", "constraint UniqueOrder");
    [Fact] void should_keep_screen_between_queries() => InOrder("query Find", "screen List", "query Other");

    void InOrder(params string[] declarations)
    {
        var positions = declarations.Select(declaration => _after.IndexOf(declaration, StringComparison.Ordinal)).ToArray();
        positions.All(position => position >= 0).ShouldBeTrue();
        positions.SequenceEqual(positions.Order()).ShouldBeTrue();
    }
}
