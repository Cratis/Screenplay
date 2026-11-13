// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Places a new child in a children collection the way its <see cref="SemanticProjectionChildrenOrdering"/> says.
/// </summary>
/// <remarks>
/// The order is stored, as Chronicle stores it (Chronicle#5758): a new child is appended, prepended, or appended and then the
/// collection is stably sorted by the ordering's property - a child without a value first when ascending and last when
/// descending. Only adding a child places it; an update to an existing child is written where the child already is.
/// </remarks>
internal static class SemanticChildrenOrder
{
    /// <summary>
    /// Adds a new child to a collection.
    /// </summary>
    /// <param name="elements">The children the collection holds.</param>
    /// <param name="child">The new child.</param>
    /// <param name="ordering">The <see cref="SemanticProjectionChildrenOrdering"/> of the collection, or <see langword="null"/> to append.</param>
    /// <returns>The collection with the child placed.</returns>
    public static ImmutableArray<SemanticValue> Insert(ImmutableArray<SemanticValue> elements, SemanticValue child, SemanticProjectionChildrenOrdering? ordering)
    {
        switch (ordering?.Placement)
        {
            case SemanticChildrenPlacement.Prepend:
                return elements.Insert(0, child);
            case SemanticChildrenPlacement.Sorted:
                var added = elements.Add(child);
                var sorted = ordering.Direction == SemanticSortDirection.Descending
                    ? added.OrderByDescending(element => Read(element, ordering.SortBy), Comparer.Instance)
                    : added.OrderBy(element => Read(element, ordering.SortBy), Comparer.Instance);
                return [.. sorted];
            default:
                return elements.Add(child);
        }
    }

    static SemanticValue Read(SemanticValue element, ImmutableArray<SemanticId> path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current is not SemanticCompositeValue composite ||
                composite.Properties.FirstOrDefault(property => property.TargetProperty == segment) is not { } property)
            {
                return SemanticValue.Null;
            }

            current = property.Value;
        }

        return current;
    }

    // A value without one sorts below every value, so it comes first ascending and last descending.
    sealed class Comparer : IComparer<SemanticValue>
    {
        public static readonly Comparer Instance = new();

        public int Compare(SemanticValue? x, SemanticValue? y) => (x, y) switch
        {
            (null or SemanticNullValue, null or SemanticNullValue) => 0,
            (null or SemanticNullValue, _) => -1,
            (_, null or SemanticNullValue) => 1,
            (SemanticNumberValue left, SemanticNumberValue right) => left.Value.CompareTo(right.Value),
            (SemanticTextValue left, SemanticTextValue right) => string.CompareOrdinal(left.Value, right.Value),
            (SemanticBooleanValue left, SemanticBooleanValue right) => left.Value.CompareTo(right.Value),
            _ => x.Kind.CompareTo(y.Kind)
        };
    }
}
