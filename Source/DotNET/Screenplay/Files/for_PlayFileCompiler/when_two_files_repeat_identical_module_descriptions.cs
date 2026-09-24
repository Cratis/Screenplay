// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_two_files_repeat_identical_module_descriptions : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("module Sales\n  description \"Sales\"", "first.play"),
            compiler.Parse("module Sales\n  description \"Sales\"", "second.play")]);
    }

    [Fact] void should_accept_the_folder_without_a_diagnostic() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_difference() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_description() => _result.Value!.Modules.Single().Description.ShouldEqual("Sales");
}
