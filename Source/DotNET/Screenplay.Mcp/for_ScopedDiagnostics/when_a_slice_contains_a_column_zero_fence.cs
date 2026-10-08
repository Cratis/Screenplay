// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_a_slice_contains_a_column_zero_fence : Specification
{
    ScopedDiagnosticResult _result;
    readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = "module M\n  feature F\n    slice StateChange Add\n      command Add\n        description\n          ```text\nBody\n```\n      brokenSlice\n    brokenFeature\n  brokenModule"
    };

    void Because() => _result = ScopedDiagnostics.Select(_sources, "M.F.Add")!;

    [Fact] void should_keep_the_slice_range_after_the_fence() => _result.Diagnostics.Any(diagnostic => diagnostic.Location.Line == 9).ShouldBeTrue();
    [Fact] void should_keep_the_enclosing_feature_range() => _result.Diagnostics.Any(diagnostic => diagnostic.Location.Line == 10).ShouldBeFalse();
    [Fact] void should_keep_the_enclosing_module_range() => _result.Diagnostics.Any(diagnostic => diagnostic.Location.Line == 11).ShouldBeFalse();
}
