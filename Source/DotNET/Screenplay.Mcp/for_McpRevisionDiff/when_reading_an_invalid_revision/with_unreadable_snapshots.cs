// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.when_reading_an_invalid_revision;

public class with_unreadable_snapshots : given.two_snapshots
{
    List<JsonElement> _results = [];

    void Because()
    {
        var wrongRevision = JsonNode.Parse(BeforeJson)!;
        wrongRevision["revision"] = Proposal.Workspace.Revision.ToString();
        foreach (var unreadable in new[] { "not-json", "{}", "main", wrongRevision.ToJsonString(), BeforeJson + BeforeJson, BeforeJson.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal) })
        {
            _results.Add(Call(new { beforeWorkspaceJson = unreadable, afterWorkspaceJson = AfterJson }));
            _results.Add(Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = unreadable }));
        }
    }

    [Fact] void should_refuse_every_unreadable_revision() => _results.TrueForAll(result => result.GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_return_a_structured_failure() => _results.TrueForAll(result => result.GetProperty("structuredContent").GetProperty("failureKind").GetString() == "UnreadableRevision").ShouldBeTrue();
    [Fact] void should_identify_which_revision_was_unreadable() => _results.Select((result, index) => result.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains(index % 2 == 0 ? "beforeWorkspaceJson" : "afterWorkspaceJson", StringComparison.Ordinal)).All(value => value).ShouldBeTrue();
}
