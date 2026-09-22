// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_proposing_against_unbindable_source : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("name ProjectName", "name MissingType", StringComparison.Ordinal));
        Initialize();
    }

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Demo" }).GetProperty("result").GetProperty("structuredContent");
        using var envelope = JsonDocument.Parse(opened.GetProperty("workspaceJson").GetString()!);
        _result = Call("propose", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            operation = "move-document",
            documentId = envelope.RootElement.GetProperty("documents")[0].GetProperty("id").GetString(),
            path = "renamed.play"
        }).GetProperty("result");
    }

    [Fact] void should_not_create_an_applicable_proposal() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_typed_conflicts() => _result.GetProperty("structuredContent").GetProperty("conflicts").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_keep_disk_unchanged() => File.Exists(Path.Combine(RootPath, "renamed.play")).ShouldBeFalse();
}
