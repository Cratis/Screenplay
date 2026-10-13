// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>Checks proposal candidates without making release admission an authoring effect.</summary>
public static class WorkspaceReleaseBaseline
{
    /// <summary>Checks the frozen stored contracts, failing closed for contract changes with an unreadable baseline.</summary>
    /// <param name="before">The original workspace.</param>
    /// <param name="candidate">The complete proposal candidate.</param>
    /// <param name="baseline">The independent baseline read.</param>
    /// <returns>The shared structural verdict.</returns>
    public static ReleaseBaselineResult Check(ScreenplayWorkspace before, ScreenplayWorkspace candidate, ReleaseBaselineRead baseline)
    {
        if (baseline.Status == "absent") return new(null, []);
        if (baseline.Status != "unreadable")
        {
            if (!candidate.Compilation.Success && baseline.Document is { } document && document.ApplicationIdentity == candidate.IdentityCatalog.Application.ToString())
            {
                var revisions = WorkspaceSyntaxIndex.Create(candidate).Entries
                    .Where(entry => entry.Node is EventSyntax && entry.EventContractId is not null)
                    .Select(entry => (entry.EventContractId!.Value.ToString(), ((EventSyntax)entry.Node).Generation)).ToHashSet();
                var missing = document.Entries.Where(entry => !revisions.Contains((entry.Event, entry.Revision)))
                    .Select(entry => new ReleaseBaselineViolation("BaselinedRevisionMissing", entry.Event, entry.Revision)).ToImmutableArray();
                if (!missing.IsEmpty) return new(null, missing);
            }

            return ReleaseBaseline.Check(candidate, baseline);
        }
        return ContractsEqual(before, candidate) ? new(null, []) : new(null, [], "UnreadableReleaseBaseline", baseline.Reason);
    }

    static bool ContractsEqual(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        if (before.Compilation.Success && after.Compilation.Success)
        {
            var previous = Snapshot(before);
            var next = Snapshot(after);
            return previous.Entries.Length == next.Entries.Length && ReleaseBaseline.Check(after, new("valid", "temporary", previous)).Success;
        }

        // Authoring-only models cannot resolve stored schemas. Prove an unchanged contract syntax set instead;
        // a changed or unresolved set fails closed, rather than treating an unknown schema as mutable.
        return ContractSyntax(before).SequenceEqual(ContractSyntax(after), StringComparer.Ordinal);
    }

    static ReleaseBaselineDocument Snapshot(ScreenplayWorkspace workspace) => ReleaseBaseline.Admit(
        workspace, ReleaseBaselineStore.ReadBytes(null, workspace.IdentityCatalog.Application), "comparison", DateTimeOffset.UnixEpoch).Document!;

    static IEnumerable<string> ContractSyntax(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries
        .Select(entry => entry.Node switch
        {
            EventSyntax value => JsonSerializer.Serialize(new { kind = "event", id = entry.EventContractId?.ToString(), value.Generation, eventType = value.Id ?? value.Name, properties = Properties(value.Properties) }),
            TypeSyntax value => JsonSerializer.Serialize(new { kind = "type", id = entry.SemanticId?.ToString(), value.Name, properties = Properties(value.Properties) }),
            ConceptSyntax value => JsonSerializer.Serialize(new { kind = "concept", id = entry.SemanticId?.ToString(), value.Name, value.Type, value.Values, value.ValueNumbers, attributes = value.Attributes.Select(attribute => new { attribute.Name, attribute.Scope, attribute.SpecialCategory, attribute.Criminal }) }),
            _ => null
        }).OfType<string>().Order(StringComparer.Ordinal);

    static IEnumerable<object> Properties(IEnumerable<PropertySyntax> properties) => properties.Select(property => new { property.Name, type = property.Type.Name, property.Type.IsOptional, property.Type.IsCollection }).OrderBy(property => property.Name, StringComparer.Ordinal);
}
