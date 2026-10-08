// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reviewing_semantic_diff_against_durable_state.given;

public class a_retained_proposal : for_McpConnection.given.a_connection
{
    internal McpManagedFiles Files = null!;
    internal ScreenplayWorkspace Original = null!;
    internal byte[] BeforeState = [];
    internal string ProposalId = string.Empty;
    internal string Revision = string.Empty;
    internal JsonElement First;

    void Establish()
    {
        Original = Workspace();
        BeforeState = McpState.Serialize(Original);
        Files = new(Root);
        McpManagedFiles.WritePrivate(Files.PathFor(McpState.FileName, create: true), BeforeState);
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var proposed = Call("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "slice"
        }).GetProperty("result").GetProperty("structuredContent");
        ProposalId = proposed.GetProperty("proposalId").GetString()!;
        First = Call("read-proposal", new { proposalId = ProposalId, view = "semantic-diff", limit = 1 }).GetProperty("result");
        Revision = First.GetProperty("structuredContent").GetProperty("result").GetProperty("sourceRevision").GetString()!;
    }

    internal JsonElement ReadFirst() => Call("read-proposal", new { proposalId = ProposalId, view = "semantic-diff", limit = 1 }).GetProperty("result");

    internal JsonElement Continue() => Call("read-proposal", new { proposalId = ProposalId, view = "semantic-diff", offset = 1, limit = 1, expectedSourceRevision = Revision }).GetProperty("result");
}
