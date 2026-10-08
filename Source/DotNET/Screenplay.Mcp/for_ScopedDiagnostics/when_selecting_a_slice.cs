// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_a_slice : given.a_model
{
    ScopedDiagnosticResult _result;

    void Because() => _result = ScopedDiagnostics.Select(Sources, "Orders.Registration.Add")!;

    [Fact] void should_count_the_slice_and_its_event() => _result.DeclarationCount.ShouldEqual(2);
    [Fact] void should_count_the_dependent_command_only() => _result.DependentDeclarationCount.ShouldEqual(1);
    [Fact] void should_include_target_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownTarget", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_direct_dependent_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownDependent", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_exclude_sibling_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownSibling", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_exclude_unrelated_errors_in_the_dependent_slice() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownUnrelated", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_exclude_transitive_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownTransitive", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_match_a_partial_module_name() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownPrefix", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_report_the_affected_scope() => _result.AffectedScopes.ShouldContainOnly("Reporting.Views.Consume");
    [Fact] void should_disclose_reference_coverage_limits() => _result.DependencyCoverage.ShouldContain("Not code");
}
