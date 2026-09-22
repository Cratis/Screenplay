// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_rejecting_untrusted_apply_requests : given.a_connection
{
    JsonElement _unknown;
    JsonElement _stale;
    JsonElement _failedProposal;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var catalog = opened.GetProperty("catalogRevision").GetString();
        _unknown = Call("apply", new { proposalId = "client-forged", expectedRevision = revision, expectedCatalogRevision = catalog }).GetProperty("result");
        var workspace = Workspace();
        var proposed = Call("propose", new { operation = "move-document", documentId = workspace.Documents[0].Id.ToString(), path = "renamed.play", expectedRevision = revision, expectedCatalogRevision = catalog }).GetProperty("result").GetProperty("structuredContent");
        _stale = Call("apply", new { proposalId = proposed.GetProperty("proposalId").GetString(), expectedRevision = revision, expectedCatalogRevision = "stale" }).GetProperty("result");
        _failedProposal = Call("propose", new { operation = "move-document", documentId = workspace.Documents[0].Id.ToString(), path = "../outside.play", expectedRevision = revision, expectedCatalogRevision = catalog }).GetProperty("result");
    }

    [Fact] void should_reject_a_client_created_proposal_id() => _unknown.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_reject_a_stale_catalog_revision() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_reject_a_path_escape() => _failedProposal.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_leave_the_original_source_intact() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
