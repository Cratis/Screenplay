// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_checks_with_source_errors : given.a_model
{
    ScopedDiagnosticResult _result;

    void Establish() => Sources["application.play"] += "\nmodule Broken\n  feature F\n    slice Wat Bad";

    void Because()
    {
        ScopedDiagnostics.TryValidate(Sources, "Orders.Registration.Add", CompletenessChecks.All, out var result, out _).ShouldBeTrue();
        _result = result!;
    }

    [Fact] void should_skip_completeness_findings() => _result.Diagnostics.Any(diagnostic => new[] { "PLAY0530", "PLAY0531", "PLAY0532", "PLAY0533", "PLAY0534", "PLAY0535", "PLAY0536", "PLAY0537" }.Contains(diagnostic.Code, StringComparer.Ordinal)).ShouldBeFalse();
    [Fact] void should_still_report_source_errors() => _result.WholeApplicationErrorCount.ShouldBeGreaterThan(0);
}
