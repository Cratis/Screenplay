// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

sealed record McpReference(string Name, string[] Kinds, string[] Scope, SourceLocation Location, string Role = "reference", McpReadOwner? Owner = null)
{
    // Resolve event/operation names before validating the reference's required kind.
    // Use the snapshot's complete physical candidate view, not an assembled selection.
    [JsonIgnore]
    internal bool UseProductionCandidates { get; init; }
}
