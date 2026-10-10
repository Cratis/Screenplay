// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.when_reading_an_invalid_revision;

public class with_persisted_path_identities_from_a_different_application : given.an_export_with_a_different_application_identity
{
    JsonElement _result;

    void Establish()
    {
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        using var state = McpFileAccess.CreatePrivate(Path.Combine(RootPath, ".screenplay", "identities.json"));
        state.Write(McpState.Serialize(McpWorkspaceTransport.Restore(Opened.GetProperty("workspaceJson").GetString()!)));
    }

    void Because() => _result = Compare(new { before = new { path = RootPath }, after = new { workspaceJson = Export } });

    [Fact] void should_refuse_to_guess_identity_continuity() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_a_structured_failure() => _result.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("IncompatibleRevisions");
}
