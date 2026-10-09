// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_moving_a_slice_and_restarting : given.an_authoring_connection
{
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _diff;
    string[] _beforeIds = [];
    string[] _afterIds = [];
    string[] _beforeEvents = [];
    string[] _afterEvents = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source + "\nmodule Projects\n  feature Archive\n    slice StateView Old");
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var semantics = Page("semantics", revision).EnumerateArray().ToArray();
        _beforeIds = [.. semantics.Select(item => item.GetProperty("semanticId").GetString())];
        _beforeEvents = [.. Page("eventContracts", revision).EnumerateArray().Select(item => item.GetProperty("eventContractId").GetString())];
        var target = semantics.Single(item => item.GetProperty("address").GetProperty("kind").GetString() == "Slice" && item.GetProperty("address").GetProperty("parts").EnumerateArray().Last().GetProperty("key").GetString() == "RegisterProject").GetProperty("address");
        var newParent = semantics.Single(item => item.GetProperty("address").GetProperty("kind").GetString() == "Feature" && item.GetProperty("address").GetProperty("parts").EnumerateArray().Last().GetProperty("key").GetString() == "Archive").GetProperty("address");
        _proposal = Result("propose-move", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target,
            newParent,
            targetHandle = Result("read-ast", new { expectedRevision = revision, kind = "SliceSyntax", name = "RegisterProject" }).GetProperty("page").GetProperty("items")[0].GetProperty("handle"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        var proposalId = _proposal.GetProperty("proposalId").GetString();
        _review = Result("read-proposal", new { proposalId });
        _diff = Result("read-proposal", new { proposalId, view = "semantic-diff" });
        Apply(opened, _proposal);
        Connection = new(new McpTools(new McpRoot(RootPath)));
        Initialize();
        var reopened = Open();
        var afterRevision = reopened.GetProperty("revision").GetString();
        _afterIds = [.. Page("semantics", afterRevision).EnumerateArray().Select(item => item.GetProperty("semanticId").GetString())];
        _afterEvents = [.. Page("eventContracts", afterRevision).EnumerateArray().Select(item => item.GetProperty("eventContractId").GetString())];
    }

    [Fact] void should_report_semantic_and_event_migrations() => _proposal.GetProperty("moveReport").GetProperty("identityMigrations").EnumerateArray().Select(item => item.GetProperty("domain").GetString()).Distinct().ShouldContainOnly("semantic", "event");
    [Fact] void should_list_no_retired_identity() => _proposal.GetProperty("moveReport").GetProperty("retired").GetArrayLength().ShouldEqual(0);
    [Fact] void should_show_the_same_migrations_on_review() => _review.GetProperty("moveReport").GetRawText().ShouldEqual(_proposal.GetProperty("moveReport").GetRawText());
    [Fact] void should_classify_the_semantic_difference_as_an_owner_move() => _diff.GetRawText().Contains("owner", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_every_semantic_id_after_apply_and_reopen() => _afterIds.ShouldContainOnly(_beforeIds);
    [Fact] void should_keep_every_event_contract_id_after_apply_and_reopen() => _afterEvents.ShouldContainOnly(_beforeEvents);
}
