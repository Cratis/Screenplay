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
            compiler.Parse("module Sales\n  description \"Sales\"", "second.play")]);
    }

    [Fact] void should_reject_a_second_owner_even_with_identical_text() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_first_owner() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDeclarationAcrossFiles).Message.ShouldContain("first.play");
    [Fact] void should_locate_the_second_owner() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDeclarationAcrossFiles).Location.Path.ShouldEqual("second.play");
}
