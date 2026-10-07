// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_an_indexed_group_is_not_comparable : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "system Store\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      operation Save\n        uses Store\n      command Duplicate\n      command Duplicate\n";
        CompareSnapshots(source, source);
    }

    [Fact] void should_count_the_uncomparable_indexed_group_instead_of_discarding_it() => Section("members").GetProperty("unavailable").EnumerateArray().Any(reason => reason.GetString().Contains("indexed kind/address groups", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_comparison_as_incomplete() => Diff.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_report_no_semantic_change() => Diff.GetProperty("hasSemanticChange").ValueKind.ShouldEqual(JsonValueKind.Null);
}
