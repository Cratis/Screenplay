// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_selected_completeness_checks : Specification
{
    readonly Dictionary<string, string> _sources = new()
    {
        ["application.play"] = "module M\n  feature F\n    slice StateView View\n      event Changed\n        id Uuid\n    slice StateView Other\n      event OtherChanged\n        id Uuid"
    };
    ScopedDiagnosticResult _selected;
    ScopedDiagnosticResult _none;

    void Because()
    {
        ScopedDiagnostics.TryValidate(_sources, "M.F.View", new([CompletenessCheck.EventConsumers]), out var selected, out _).ShouldBeTrue();
        ScopedDiagnostics.TryValidate(_sources, "M.F.View", CompletenessChecks.None, out var none, out _).ShouldBeTrue();
        _selected = selected!;
        _none = none!;
    }

    [Fact] void should_include_only_the_selected_scope_finding() => _selected.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0536");
    [Fact] void should_count_findings_across_the_whole_application() => _selected.WholeApplicationWarningCount.ShouldEqual(2);
    [Fact] void should_not_run_unselected_checks() => _none.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_count_unselected_findings() => _none.WholeApplicationWarningCount.ShouldEqual(0);
}
