// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.given;

public class two_snapshots : for_McpSemanticDiff.given.a_semantic_comparison
{
    internal string BeforeJson = null!;
    internal string AfterJson = null!;

    void Establish()
    {
        Propose(Source.Replace("event Registered\n        name String", "event Registered\n        name String\n        extra String", StringComparison.Ordinal));
        BeforeJson = Export(Proposal.Before);
        AfterJson = Export(Proposal.Workspace);
    }

    internal static string Export(ScreenplayWorkspace workspace) => Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(workspace));

    internal static JsonElement Call(object arguments) => JsonSerializer.SerializeToElement(new McpTools().Call(JsonSerializer.SerializeToElement(new { name = "semantic-diff", arguments })), McpJson.Options);

    internal JsonElement Compare(int offset = 0, int limit = 200, string? revision = null)
    {
        var arguments = new Dictionary<string, object>
        {
            ["beforeWorkspaceJson"] = BeforeJson,
            ["afterWorkspaceJson"] = AfterJson,
            ["offset"] = offset,
            ["limit"] = limit
        };
        if (revision is not null) arguments["expectedSourceRevision"] = revision;

        return Call(arguments);
    }
}
