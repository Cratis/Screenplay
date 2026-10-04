// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

sealed record McpReference(string Name, string[] Kinds, string[] Scope, SourceLocation Location, string Role = "reference", McpReadOwner? Owner = null)
{
    // Resolve event/operation names before validating the reference's required kind.
    // Keep syntax evidence private: wire results expose declarations, not compiler nodes.
    [JsonIgnore]
    internal AuthoringProductionResolution? ProductionResolution { get; init; }
}
