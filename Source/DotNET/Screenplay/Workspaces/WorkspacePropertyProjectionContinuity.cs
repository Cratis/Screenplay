// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

// Compare effective writes, not just AutoMap links. An explicit replacement and its former AutoMap
// must be equivalent for EVERY feed and variant consuming the physical mapping slot.
sealed class WorkspacePropertyProjectionContinuity(WorkspacePropertyPlan before, WorkspacePropertyRequest request, IReadOnlyList<WorkspacePropertyPlan.PropertyMember> selected)
{
    internal WorkspacePropertyRefusal? Inspect(ScreenplayWorkspace candidate)
    {
        var after = new WorkspacePropertyPlan(WorkspaceSyntaxIndex.Create(candidate));
        var expected = before.Links.ProjectionEffects.Where(effect => !Removed(effect)).Select(effect => Snapshot(effect, true)).ToArray();
        var actual = after.Links.ProjectionEffects.Select(effect => Snapshot(effect, false)).ToArray();
        var differences = Differences(expected, actual).Concat(Differences(actual, expected)).ToArray();
        return differences.Length == 0 ? null : new(WorkspaceConflictKind.AmbiguousPropertyReference, [.. differences.Select(OriginalCandidate).Distinct()]);
    }

    static IEnumerable<EffectSnapshot> Differences(EffectSnapshot[] left, EffectSnapshot[] right)
    {
        var counts = right.GroupBy(snapshot => snapshot.Key).ToDictionary(group => group.Key, group => group.Count());
        return left.GroupBy(snapshot => snapshot.Key).Where(group => group.Count() != counts.GetValueOrDefault(group.Key)).SelectMany(group => group);
    }

    bool Removed(WorkspacePropertyProjectionEffect effect) => request.Action == "remove" && effect.Targets.Concat(effect.Sources.SelectMany(source => source.Members)).Any(IsSelected);

    bool IsSelected(AuthoredDeclaration declaration) => selected.Any(member => member.Declaration == declaration);

    string PlannedName(AuthoredDeclaration declaration, bool planned) => planned && request.Action == "rename" && IsSelected(declaration) ? request.NewName! : declaration.Name;

    string Endpoint(AuthoredDeclaration declaration, bool planned) => string.Join('.', declaration.Scope.Append(PlannedName(declaration, planned)));

    EffectSnapshot Snapshot(WorkspacePropertyProjectionEffect effect, bool planned)
    {
        var mapping = WorkspaceSyntaxMutation.Json(effect.Mapping);
        if (effect.Targets is [var target]) mapping["property"] = PlannedName(target, planned);
        RewritePaths(mapping, effect.Sources, planned);
        var key = new EffectKey(
            effect.Feed.Handle.Document,
            effect.Feed.Handle.Path,
            string.Join(',', effect.Events.Select(@event => @event.Address)),
            string.Join(',', effect.Targets.Select(target => Endpoint(target, planned))),
            mapping.ToJsonString(),
            effect.Complete);
        return new(key, effect, planned);
    }

    void RewritePaths(JsonNode node, WorkspacePropertyProjectionSource[] sources, bool planned)
    {
        if (node is JsonObject value) RewritePath(value, sources, planned);
        JsonNode?[] children = node switch
        {
            JsonObject @object => [.. @object.Select(pair => pair.Value)],
            JsonArray array => [.. array],
            _ => []
        };
        foreach (var child in children.OfType<JsonNode>()) RewritePaths(child, sources, planned);
    }

    void RewritePath(JsonObject value, WorkspacePropertyProjectionSource[] sources, bool planned)
    {
        if (value["kind"]?.GetValue<string>() != nameof(PathExpressionSyntax) || value["path"] is not { } path) return;
        var source = sources.FirstOrDefault(source => source.Path == path.GetValue<string>());
        if (source?.Members is [var member]) value["path"] = PlannedName(member, planned);
    }

    WorkspacePropertyCandidate OriginalCandidate(EffectSnapshot snapshot)
    {
        var effect = snapshot.Effect;
        var members = effect.Targets.Concat(effect.Sources.SelectMany(source => source.Members));
        var member = snapshot.Planned ? members.Select(before.Member).OfType<WorkspacePropertyPlan.PropertyMember>().FirstOrDefault() :
            before.Members.FirstOrDefault(member => !before.Historical(member.Owner) && members.Any(declaration => Endpoint(member.Declaration, true) == declaration.Address));
        member ??= selected[0];
        return new(member.Entry.Address!, member.Kind, "mapping-target", before.LocationAtPosition(effect.Feed.Handle));
    }

    sealed record EffectSnapshot(EffectKey Key, WorkspacePropertyProjectionEffect Effect, bool Planned);
    sealed record EffectKey(DocumentId Document, string Feed, string Event, string Target, string Mapping, bool Complete);
}
