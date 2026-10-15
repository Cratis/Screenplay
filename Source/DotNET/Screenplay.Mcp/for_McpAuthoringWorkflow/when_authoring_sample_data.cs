// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// Sample data is typed syntax like everything else a UI author writes: a read, a typed replace of a row value, a typed
/// replace of the ui profile's selection (the one place the active pack is chosen) and a typed remove all round trip.
/// </summary>
public class when_authoring_sample_data : given.an_authoring_connection
{
    const string Application = """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                name String
                revenue Decimal

        sample data NordicDemo
          title "CRM · Nordic SMB"
          locale nb-NO
          shape Customer
            row
              name = "Fjordline AS"
              revenue = 125000.5

        sample data OtherDemo
          shape Customer
            row
              name = "Nordlys AS"

        ui profile Web
          target platform web

          sample data
            active NordicDemo
        """;

    JsonElement _opened;
    JsonElement[] _packs = [];
    JsonElement[] _selections = [];
    string _changedValue = string.Empty;
    string _changedActive = string.Empty;
    string _removedPack = string.Empty;
    string _removedSelection = string.Empty;
    string _unknownPack = string.Empty;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        Apply(empty, Result("propose-source", ArgumentsFor(empty, new { operation = "create-document", path = "application.play", stableKey = "application", source = Application })));
        _opened = Open();
        var revision = _opened.GetProperty("revision").GetString()!;
        _packs = Nodes("SampleDataPackSyntax", revision);
        _selections = Nodes("SampleDataSelectionSyntax", revision);

        var field = Nodes("SampleDataFieldSyntax", revision).First(node => node.GetProperty("node").GetProperty("name").GetString() == "name");
        var changedField = Replacement(field, node => node["value"] = "Fjordline Handel AS");
        _changedValue = Text(Propose(changedField));

        var profile = Nodes("UiProfileSyntax", revision).Single();
        _changedActive = Text(Propose(Replacement(profile, node =>
        {
            var selection = node["sampleData"]!.AsObject();
            selection["active"]!.AsObject()["name"] = "OtherDemo";
        })));
        try
        {
            Propose(Replacement(profile, node => node["sampleData"]!.AsObject()["active"]!.AsObject()["name"] = "Ghost"));
        }
        catch (McpFailure failure)
        {
            _unknownPack = failure.Message;
        }

        _removedSelection = Text(Propose(Replacement(profile, node => node["sampleData"] = null)));
        _removedPack = Text(Propose(new { operation = "remove", target = _packs.First(pack => pack.GetProperty("node").GetProperty("name").GetString() == "OtherDemo").GetProperty("handle") }));
    }

    [Fact] void should_read_each_pack_as_a_typed_node() => _packs.Length.ShouldEqual(2);
    [Fact] void should_read_the_selection_the_ui_profile_carries() => _selections.Length.ShouldEqual(1);
    [Fact] void should_replace_a_row_value() => _changedValue.ShouldContain("name = \"Fjordline Handel AS\"");
    [Fact] void should_keep_the_other_values_when_replacing_one() => _changedValue.ShouldContain("revenue = 125000.5");
    [Fact] void should_change_the_active_pack_once_in_the_profile() => _changedActive.ShouldContain("sample data\n    active OtherDemo");
    [Fact] void should_refuse_an_active_pack_nothing_declares() => _unknownPack.ShouldContain("PLAY0714");
    [Fact] void should_remove_the_selection_from_the_profile() => _removedSelection.ShouldNotContain("active NordicDemo");
    [Fact] void should_keep_the_profile_when_removing_its_selection() => _removedSelection.ShouldContain("ui profile Web");
    [Fact] void should_remove_a_pack() => _removedPack.ShouldNotContain("sample data OtherDemo");
    [Fact] void should_keep_the_other_pack_when_removing_one() => _removedPack.ShouldContain("sample data NordicDemo");

    JsonElement[] Nodes(string kind, string revision) => [.. Result("read-ast", new { expectedRevision = revision, kind, includeContent = true, limit = 200 })
        .GetProperty("page").GetProperty("items").EnumerateArray()];

    object Replacement(JsonElement target, Action<System.Text.Json.Nodes.JsonObject> edit)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(target.GetProperty("node").GetRawText())!.AsObject();
        edit(node);
        return new { operation = "replace", target = target.GetProperty("handle"), node };
    }

    static object ArgumentsFor(JsonElement opened, object document) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { document }
    };

    JsonElement Propose(object operation) => Result("propose-ast", ProposalArguments(operation));

    string Text(JsonElement proposal)
    {
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        var text = Candidate(proposal).Documents.Single().Text;
        Result("discard-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() });
        return text;
    }

    object ProposalArguments(object operation) => new
    {
        expectedRevision = _opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
        validation = "Authoring",
        formatting = "CanonicalizeTouchedDocuments",
        includeContent = true,
        operations = new[] { operation }
    };
}
