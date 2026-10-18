// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_refusing_destination_drift : given.a_destination_repair
{
    [Theory]
    [InlineData("content")]
    [InlineData("missing")]
    [InlineData("invalid-utf8")]
    [InlineData("directory")]
    void should_refuse_every_retained_preview_and_apply_after_attachment_evidence_changes(string change)
    {
        Start();
        var proposal = ProposeDestination();
        ChangeAttachment(change);
        FailureOf(() => Review(proposal)).ShouldEqual("RepairEvidenceDrift");
        FailureOf(() => Install(proposal)).ShouldEqual("RepairEvidenceDrift");
        ShouldHaveNoInstalledSource();
    }

    [Theory]
    [InlineData("source")]
    [InlineData("membership")]
    [InlineData("identity-state")]
    void should_refuse_exact_disk_drift_without_replanning(string change)
    {
        Start();
        var proposal = ProposeDestination();
        ChangeDisk(change);
        Catch.Exception(() => Review(proposal)).ShouldNotBeNull();
        Catch.Exception(() => Install(proposal)).ShouldNotBeNull();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(change == "source" ? OriginalSource + "\n// disk drift\n" : OriginalSource);
        File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("attachment")]
    [InlineData("source")]
    [InlineData("membership")]
    [InlineData("identity-state")]
    void should_refuse_validation_to_retention_races_without_accepting_new_proof(string change)
    {
        Start();
        Workspaces.BeforeStore = () => { if (change == "attachment") ChangeAttachment("content"); else ChangeDisk(change); };
        Catch.Exception(() => Workspaces.ProposeRepair(Arguments(DestinationRequest()))).ShouldNotBeNull();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(change == "source" ? OriginalSource + "\n// disk drift\n" : OriginalSource);
        File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldBeFalse();
        FailureOf(() => Workspaces.ReadProposal(Arguments(new { proposalId = "not-retained" }))).ShouldEqual("UnknownProposal");
    }

    [Theory]
    [InlineData("content")]
    [InlineData("missing")]
    [InlineData("invalid-utf8")]
    void should_recheck_physical_candidate_inputs_after_staging_and_roll_back_before_installation(string change)
    {
        Start();
        var proposal = ProposeDestination();
        Workspaces.BeforeInstall = () => ChangeAttachment(change);
        var result = Install(proposal);
        result.GetProperty("success").GetBoolean().ShouldBeFalse();
        result.GetProperty("status").GetString().ShouldEqual("RolledBack");
        result.GetProperty("failureKind").GetString().ShouldEqual("RepairEvidenceDrift");
        result.GetProperty("installedDocuments").GetInt32().ShouldEqual(0);
        ShouldHaveNoInstalledSource();
        File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_foreign_workspace_and_catalog_expectations_before_resolving_intent()
    {
        Start();
        foreach (var field in new[] { "expectedRevision", "expectedCatalogRevision" })
        {
            var request = DestinationRequest();
            var current = request[field]!.GetValue<string>();
            request[field] = current[..^1] + (current[^1] == '0' ? '1' : '0');
            request["destinationIntent"] = null;
            var result = Content(Workspaces.ProposeRepair(Arguments(request)));
            result.GetProperty("success").GetBoolean().ShouldBeFalse();
            result.GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
        }
        ShouldHaveNoInstalledSource();
    }

    [Fact]
    void should_ignore_unreferenced_non_model_files_without_refreshing_the_retained_candidate()
    {
        Start();
        var proposal = ProposeDestination();
        var retained = Review(proposal).GetProperty("result").GetRawText();
        File.WriteAllText(Path.Combine(RootPath, "Unreferenced.cs"), "irrelevant");
        Review(proposal).GetProperty("result").GetRawText().ShouldEqual(retained);
        Install(proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    void ChangeAttachment(string change)
    {
        var path = Path.Combine(RootPath, "Handler.cs");
        switch (change)
        {
            case "content": File.WriteAllText(path, "changed physical input"); break;
            case "missing": File.Delete(path); break;
            case "invalid-utf8": File.WriteAllBytes(path, [0xff]); break;
            case "directory": File.Delete(path); Directory.CreateDirectory(path); break;
        }
    }

    void ChangeDisk(string change)
    {
        switch (change)
        {
            case "source": File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// disk drift\n"); break;
            case "membership": File.WriteAllText(Path.Combine(RootPath, "Added.play"), "concept Added : String\n"); break;
            case "identity-state":
                McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
                File.WriteAllText(Path.Combine(RootPath, ".screenplay", "identities.json"), "competing identity catalog");
                break;
        }
    }
}
