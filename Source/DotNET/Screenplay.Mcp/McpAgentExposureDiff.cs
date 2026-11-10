// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Mcp;

static class McpAgentExposureDiff
{
    const int Version = 2;

    internal static object Read(AgentExposureComparison exposure, JsonElement arguments, string revision, string beforeRevision, string afterRevision)
    {
        if (McpJson.Integer(arguments, "version", 0, 0, int.MaxValue) != Version)
        {
            throw new McpFailure("'version' must equal 2 for the agent-exposure view; the requested and answered versions must match on every page.", -32602);
        }

        return new
        {
            version = Version,
            sourceRevision = revision,
            beforeRevision,
            afterRevision,
            comparisonLevel = "agent-exposure",
            coverage = exposure.Coverage.ToString(),
            hasWidening = exposure.HasWidening,
            page = McpPaging.BoundedSourcePage(
                exposure.Rows.Select(row => (object)new
                {
                    kind = row.Kind.ToString(),
                    qualifiedName = row.QualifiedName,
                    change = row.Change.ToString(),
                    row.Reason,
                    beforeScope = row.BeforeScope?.ToString(),
                    afterScope = row.AfterScope?.ToString(),
                    row.DiagnosticCode
                }),
                arguments,
                revision)
        };
    }
}
