// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed record McpAuthoringProposal(ScreenplayWorkspace Before, WorkspaceAuthoringResult Result, WorkspaceAuthoringValidation Requirement, WorkspaceAuthoringReferencePolicy ReferencePolicy = WorkspaceAuthoringReferencePolicy.Safe) : IMcpProposal
{
    public ScreenplayWorkspace Workspace => Result.Workspace!;
    public WorkspaceWritePlan WritePlan => Result.WritePlan!;
    public bool Accepted => Result.Accepted && (Requirement == WorkspaceAuthoringValidation.Authoring || Result.ExecutableReady);
    public string Validation => Requirement.ToString();
}
