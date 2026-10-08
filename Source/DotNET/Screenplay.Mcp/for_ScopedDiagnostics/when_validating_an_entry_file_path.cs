// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_an_entry_file_path : given.a_disk_application
{
    string _entry;

    void Establish()
    {
        _entry = Path.Combine(Root, "application.play");
        File.WriteAllText(_entry, "concept Name : String\nmodule M\n  feature F\n    import \"parts/*.play\"", Encoding.Unicode);
        var parts = Path.Combine(Root, "parts");
        Directory.CreateDirectory(parts);
        File.WriteAllText(Path.Combine(parts, "view.play"), "slice StateView View\n  event Changed\n    value Name", Encoding.Unicode);
        File.WriteAllText(Path.Combine(Root, "unrelated.play"), "module Other\n  feature F\n    slice Wat Broken");
    }

    void Because() => Resolved = ScopedDiagnostics.TryValidate(_entry, "M.F.View", new([CompletenessCheck.EventConsumers]), out Result, out Error);

    [Fact] void should_resolve_the_imported_scope() => Resolved.ShouldBeTrue();
    [Fact] void should_not_report_a_path_error() => Error.ShouldBeNull();
    [Fact] void should_count_the_imported_slice_and_event() => Result!.DeclarationCount.ShouldEqual(2);
    [Fact] void should_preserve_native_file_encoding_handling() => Result!.WholeApplicationErrorCount.ShouldEqual(0);
    [Fact] void should_ignore_unimported_files_next_to_the_entry_file() => Result!.WholeApplicationErrorCount.ShouldEqual(0);
    [Fact] void should_run_selected_completeness_checks() => Result!.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0536");
    [Fact] void should_count_selected_findings_in_the_whole_application() => Result!.WholeApplicationWarningCount.ShouldEqual(1);
}
