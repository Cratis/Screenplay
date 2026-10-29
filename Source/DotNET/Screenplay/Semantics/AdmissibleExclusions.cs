// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleExclusions(AdmissibleSyntax index, AdmissibleCauses causes)
{
    readonly Dictionary<AdmissibleSyntax.Unit, SemanticExclusion> _entries = [];

    internal HashSet<SyntaxNode> Removed { get; } = [];
    internal IEnumerable<SemanticExclusion> Entries => _entries.Values;
    internal IEnumerable<SliceSyntax> Slices => Removed.OfType<SliceSyntax>();

    internal void Link(SyntaxNode source, SyntaxNode target)
    {
        if (index.Find(source) is { } from && index.Find(target) is { } to) causes.Link(from, to);
    }

    internal bool Add(SyntaxNode node, IEnumerable<Diagnostic> diagnostics, string? reason)
    {
        var unit = index.Find(node);
        if (unit is null) return false;
        if (_entries.TryGetValue(unit, out var previous))
        {
            diagnostics = previous.Diagnostics.Concat(diagnostics).Distinct();
            reason = string.Join(' ', new[] { previous.Reason, reason }.Where(value => value is not null).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        }
        _entries[unit] = new(unit.Kind, unit.Address, unit.ParentId, node.Location, [.. diagnostics]) { Reason = reason };
        Removed.Add(node);
        return true;
    }
}
