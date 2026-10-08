// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_nested_features : Specification
{
    readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = """
            module M
              feature Outer
                slice StateChange OuterSlice
                  event OuterEvent
                    value UnknownOuter
                feature Inner
                  slice StateChange InnerSlice
                    event InnerEvent
                      value UnknownInner
            """
    };

    ScopedDiagnosticResult _outer;
    ScopedDiagnosticResult _inner;

    void Because()
    {
        _outer = ScopedDiagnostics.Select(_sources, "M.Outer")!;
        _inner = ScopedDiagnostics.Select(_sources, "M.Outer.Inner")!;
    }

    [Fact] void should_include_the_inner_feature_in_the_outer_scope() => _outer.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownInner", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_the_outer_slice_in_the_outer_scope() => _outer.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownOuter", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_the_inner_slice_in_the_inner_scope() => _inner.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownInner", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_exclude_the_outer_slice_from_the_inner_scope() => _inner.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownOuter", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_count_fewer_declarations_in_the_inner_scope() => _inner.DeclarationCount.ShouldBeLessThan(_outer.DeclarationCount);
}
