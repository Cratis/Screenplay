// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_merging_duplicate_forms : Specification
{
    const string Source =
        """
        module Sales
          form RegisterForm for Register
            field name
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([compiler.Parse(Source, "first.play"), compiler.Parse(Source, "second.play")]);
    }

    [Fact] void should_reject_the_folder() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate_form() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDeclarationAcrossFiles).Message.ShouldEqual("Duplicate form 'RegisterForm' in module 'Sales' - already declared in 'first.play'");
    [Fact] void should_locate_the_second_declaration() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDeclarationAcrossFiles).Location.Path.ShouldEqual("second.play");
    [Fact] void should_keep_only_one_form() => _result.Value!.Modules.Single().Forms!.Count().ShouldEqual(1);
}
