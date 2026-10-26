// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Compares semantic models after normalisation.
/// </summary>
/// <remarks>
/// A missing or unexpected property or set element is reported once, at its own path, without descending into it. Leaves compare
/// by exact canonical text. Ordered arrays compare by position.
/// </remarks>
internal static class SemanticModelEquivalence
{
    /// <summary>
    /// Normalises and compares two semantic models.
    /// </summary>
    /// <param name="expected">The expected model.</param>
    /// <param name="actual">The actual model.</param>
    /// <returns>The comparison.</returns>
    /// <exception cref="SemanticNormalizationFailed">Either model cannot be normalised.</exception>
    public static SemanticModelComparison Compare(ExecutableSemanticModel expected, ExecutableSemanticModel actual) =>
        Compare(SemanticModelNormalizer.Normalize(expected), SemanticModelNormalizer.Normalize(actual));

    /// <summary>
    /// Compares two normalised semantic models.
    /// </summary>
    /// <param name="expected">The expected model.</param>
    /// <param name="actual">The actual model.</param>
    /// <returns>The comparison.</returns>
    public static SemanticModelComparison Compare(NormalizedSemanticModel expected, NormalizedSemanticModel actual)
    {
        var differences = new List<SemanticDifference>();
        Compare(expected.Root, actual.Root, string.Empty, differences);
        return new([.. differences.OrderBy(_ => _.Path, StringComparer.Ordinal)]);
    }

    static void Compare(NormalizedSemanticNode expected, NormalizedSemanticNode actual, string path, List<SemanticDifference> differences)
    {
        if (expected.Kind != actual.Kind)
        {
            differences.Add(new(PathOrRoot(path), SemanticDifferenceKind.Changed, expected.Text, actual.Text));
            return;
        }

        switch (expected.Kind)
        {
            case NormalizedSemanticNodeKind.Leaf:
                CompareLeaves(expected, actual, path, differences);
                break;
            case NormalizedSemanticNodeKind.Object:
                CompareMembers(expected, actual, name => $"{path}/{name}", differences);
                break;
            case NormalizedSemanticNodeKind.Set:
                CompareMembers(expected, actual, segment => $"{path}[{segment}]", differences);
                break;
            case NormalizedSemanticNodeKind.List:
                CompareItems(expected, actual, path, differences);
                break;
        }
    }

    static void CompareLeaves(NormalizedSemanticNode expected, NormalizedSemanticNode actual, string path, List<SemanticDifference> differences)
    {
        if (!string.Equals(expected.Value, actual.Value, StringComparison.Ordinal))
        {
            differences.Add(new(PathOrRoot(path), SemanticDifferenceKind.Changed, expected.Text, actual.Text));
        }
    }

    static void CompareMembers(NormalizedSemanticNode expected, NormalizedSemanticNode actual, Func<string, string> pathOf, List<SemanticDifference> differences)
    {
        foreach (var name in expected.Members.Keys.Union(actual.Members.Keys, StringComparer.Ordinal))
        {
            var inExpected = expected.Members.TryGetValue(name, out var expectedMember);
            var inActual = actual.Members.TryGetValue(name, out var actualMember);
            if (inExpected && inActual)
            {
                Compare(expectedMember!, actualMember!, pathOf(name), differences);
            }
            else
            {
                differences.Add(Absent(pathOf(name), inExpected ? expectedMember : null, inActual ? actualMember : null));
            }
        }
    }

    static void CompareItems(NormalizedSemanticNode expected, NormalizedSemanticNode actual, string path, List<SemanticDifference> differences)
    {
        for (var index = 0; index < Math.Max(expected.Items.Length, actual.Items.Length); index++)
        {
            var itemPath = $"{path}[{index.ToString(CultureInfo.InvariantCulture)}]";
            if (index < expected.Items.Length && index < actual.Items.Length)
            {
                Compare(expected.Items[index], actual.Items[index], itemPath, differences);
            }
            else
            {
                differences.Add(Absent(itemPath, index < expected.Items.Length ? expected.Items[index] : null, index < actual.Items.Length ? actual.Items[index] : null));
            }
        }
    }

    static SemanticDifference Absent(string path, NormalizedSemanticNode? expected, NormalizedSemanticNode? actual) =>
        expected is not null
            ? new(path, SemanticDifferenceKind.Missing, expected.Text, null)
            : new(path, SemanticDifferenceKind.Unexpected, null, actual!.Text);

    static string PathOrRoot(string path) => path.Length == 0 ? "/" : path;
}
