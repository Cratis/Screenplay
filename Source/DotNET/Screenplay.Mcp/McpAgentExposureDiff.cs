// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// A separately versioned opt-in view; default semantic-diff bytes remain frozen.
static class McpAgentExposureDiff
{
    internal static object Read(ScreenplayWorkspace before, ScreenplayWorkspace after, JsonElement arguments, string revision, bool matchByAddress)
    {
        var difference = AgentExposureComparison.Compare(before, after, matchByAddress ? DeclarationMatching.Address : DeclarationMatching.Identity);
        return new
        {
            schema = "agent-exposure-diff/1",
            sourceRevision = revision,
            beforeRevision = before.Revision.ToString(),
            afterRevision = after.Revision.ToString(),
            comparisonLevel = "explicit-agent-allowlist",
            matching = Name(difference.Matching),
            complete = difference.Complete,
            hasExposureChange = difference.HasExposureChange,
            beforeAvailability = Name(difference.BeforeAvailability),
            afterAvailability = Name(difference.AfterAvailability),
            notCompared = difference.NotCompared,
            page = McpPaging.BoundedSourcePage(difference.Changes.Select(Project), arguments, revision)
        };
    }

    static object Project(AgentExposureChange change) => new
    {
        change = Name(change.Change),
        before = Operation(change.Before),
        after = Operation(change.After),
        scopeChange = Name(change.ScopeChange)
    };

    static object? Operation(AgentExposureOperation? operation) => operation is null ? null : new
    {
        kind = Name(operation.Kind),
        id = operation.Id?.ToString(),
        qualifiedName = operation.QualifiedName,
        scope = Name(operation.Scope),
        description = operation.Description
    };

    static string Name<T>(T value) where T : Enum => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
