// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_a_scope_through_the_public_api : given.a_model
{
    bool _resolved;
    ScopedDiagnosticResult? _result;
    ScopeSelectionError? _error;

    void Establish() => Sources["application.play"] += "\nmodule Broken\n  feature F\n    slice Wat Bad";

    void Because() => _resolved = ScopedDiagnostics.TryValidate(Sources, "Orders.Registration.Add", CompletenessChecks.None, out _result, out _error);

    [Fact] void should_resolve_even_with_model_errors() => _resolved.ShouldBeTrue();
    [Fact] void should_not_report_a_scope_error() => _error.ShouldBeNull();
    [Fact] void should_name_the_scope() => _result!.Scope.ShouldEqual("Orders.Registration.Add");
    [Fact] void should_count_the_scope_declarations() => _result!.DeclarationCount.ShouldEqual(2);
    [Fact] void should_count_only_direct_dependents() => _result!.DependentDeclarationCount.ShouldEqual(1);
    [Fact] void should_include_direct_dependent_diagnostics() => _result!.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownDependent", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_exclude_transitive_diagnostics() => _result!.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownTransitive", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_name_the_affected_scope() => _result!.AffectedScopes.ShouldContainOnly("Reporting.Views.Consume");
    [Fact] void should_report_whole_application_errors() => _result!.WholeApplicationErrorCount.ShouldEqual(new ScreenplayCompiler().Compile(Sources["application.play"]).Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    [Fact] void should_report_whole_application_warnings() => _result!.WholeApplicationWarningCount.ShouldEqual(new ScreenplayCompiler().Compile(Sources["application.play"]).Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning));
}
