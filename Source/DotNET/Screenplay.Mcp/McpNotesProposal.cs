// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record McpNotesPlan(byte[]? Before, byte[] After, string BeforeRevision, string AfterRevision);

sealed record McpNotesProposal(ScreenplayWorkspace Before, McpNotesPlan Notes) : IMcpProposal
{
    public ScreenplayWorkspace Workspace => Before;
    public WorkspaceWritePlan WritePlan => new()
    {
        BeforeRevision = Before.Revision,
        AfterRevision = Before.Revision,
        BeforeCatalogRevision = Before.IdentityCatalog.Revision,
        AfterCatalogRevision = Before.IdentityCatalog.Revision
    };
    public bool Accepted => true;
    public string Validation => "Notes";
}

sealed record McpNotesJournal(byte[]? Before, byte[] After, McpRecoveryAccess? Access);
