// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Resolves which slice owns a declaration address on each side of a comparison, and which slices depend on a slice.
/// </summary>
/// <remarks>
/// Slices are keyed by their address after the comparison. A slice that only exists before is keyed by its before
/// address behind <see cref="RemovedPrefix"/>, so it can never collide with a slice that exists after.
/// </remarks>
internal sealed class SliceOwnership
{
    const char RemovedPrefix = '\u0001';

    readonly string[] _before;
    readonly string[] _after;
    readonly Dictionary<string, string> _beforeToAfter = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> _afterToBefore = new(StringComparer.Ordinal);
    readonly Lazy<ILookup<string, string>> _beforeDependents;
    readonly Lazy<ILookup<string, string>> _afterDependents;

    internal SliceOwnership(AuthoringSnapshot before, AuthoringSnapshot after, IEnumerable<StructuralComparison.Change> changes)
    {
        _before = Slices(before);
        _after = Slices(after);
        foreach (var change in changes.Where(change => IsSlice(change) && change.BeforeAddress is not null && change.AfterAddress is not null))
        {
            _beforeToAfter[change.BeforeAddress!] = change.AfterAddress!;
            _afterToBefore[change.AfterAddress!] = change.BeforeAddress!;
        }

        _beforeDependents = new(() => Dependents(before));
        _afterDependents = new(() => Dependents(after));
    }

    internal static bool IsRemoved(string key) => key.StartsWith(RemovedPrefix);

    internal static bool IsSlice(StructuralComparison.Change change) => change.Kind == "Slice" && change.Section == "declarations";

    internal IReadOnlyList<string> Owners(StructuralComparison.Change change)
    {
        var owners = new List<string>(2);
        if (Owner(change.AfterAddress, before: false) is { } after) owners.Add(after);
        if (Owner(change.BeforeAddress, before: true) is { } before && !owners.Contains(before)) owners.Add(before);
        return owners;
    }

    internal string? Owner(string? address, bool before)
    {
        if (address is null) return null;
        var slice = (before ? _before : _after).FirstOrDefault(slice => address == slice || (address.StartsWith(slice, StringComparison.Ordinal) && address.Length > slice.Length && address[slice.Length] == '.'));
        if (slice is null) return null;
        return before ? KeyFromBefore(slice) : slice;
    }

    internal (string? Before, string? After) Addresses(string key)
    {
        if (IsRemoved(key)) return (key[1..], null);
        if (_afterToBefore.TryGetValue(key, out var renamed)) return (renamed, key);
        return (_before.Contains(key, StringComparer.Ordinal) ? key : null, key);
    }

    internal IEnumerable<string> Dependents(string? afterAddress, string? beforeAddress) => afterAddress is not null
        ? _afterDependents.Value[afterAddress]
        : _beforeDependents.Value[beforeAddress!].Select(KeyFromBefore).Where(key => !IsRemoved(key));

    static string[] Slices(AuthoringSnapshot snapshot) =>
        [.. snapshot.Index.Declarations.Where(declaration => declaration.Kind == "Slice").Select(declaration => declaration.Address).Distinct(StringComparer.Ordinal).OrderByDescending(address => address.Length)];

    static ILookup<string, string> Dependents(AuthoringSnapshot snapshot) => snapshot.DependencyGraph.Implied("slice", "slice")
        .Where(dependency => dependency.Source.Address != dependency.Target.Address)
        .ToLookup(dependency => dependency.Target.Address, dependency => dependency.Source.Address, StringComparer.Ordinal);

    string KeyFromBefore(string slice)
    {
        if (_beforeToAfter.TryGetValue(slice, out var after)) return after;
        return _after.Contains(slice, StringComparer.Ordinal) ? slice : RemovedPrefix + slice;
    }
}
