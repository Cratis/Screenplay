// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_fragments_declare_dependencies : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("module Payroll\n  depends on Timesheets\n  feature Handover\n    depends on Runs\n    depends on Runs\n  feature Runs\n", "a.play"),
            compiler.Parse("module Payroll\n  depends on Timesheets\n  depends on Engagements\n  feature Handover\n    depends on Payroll.Runs\n    depends on Timesheets.Approval\nmodule Timesheets\n  feature Approval\nmodule Engagements\n", "b.play")
        ]);
    }

    [Fact] void should_accumulate_module_targets_in_path_order() => _result.Value!.Modules.First().DependsOn.Select(dependency => dependency.Target).ShouldEqual(["Timesheets", "Engagements"]);
    [Fact] void should_deduplicate_resolved_aliases() => _result.Value!.Modules.First().Features.First().DependsOn.Select(dependency => dependency.Target).ShouldEqual(["Runs", "Timesheets.Approval"]);
    [Fact] void should_warn_on_each_repeat() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDependencyDeclaration).Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Path}:{diagnostic.Location.Line}").ShouldEqual(["PLAY0555@a.play:5", "PLAY0555@b.play:2", "PLAY0555@b.play:5"]);
    [Fact] void should_report_repeats_as_warnings() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDependencyDeclaration).All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
