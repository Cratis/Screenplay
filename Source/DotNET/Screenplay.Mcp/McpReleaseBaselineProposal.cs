// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record McpReleaseBaselineProposal(ScreenplayWorkspace Before, byte[]? BeforeBaseline, byte[] AfterBaseline) : IMcpProposal
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
    public string Validation => "ReleaseBaseline";
}

sealed record McpReleaseBaselineJournal(byte[]? Before, byte[] After, McpRecoveryAccess? Access);
