// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Tool.Mcp;

sealed record McpReference(string Name, string[] Kinds, string[] Scope, SourceLocation Location, string Role = "reference", McpReadOwner? Owner = null);
