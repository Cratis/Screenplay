// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Collects what changed in one slice while a comparison is summarized.
/// </summary>
/// <param name="beforeAddress">The slice address before the comparison, when it existed.</param>
/// <param name="afterAddress">The slice address after the comparison, when it exists.</param>
internal sealed class SliceEntry(string? beforeAddress, string? afterAddress)
{
    internal string? BeforeAddress { get; } = beforeAddress;

    internal string? AfterAddress { get; } = afterAddress;

    internal SemanticId? SemanticId { get; set; }

    internal HashSet<SliceChangeKind> Kinds { get; } = [];

    internal List<ComparedDeclaration> Declarations { get; } = [];

    internal SortedSet<string> Dependents { get; } = new(StringComparer.Ordinal);

    internal SliceChange ToChange()
    {
        return new(Significant(), BeforeAddress, AfterAddress, SemanticId, [.. Declarations.Distinct()], [.. Dependents]);
    }

    SliceChangeKind[] Significant()
    {
        // An added or removed slice is wholly new or gone, so nothing else about it is worth reporting.
        if (Kinds.Contains(SliceChangeKind.Added)) return [SliceChangeKind.Added];
        if (Kinds.Contains(SliceChangeKind.Removed)) return [SliceChangeKind.Removed];
        return [.. Kinds.Order()];
    }
}
