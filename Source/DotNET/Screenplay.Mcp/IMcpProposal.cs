// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

interface IMcpProposal
{
    ScreenplayWorkspace Before { get; }
    ScreenplayWorkspace Workspace { get; }
    WorkspaceWritePlan WritePlan { get; }
    bool Accepted { get; }
    string Validation { get; }
}
