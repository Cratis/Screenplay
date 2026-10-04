// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Records, for one authoring transaction, which candidate occurrences descend from which original occurrences.
/// Correspondence comes only from the transaction's own operations: untouched and moved documents, nodes carried
/// through typed edits, the replaced node of a replacement and its unchanged singular children, moved subtrees,
/// shape-preserving rename rewrites, and uniquely demonstrable absence assertions inside a replaced region.
/// A candidate occurrence without provenance is new, whatever its physical path or collection index.
/// </summary>
sealed class WorkspaceEditProvenance
{
    readonly Dictionary<(DocumentId Document, string Path), (DocumentId Document, string Path)> _origins = [];
    readonly Dictionary<(DocumentId Document, string Path), (DocumentId Document, string Path)> _images = [];
    readonly HashSet<(DocumentId Document, string Path)> _ambiguous = [];

    internal (DocumentId Document, string Path)? Origin(WorkspaceSyntaxEntry entry) =>
        _origins.TryGetValue((entry.Handle.Document, entry.Handle.Path), out var origin) ? origin : null;

    internal (DocumentId Document, string Path)? Image(WorkspaceSyntaxEntry original) =>
        _images.TryGetValue((original.Handle.Document, original.Handle.Path), out var image) ? image : null;

    internal bool IsAmbiguous(WorkspaceSyntaxEntry entry) => _ambiguous.Contains((entry.Handle.Document, entry.Handle.Path));

    internal void Ambiguous((DocumentId Document, string Path) position) => _ambiguous.Add(position);

    internal void Map((DocumentId Document, string Path) candidate, (DocumentId Document, string Path) original)
    {
        _origins[candidate] = original;
        _images[original] = candidate;
    }

    /// <summary>
    /// Maps every original occurrence under a subtree proven identical to the candidate subtree at another position.
    /// </summary>
    internal void Subtree(WorkspaceSyntaxIndex before, (DocumentId Document, string Path) original, (DocumentId Document, string Path) candidate)
    {
        foreach (var entry in Under(before, original))
        {
            Map((candidate.Document, candidate.Path + entry.Handle.Path[original.Path.Length..]), (entry.Handle.Document, entry.Handle.Path));
        }
    }

    /// <summary>
    /// Maps a replaced JSON subtree: the replacement corresponds to the original, as does every singular child of the
    /// same syntax kind; a child member or collection proven structurally unchanged corresponds entirely.
    /// </summary>
    internal void Replacement(JsonNode original, (DocumentId Document, string Path) originalPosition, JsonNode replacement, (DocumentId Document, string Path) position)
    {
        Map(position, originalPosition);
        if (original is not JsonObject before || replacement is not JsonObject after)
        {
            return;
        }

        foreach (var (name, value) in after)
        {
            var previous = before[name];
            if (value is not (JsonObject or JsonArray) || previous is null)
            {
                continue;
            }

            var childOriginal = (originalPosition.Document, $"{originalPosition.Path}/{name}");
            var child = (position.Document, $"{position.Path}/{name}");
            if (JsonNode.DeepEquals(previous, value))
            {
                Identical(previous, childOriginal, value, child);
            }
            else if (value is JsonObject current && previous is JsonObject prior && Kind(current) is { } kind && kind == Kind(prior))
            {
                Replacement(prior, childOriginal, current, child);
            }
        }
    }

    /// <summary>
    /// Maps two structurally identical JSON subtrees position by position.
    /// </summary>
    internal void Identical(JsonNode original, (DocumentId Document, string Path) originalPosition, JsonNode candidate, (DocumentId Document, string Path) position)
    {
        if (original is JsonObject before && candidate is JsonObject after)
        {
            Map(position, originalPosition);
            foreach (var (name, value) in after)
            {
                if (value is not null && before[name] is { } previous)
                {
                    Identical(previous, (originalPosition.Document, $"{originalPosition.Path}/{name}"), value, (position.Document, $"{position.Path}/{name}"));
                }
            }
        }
        else if (original is JsonArray priorItems && candidate is JsonArray items && priorItems.Count == items.Count)
        {
            for (var index = 0; index < items.Count; index++)
            {
                if (priorItems[index] is { } previous && items[index] is { } value)
                {
                    Identical(previous, (originalPosition.Document, $"{originalPosition.Path}/{index}"), value, (position.Document, $"{position.Path}/{index}"));
                }
            }
        }
    }

    /// <summary>
    /// Corresponds absence assertions inside a replaced region only when an unclaimed original with the same owner is
    /// structurally identical to exactly one unclaimed candidate and vice versa. Anything else is new; a candidate with
    /// several equally plausible originals, or an original claimed by several candidates, is ambiguous.
    /// </summary>
    internal void Region(WorkspaceSyntaxIndex before, (DocumentId Document, string Path) original, WorkspaceSyntaxIndex after, (DocumentId Document, string Path) candidate)
    {
        var originals = Under(before, original).Where(entry => entry.Node is SpecificationAbsentReadModelSyntax && !_images.ContainsKey((entry.Handle.Document, entry.Handle.Path))).ToArray();
        var candidates = Under(after, candidate).Where(entry => entry.Node is SpecificationAbsentReadModelSyntax && Origin(entry) is null).ToArray();
        var matches = candidates.ToDictionary(
            entry => entry,
            entry => originals.Where(previous => Equals(Owner(before, previous), Owner(after, entry)) && SyntaxJson.StructurallyEqual(previous.Node, entry.Node)).ToArray());
        foreach (var (entry, equal) in matches)
        {
            if (equal.Length == 0)
            {
                continue;
            }

            if (equal.Length == 1 && matches.Count(other => other.Value.Contains(equal[0])) == 1)
            {
                Subtree(before, (equal[0].Handle.Document, equal[0].Handle.Path), (entry.Handle.Document, entry.Handle.Path));
            }
            else
            {
                _ambiguous.Add((entry.Handle.Document, entry.Handle.Path));
            }
        }
    }

    static IEnumerable<WorkspaceSyntaxEntry> Under(WorkspaceSyntaxIndex index, (DocumentId Document, string Path) root) =>
        index.Entries.Where(entry => entry.Handle.Document == root.Document &&
            (entry.Handle.Path == root.Path || entry.Handle.Path.StartsWith($"{root.Path}/", StringComparison.Ordinal)));

    static SemanticAddress? Owner(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry entry)
    {
        for (var current = entry.Parent is { } parent ? index.Find(parent) : null; current is not null; current = current.Parent is { } ancestor ? index.Find(ancestor) : null)
        {
            if (current.Address is { } address)
            {
                return address;
            }
        }

        return null;
    }

    static string? Kind(JsonObject node) => node["kind"] is JsonValue value && value.TryGetValue<string>(out var kind) ? kind : null;
}
