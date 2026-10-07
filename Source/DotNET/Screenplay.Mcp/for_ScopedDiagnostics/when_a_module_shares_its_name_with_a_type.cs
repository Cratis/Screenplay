// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_a_module_shares_its_name_with_a_type : given.a_module_with_a_real_dependent
{
    ScopedDiagnosticResult _result;

    void Establish() => Sources["outside.play"] = "type Orders\n  broken\nmodule Outside\n  feature F\n    slice StateChange Bad\n      command UseType\n        value Orders\n        broken";
    void Because() => _result = ScopedDiagnostics.Select(Sources, "Orders")!;

    [Fact] void should_count_only_the_module_hierarchy() => _result.DeclarationCount.ShouldEqual(4);
    [Fact] void should_count_only_the_real_dependent() => _result.DependentDeclarationCount.ShouldEqual(1);
    [Fact] void should_report_only_the_real_affected_scope() => _result.AffectedScopes.ShouldContainOnly("Reporting.F.Good");
    [Fact] void should_keep_the_scoped_verdict_clean() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_exercise_an_unrelated_error() => new McpSnapshot(Sources).Compilation.Success.ShouldBeFalse();
}
