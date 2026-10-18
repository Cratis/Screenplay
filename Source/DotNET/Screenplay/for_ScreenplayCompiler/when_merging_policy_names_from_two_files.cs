// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_merging_policy_names_from_two_files : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = PlayFolderMerge.Merge([
        _compiler.Parse("policy IsAdmin\n  require authenticated", "first.play"),
        _compiler.Parse("policy IsAdmin\n  require role \"Auditor\"", "second.play")
    ]);

    [Fact] void should_report_only_the_cross_file_duplicate() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.RepeatedDeclarationAcrossFiles);
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_locate_the_second_file() => _result.Diagnostics.Single().Location.Path.ShouldEqual("second.play");
    [Fact] void should_locate_the_second_declaration() => _result.Diagnostics.Single().Location.Line.ShouldEqual(1);
    [Fact] void should_keep_only_the_first_policy() => _result.Value!.Policies.Count().ShouldEqual(1);
}
