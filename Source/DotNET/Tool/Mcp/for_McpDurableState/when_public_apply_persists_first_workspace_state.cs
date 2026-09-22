// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_public_apply_persists_first_workspace_state : for_McpConnection.given.a_connection
{
    JsonElement _applied;
    JsonElement _reopened;
    string _document = string.Empty;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        _document = Workspace().Documents.Single().Id.ToString();
        var proposal = Call("propose", new { expectedRevision, expectedCatalogRevision, operation = "move-document", documentId = _document, path = "moved.play" })
            .GetProperty("result").GetProperty("structuredContent");
        _applied = Call("apply", new { expectedRevision, expectedCatalogRevision, proposalId = proposal.GetProperty("proposalId").GetString() })
            .GetProperty("result").GetProperty("structuredContent");
        Connection = new(new McpTools(new McpRoot(RootPath)));
        Initialize();
        _reopened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_apply_both_sources_and_identity_state() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_reopen_without_exporting_or_importing_an_envelope() => _reopened.GetProperty("revision").GetString().ShouldEqual(_applied.GetProperty("workspace").GetProperty("revision").GetString());
    [Fact] void should_preserve_the_full_catalog() => _reopened.GetProperty("catalogRevision").GetString().ShouldEqual(_applied.GetProperty("workspace").GetProperty("catalogRevision").GetString());
    [Fact] void should_preserve_document_identity() => McpState.Deserialize(new McpManagedFiles(Root).Read(McpState.FileName)).Mappings.Single().Id.ToString().ShouldEqual(_document);
}
