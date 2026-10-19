// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_every_offer_proposes_successfully : given.a_refactoring_workspace
{
    JsonElement[] _extractions = [];
    JsonElement _rename;

    void Because()
    {
        _extractions = [.. Offers("extract-inline-event").Select(offer => ProposeExtraction(offer.GetProperty("subject")).GetProperty("result"))];
        var offer = Offers("rename-declaration").Single(value => value.GetProperty("name").GetString() == "Rename" && value.GetProperty("declarationKind").GetString() == "CommandSyntax");
        _rename = Call("propose-rename", new
        {
            expectedRevision = CurrentRevision,
            expectedCatalogRevision = CatalogRevision,
            target = offer.GetProperty("subject"),
            expectedName = "Rename",
            newName = "RenameProject"
        }).GetProperty("result");
    }

    [Fact] void should_discover_more_than_one_extraction() => _extractions.Length.ShouldEqual(2);
    [Fact] void should_accept_every_extraction_offer() => _extractions.All(result => !result.GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_return_a_proposal_for_every_extraction_offer() => _extractions.All(result => result.GetProperty("structuredContent").TryGetProperty("proposalId", out _)).ShouldBeTrue();
    [Fact] void should_accept_a_rename_offer() => _rename.GetProperty("isError").GetBoolean().ShouldBeFalse();
}
