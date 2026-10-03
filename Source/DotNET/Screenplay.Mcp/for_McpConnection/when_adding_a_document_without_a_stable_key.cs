// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_adding_a_document_without_a_stable_key : given.a_connection
{
    JsonElement _opened;
    JsonElement _proposal;
    JsonElement _applied;
    string _identities = null!;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        _opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        _proposal = Call("propose", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            operation = "add-document",
            path = "application.play",
            bytesBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Source))
        }).GetProperty("result").GetProperty("structuredContent");
        _applied = Call("apply", new
        {
            proposalId = _proposal.GetProperty("proposalId").GetString(),
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result").GetProperty("structuredContent");
        _identities = File.ReadAllText(Path.Combine(RootPath, ".screenplay", "identities.json"));
    }

    [Fact] void should_accept_the_proposal() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_apply_the_document() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_derive_the_stable_key_from_the_path() =>
        JsonDocument.Parse(_identities).RootElement.GetProperty("documents")[0].GetProperty("stableKey").GetString().ShouldEqual("application");
}
