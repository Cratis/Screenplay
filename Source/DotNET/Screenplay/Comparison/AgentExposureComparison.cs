// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison;

static class AgentExposureComparison
{
    internal static AgentExposureDifference Compare(ScreenplayWorkspace before, ScreenplayWorkspace after, DeclarationMatching matching)
    {
        var beforeAvailability = Availability(before);
        var afterAvailability = Availability(after);
        var complete = beforeAvailability == AgentExposureAvailability.Available && afterAvailability == AgentExposureAvailability.Available;
        var changes = complete ? Changes(Operations(before, matching), Operations(after, matching)).OrderBy(change => (change.After ?? change.Before)!.QualifiedName, StringComparer.Ordinal)
            .ThenBy(change => (change.After ?? change.Before)!.Kind).ThenBy(change => change.Change).ToImmutableArray() : [];
        return new(
            matching,
            complete,
            complete ? changes.Length > 0 : null,
            beforeAvailability,
            afterAvailability,
            changes,
            [
                "Explicit allowlist membership, required scope and description only; not effective authorization, transitive dependencies, handlers or runtime equivalence.",
                matching == DeclarationMatching.Address ? "Address matching does not establish identity continuity across renames or moves." : "Identity matching uses the supplied authoritative catalogs within one application."
            ]);
    }

    static AgentExposureAvailability Availability(ScreenplayWorkspace workspace) => workspace.Compilation.Success && workspace.Compilation.Value is not null
        ? AgentExposureAvailability.Available : AgentExposureAvailability.CompilationFailed;

    static Dictionary<(SemanticAgentOperationKind Kind, string Target), AgentExposureOperation> Operations(ScreenplayWorkspace workspace, DeclarationMatching matching)
    {
        var application = workspace.Compilation.Value!.Model.Application;
        var targets = SemanticAgentOperationCatalog.Of(application).ToDictionary(target => (target.Kind, target.Id));
        return (application.Agents?.Operations ?? []).ToDictionary(operation => Key(operation, targets[(operation.Kind, operation.Operation)].QualifiedName, matching), operation =>
        {
            var name = targets[(operation.Kind, operation.Operation)].QualifiedName;
            return new AgentExposureOperation(operation.Kind, matching == DeclarationMatching.Identity ? operation.Operation : null, name, operation.Scope, operation.Description);
        });
    }

    static (SemanticAgentOperationKind, string) Key(SemanticAgentOperation operation, string name, DeclarationMatching matching) =>
        (operation.Kind, matching == DeclarationMatching.Identity ? operation.Operation.ToString() : name);

    static IEnumerable<AgentExposureChange> Changes(Dictionary<(SemanticAgentOperationKind Kind, string Target), AgentExposureOperation> before, Dictionary<(SemanticAgentOperationKind Kind, string Target), AgentExposureOperation> after)
    {
        foreach (var key in before.Keys.Union(after.Keys))
        {
            before.TryGetValue(key, out var previous);
            after.TryGetValue(key, out var current);
            foreach (var change in EntryChanges(previous, current)) yield return change;
        }
    }

    static IEnumerable<AgentExposureChange> EntryChanges(AgentExposureOperation? before, AgentExposureOperation? after)
    {
        if (before is null)
        {
            yield return new(AgentExposureChangeKind.Added, null, after);
        }
        else if (after is null)
        {
            yield return new(AgentExposureChangeKind.Removed, before, null);
        }
        else
        {
            if (before.Scope != after.Scope)
            {
                yield return new(
                    AgentExposureChangeKind.ScopeChanged,
                    before,
                    after,
                    after.Scope == SemanticAgentScope.Read ? AgentExposureScopeChange.Widened : AgentExposureScopeChange.Narrowed);
            }

            if (before.Description != after.Description) yield return new(AgentExposureChangeKind.DescriptionChanged, before, after);
        }
    }
}
