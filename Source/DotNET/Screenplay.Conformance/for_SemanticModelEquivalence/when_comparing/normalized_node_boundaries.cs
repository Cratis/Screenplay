// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class normalized_node_boundaries : Specification
{
    [Theory]
    [InlineData("1", "1", 0)]
    [InlineData("1", "1.0", 1)]
    [InlineData("\"A\"", "\"a\"", 1)]
    public void should_compare_leaves_ordinally_at_the_root(string expected, string actual, int count)
    {
        var comparison = Compare(NormalizedSemanticNode.Leaf(expected), NormalizedSemanticNode.Leaf(actual));
        comparison.Differences.Length.ShouldEqual(count);
        if (count != 0) comparison.Differences.Single().ShouldEqual(new SemanticDifference("/", SemanticDifferenceKind.Changed, expected, actual));
    }

    [Fact]
    public void should_report_only_the_parent_for_a_root_kind_mismatch()
    {
        var comparison = Compare(NormalizedSemanticNode.Object([new("child", NormalizedSemanticNode.Leaf("1"))]), NormalizedSemanticNode.List([NormalizedSemanticNode.Leaf("2")]));
        Assert.Equal(new[] { new SemanticDifference("/", SemanticDifferenceKind.Changed, "{\"child\":1}", "[2]") }, comparison.Differences);
    }

    [Fact]
    public void should_report_missing_and_unexpected_composites_once_in_ordinal_path_order()
    {
        var composite = NormalizedSemanticNode.Object([new("child", NormalizedSemanticNode.List([NormalizedSemanticNode.Leaf("1")]))]);
        var comparison = Compare(NormalizedSemanticNode.Object([new("z", composite)]), NormalizedSemanticNode.Object([new("a", composite)]));
        Assert.Equal(
            new[]
            {
                new SemanticDifference("/a", SemanticDifferenceKind.Unexpected, null, "{\"child\":[1]}"),
                new SemanticDifference("/z", SemanticDifferenceKind.Missing, "{\"child\":[1]}", null),
            },
            comparison.Differences);
    }

    static SemanticModelComparison Compare(NormalizedSemanticNode expected, NormalizedSemanticNode actual) =>
        SemanticModelEquivalence.Compare(new NormalizedSemanticModel(expected, []), new NormalizedSemanticModel(actual, []));
}
