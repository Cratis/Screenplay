// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpDiskChange(WorkspaceWriteEntry entry)
{
    internal WorkspaceWriteEntry Entry { get; } = entry;
    internal string? Stage { get; set; }
    internal string? Backup { get; set; }
    internal bool Installed { get; set; }
}
