// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_publishing_identifier_payload_repairs : given.an_authoring_connection
{
    const string Source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n          projectId Uuid = projectId\n          name String = \"something\"\n";

    JsonElement Action(string code) => Result("repair-capabilities").GetProperty("actions").EnumerateArray().Single(action => action.GetProperty("diagnosticCode").GetString() == code);

    [Theory]
    [InlineData("PLAY0469")]
    [InlineData("PLAY0516")]
    [InlineData("PLAY0664")]
    void should_advertise_the_unpinned_canonicalizing_action(string code)
    {
        Initialize();
        var action = Action(code);
        action.GetProperty("tool").GetString().ShouldEqual("propose-repair");
        action.GetProperty("requiredFormatting").GetString().ShouldEqual("CanonicalizeTouchedDocuments");
        action.GetProperty("pinRepairEvidence").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    void should_disclose_the_contract_change_safety_contract_for_the_identifier_payload_repair()
    {
        Initialize();
        var action = Action("PLAY0469");
        action.GetProperty("canFixAll").GetBoolean().ShouldBeFalse();
        action.GetProperty("individualReviewRequired").GetBoolean().ShouldBeTrue();
        action.GetProperty("contractChange").GetBoolean().ShouldBeTrue();
        action.GetProperty("retiresSemanticAddresses").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_describe_retired_addresses_the_way_the_editor_reads_them()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var revision = Open().GetProperty("revision").GetString()!;
        var repair = Page("repairs", revision).EnumerateArray().Single(value => value.GetProperty("diagnosticCode").GetString() == "PLAY0469");
        var address = repair.GetProperty("retiredSemanticAddresses")[0];
        address.GetProperty("kind").ValueKind.ShouldEqual(JsonValueKind.String);
        var part = address.GetProperty("parts")[0];
        part.GetProperty("kind").ValueKind.ShouldEqual(JsonValueKind.String);
        part.GetProperty("key").ValueKind.ShouldEqual(JsonValueKind.String);
    }

    [Fact]
    void should_still_refuse_a_pinned_proposal_for_the_identifier_payload_repair()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var discovery = Result("read-workspace", new { expectedRevision = revision, view = "repairs" });
        var repair = discovery.GetProperty("page").GetProperty("items").EnumerateArray().Single(value => value.GetProperty("diagnosticCode").GetString() == "PLAY0469");
        var response = Call("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0469",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments",
            pinRepairEvidence = true,
            expectedRepairEvidenceRevision = discovery.GetProperty("repairEvidenceRevision").GetString()
        });
        var failure = response.TryGetProperty("error", out var error)
            ? error.GetProperty("data").GetProperty("failureKind")
            : response.GetProperty("result").GetProperty("structuredContent").GetProperty("failureKind");
        failure.GetString().ShouldEqual("UnsupportedRepair");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }
}
