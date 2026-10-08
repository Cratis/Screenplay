// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_a_folder_path : given.a_disk_application
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n  feature F\n    slice StateView View\n      event Changed\n        value String");
        var nested = Path.Combine(Root, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "other.play"), "module Other\n  feature F\n    slice Wat Broken");
    }

    void Because() => Resolved = ScopedDiagnostics.TryValidate(Root, "M.F.View", new([CompletenessCheck.EventConsumers]), out Result, out Error);

    [Fact] void should_resolve_the_scope() => Resolved.ShouldBeTrue();
    [Fact] void should_not_report_a_path_error() => Error.ShouldBeNull();
    [Fact] void should_count_the_slice_and_event() => Result!.DeclarationCount.ShouldEqual(2);
    [Fact] void should_exclude_outside_diagnostics_from_the_scope() => Result!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_unimported_nested_files_too() => Result!.WholeApplicationErrorCount.ShouldEqual(1);
    [Fact] void should_skip_completeness_with_errors_in_another_file() => Result!.WholeApplicationWarningCount.ShouldEqual(0);
}
