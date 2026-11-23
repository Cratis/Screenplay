// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff.given;

public class an_agent_exposure_comparison : a_semantic_comparison
{
    internal JsonElement ReadExposure(object? arguments = null) => JsonSerializer.SerializeToElement(McpSemanticDiff.ReadAgentExposure(Proposal, JsonSerializer.SerializeToElement(arguments ?? new { limit = 200 })), McpJson.Options);
}
