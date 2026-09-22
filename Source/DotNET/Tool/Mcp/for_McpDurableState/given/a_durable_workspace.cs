// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState.given;

public class a_durable_workspace : for_McpConnection.given.a_connection
{
    internal ScreenplayWorkspace Original = null!;
    internal McpProposal Proposal = null!;
    internal McpManagedFiles Files = null!;
    internal byte[] OriginalState = [];

    void Establish()
    {
        Original = Workspace();
        Proposal = Rename(Original);
        Files = new(Root);
        OriginalState = McpState.Serialize(Original);
        McpManagedFiles.WritePrivate(Files.PathFor(McpState.FileName, create: true), OriginalState);
    }

    internal McpRecoveryJournal Prepare() => McpRecoveryJournal.Prepare(Root, Proposal, new(OriginalState, McpState.Serialize(Proposal.Workspace)));

    internal void Interrupt(McpRecoveryJournal journal, bool installState)
    {
        var change = journal.Changes.Single();
        McpManagedFiles.WritePrivate(change.Stage, [.. change.Entry.After.Bytes]);
        File.Move(Root.PathFor(change.Entry.Before.Path), change.Backup);
        File.Move(change.Stage, Root.PathFor(change.Entry.After.Path));
        if (installState)
        {
            File.Move(Files.PathFor(McpState.FileName), journal.StateBackup);
            McpManagedFiles.WritePrivate(Files.PathFor(McpState.FileName), journal.Record.AfterState);
        }
    }

    internal static JsonElement Arguments(object value) => JsonSerializer.SerializeToElement(value);
    internal static JsonElement Result(object value) => JsonSerializer.SerializeToElement(value).GetProperty("structuredContent");
}
