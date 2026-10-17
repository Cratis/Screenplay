// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_independent_reference_edits : given.an_authoring_connection
{
    const string Shipping = """
        slice StateChange ShipOrder
          command ShipOrder
            orderId OrderId identifier
            carrier Carrier
            produces event OrderShipped
              carrier Carrier = carrier
          constraint PublishOnce
            unique event OrderShipped
            unique event OrderRecalled
            message "Already shipped"
          event OrderPlaced
          event OrderPaid
          event OrderRecalled
          specification Shipping
            given OrderPlaced
              for "o1"
            given OrderPaid
              for "o1"
            when ShipOrder
              orderId = "o1"
              carrier = "c"
            then OrderShipped
              for "o1"
        """;
    JsonElement _proposal;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "Orders"));
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "domain Acme\nconcept OrderId : String\nconcept Carrier : String\nimport \"Orders/Orders.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "Orders", "Orders.play"), "module Orders\n  feature Fulfilment\n    import \"Ship.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "Orders", "Ship.play"), Shipping);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var document = Page("documents", opened.GetProperty("revision").GetString()).EnumerateArray().Single(document => document.GetProperty("path").GetString() == "Orders/Ship.play");
        var changed = Shipping.Replace("    unique event OrderRecalled\n", string.Empty, StringComparison.Ordinal)
            .Replace("  event OrderRecalled", "  event OrderRecalled\n  event OrderLost", StringComparison.Ordinal)
            .Replace("    when ShipOrder", "    given OrderLost\n      for \"o1\"\n    when ShipOrder", StringComparison.Ordinal);
        _proposal = Result("propose-source", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "PreserveExactSource", validation = "Authoring", referencePolicy = "Safe",
            documents = new[] { new { operation = "replace-document", documentId = document.GetProperty("documentId").GetString(), source = changed } }
        });
    }

    [Fact] void should_accept_the_batch() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_retain_a_reviewable_proposal() => _proposal.GetProperty("proposalId").GetString().ShouldNotBeEmpty();
    [Fact] void should_not_write_the_source() => File.ReadAllText(Path.Combine(RootPath, "Orders", "Ship.play")).ShouldEqual(Shipping);
}
