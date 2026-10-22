// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpSpecificationObligations
{
    internal static object Read(McpSnapshot snapshot, JsonElement arguments) => McpReviewSelection.Page(snapshot, [.. Evaluate(snapshot, arguments).Select(Describe)], arguments, "Authored specification presence only, not execution or full behavioral coverage. Rules with explicit messages match exact then error text; unidentifiable rejection rules remain info/unmet. Matching specification lists are capped at 20; declaration-details pages the full inventory. Ambiguous references never establish a match.");

    internal static List<SpecificationObligation> Evaluate(McpSnapshot snapshot, JsonElement arguments)
    {
        try
        {
            return SpecificationObligations.Evaluate(snapshot.Authoring, McpReviewSelection.Declarations(snapshot, arguments), McpJson.OptionalString(arguments, "document"));
        }
        catch (InvalidWorkspaceAuthoring exception)
        {
            throw new McpFailure(exception.Message);
        }
    }

    static object Describe(SpecificationObligation obligation) => new
    {
        ruleId = obligation.RuleId,
        declaration = obligation.Declaration,
        location = obligation.Location,
        subject = obligation.Subject,
        description = obligation.Description,
        severity = obligation.Reason is null ? "warning" : "info",
        reason = obligation.Reason,
        status = obligation.Met ? "met" : "unmet",
        specificationCount = obligation.MetBy.Length,
        specifications = obligation.MetBy.Take(20).ToArray(),
        specificationsTruncated = obligation.MetBy.Length > 20
    };
}
