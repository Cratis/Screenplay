// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Where a lineage trace starts: a command, event or read-model property, or a screen field bound to one.
sealed record McpLineageSubject(string Kind, string Address, SourceLocation Location, McpDeclaration? Member, WorkspacePropertyLink? Field);
