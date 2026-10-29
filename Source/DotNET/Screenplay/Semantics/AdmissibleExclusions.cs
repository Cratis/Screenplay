// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleExclusions(AdmissibleSyntax index)
{
    readonly Dictionary<AdmissibleSyntax.Unit, SemanticExclusion> _entries = [];
    readonly HashSet<AdmissibleSyntax.Unit> _own = [];
    readonly Dictionary<AdmissibleSyntax.Unit, AdmissibleSyntax.Unit[]> _forcedBy = [];

    internal HashSet<SyntaxNode> Removed { get; } = [];
    internal IEnumerable<Entry> Entries => _entries.Select(entry => new Entry(entry.Value, _own.Contains(entry.Key),
        [.. _forcedBy.GetValueOrDefault(entry.Key, []).Select(unit => new SemanticExclusionReference(unit.Kind, unit.Address))]));
    internal IEnumerable<SliceSyntax> Slices => Removed.OfType<SliceSyntax>();

    internal bool Add(SyntaxNode node, IEnumerable<Diagnostic> diagnostics, string? reason, IEnumerable<SyntaxNode>? forcedBy = null)
    {
        var unit = index.Find(node);
        if (unit is null) return false;
        RecordProvenance(unit, forcedBy);
        if (_entries.TryGetValue(unit, out var previous))
        {
            diagnostics = previous.Diagnostics.Concat(diagnostics).Distinct();
            reason = string.Join(' ', new[] { previous.Reason, reason }.Where(value => value is not null).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        }
        _entries[unit] = new(unit.Kind, unit.Address, unit.ParentId, node.Location, [.. diagnostics]) { Reason = reason };
        Removed.Add(node);
        return true;
    }

    void RecordProvenance(AdmissibleSyntax.Unit unit, IEnumerable<SyntaxNode>? forcedBy)
    {
        if (forcedBy is null)
        {
            _own.Add(unit);
            _forcedBy.Remove(unit);
            return;
        }
        if (_own.Contains(unit)) return;
        _forcedBy.TryAdd(unit, [.. forcedBy.Select(index.Find).OfType<AdmissibleSyntax.Unit>().Where(cause => cause != unit).Distinct()]);
    }

    internal sealed record Entry(SemanticExclusion Exclusion, bool Own, IReadOnlyList<SemanticExclusionReference> ForcedBy);
}
