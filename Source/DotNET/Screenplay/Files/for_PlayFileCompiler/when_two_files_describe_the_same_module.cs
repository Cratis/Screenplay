// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_two_files_describe_the_same_module : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("module Sales\n  description \"Sales\"", "first.play"),
            compiler.Parse("module Sales\n  description \"Other sales\"", "second.play")]);
    }

    [Fact] void should_accept_the_folder() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_different_description() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ConflictingDescriptionAcrossFiles).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_first_file() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ConflictingDescriptionAcrossFiles).Message.ShouldContain("first.play");
    [Fact] void should_locate_the_second_file() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ConflictingDescriptionAcrossFiles).Location.Path.ShouldEqual("second.play");
    [Fact] void should_keep_the_first_description() => _result.Value!.Modules.Single().Description.ShouldEqual("Sales");
}
