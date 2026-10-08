// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_a_module : given.a_model
{
    ScopedDiagnosticResult _result;

    void Because() => _result = ScopedDiagnostics.Select(Sources, "Orders")!;

    [Fact] void should_count_all_descendants_and_the_module() => _result.DeclarationCount.ShouldEqual(6);
    [Fact] void should_include_both_slices() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownSibling", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_direct_dependents() => _result.DependentDeclarationCount.ShouldEqual(1);
}
