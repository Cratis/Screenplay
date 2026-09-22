// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed record McpProposal(ScreenplayWorkspace Before, WorkspaceTransactionResult Transaction) : IMcpProposal
{
    public ScreenplayWorkspace Workspace => Transaction.Workspace!;
    public WorkspaceWritePlan WritePlan => Transaction.WritePlan!;
    public bool Accepted => Transaction.Success && Workspace.Compilation.Success;
    public string Validation => "Executable";
}
