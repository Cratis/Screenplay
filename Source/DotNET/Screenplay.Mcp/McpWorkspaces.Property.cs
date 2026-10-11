// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object Property(JsonElement arguments)
    {
        var options = McpPropertyOptions.Read(arguments);
        var workspace = Current();
        var request = ReadPropertyRequest(arguments, options);
        Root.Verify(workspace);
        var result = workspace.ProposeProperty(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }

    static WorkspacePropertyRequest ReadPropertyRequest(JsonElement arguments, McpPropertyOptions options) => new()
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Action = options.Action,
            Target = arguments.TryGetProperty("target", out var target) ? McpAstHandles.Read(target) : throw new McpFailure("Property changes require a target handle.", -32602),
            ExpectedName = McpJson.RequiredString(arguments, "expectedName"),
            NewName = options.NewName(arguments),
            AlsoRename = PropertySelections(arguments, "alsoRename"),
            AlsoRemove = PropertySelections(arguments, "alsoRemove"),
            NewGeneration = options.NewGeneration,
            EventNeverPersisted = options.NeverPersisted,
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveTrivia),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };

    static ImmutableArray<WorkspacePropertySelection> PropertySelections(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var values)) return [];
        return [.. values.EnumerateArray().Select(value =>
        {
            McpJson.ValidateObject(value, ["target", "expectedName"], ["target", "expectedName"]);
            return new WorkspacePropertySelection(McpAstHandles.Read(value.GetProperty("target")), McpJson.RequiredString(value, "expectedName"));
        })];
    }

    static object? DescribeProperty(IMcpProposal proposal) => proposal is McpAuthoringProposal authored ? DescribeProperty(authored.Result.PropertyReport) : null;

    static object? DescribeProperty(WorkspacePropertyReport? report) => report is null ? null : new
    {
        report.Action,
        target = report.Target is { } target ? new { address = McpSemanticAddresses.Describe(target.Address), target.Kind, target.Generation } : null,
        changed = report.Changed.Select(change => new { address = McpSemanticAddresses.Describe(change.Address), change.Kind, change.Role, change.Change, change.Location, change.WeakensAssertion }),
        notChanged = report.NotChanged.Select(change => new { address = McpSemanticAddresses.Describe(change.Address), change.Kind, change.Reason }),
        needsDecision = report.NeedsDecision.Select(decision => new
        {
            decision.Reason,
            subject = decision.Subject is { } subject ? new { address = McpSemanticAddresses.Describe(subject.Address), subject.Kind, subject.Generation } : null,
            candidates = decision.Candidates.Select(DescribePropertyCandidate)
        }),
        chainCandidates = report.ChainCandidates.Select(candidate => new { address = McpSemanticAddresses.Describe(candidate.Address), candidate.Kind, candidate.Role, candidate.Reason }),
        generations = report.Generations.Select(generation => new { @event = McpSemanticAddresses.Describe(generation.Event), generation.From, generation.To, generation.Mode }),
        report.DeployableWithoutTransformation,
        report.EsmVersionChange,
        report.EsmVersionKnown
    };

    static object DescribePropertyCandidate(WorkspacePropertyCandidate candidate) => new { address = McpSemanticAddresses.Describe(candidate.Address), candidate.Kind, candidate.Role, candidate.Location };
}
