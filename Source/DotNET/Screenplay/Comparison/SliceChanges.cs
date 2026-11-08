// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Attributes every change of a structural comparison to the slice that owns it, or to the model as a whole.
/// </summary>
internal static class SliceChanges
{
    internal static SliceChangeSummary Summarize(StructuralDifference difference, AuthoringSnapshot before, AuthoringSnapshot after, Func<StructuralComparison.Change, ComparedDeclaration> declaration)
    {
        var slices = new SliceOwnership(before, after, difference.Changes);
        var entries = new SortedDictionary<string, SliceEntry>(StringComparer.Ordinal);
        var modelWide = new List<ComparedDeclaration>();
        var affected = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var change in difference.Changes.Where(change => change.Section is not ("identities" or "dependants")))
        {
            var owners = slices.Owners(change);
            if (owners.Count == 0)
            {
                modelWide.Add(declaration(change));
                continue;
            }

            foreach (var owner in owners)
            {
                var entry = GetEntry(entries, slices, owner);
                var compared = declaration(change);
                if (Classify(change, owners) is { } kind) entry.Kinds.Add(kind);
                if (change.Kind == "Slice") entry.SemanticId ??= compared.SemanticId;
                entry.Declarations.Add(compared);
            }
        }

        foreach (var dependant in difference.Changes.Where(change => change.Section == "dependants"))
        {
            var dependent = slices.Owner(dependant.DependantAddress, dependant.Snapshot == "before");
            if (dependent is null || SliceOwnership.IsRemoved(dependent)) continue;
            var owners = slices.Owners(dependant);
            if (owners.Count == 0) affected.Add(dependent);
            foreach (var owner in owners.Where(owner => owner != dependent)) GetEntry(entries, slices, owner).Dependents.Add(dependent);
        }

        foreach (var entry in entries.Values)
        {
            entry.Dependents.UnionWith(slices.Dependents(entry.AfterAddress, entry.BeforeAddress));
            affected.UnionWith(entry.Dependents);
        }

        affected.ExceptWith(entries.Keys);
        return new(
            [.. entries.Values.Where(entry => entry.Kinds.Count > 0).Select(entry => entry.ToChange()).OrderBy(change => change.Address, StringComparer.Ordinal)],
            [.. modelWide.Distinct()],
            [.. affected]);
    }

    static SliceChangeKind? Classify(StructuralComparison.Change change, IReadOnlyList<string> owners)
    {
        if (SliceOwnership.IsSlice(change))
        {
            return change.ChangeKind switch
            {
                "added" => SliceChangeKind.Added,
                "removed" => SliceChangeKind.Removed,
                "renamed" => SliceChangeKind.Renamed,
                _ => SliceChangeKind.Moved
            };
        }

        // A declaration that only follows its renamed or moved slice has not changed the slice's content.
        var followsItsSlice = change.Section == "declarations" && change.ChangeKind == "moved" && owners.Count == 1;
        return followsItsSlice ? null : SliceChangeKind.Changed;
    }

    static SliceEntry GetEntry(SortedDictionary<string, SliceEntry> entries, SliceOwnership slices, string key)
    {
        if (!entries.TryGetValue(key, out var entry))
        {
            var (beforeAddress, afterAddress) = slices.Addresses(key);
            entries[key] = entry = new(beforeAddress, afterAddress);
        }

        return entry;
    }
}
