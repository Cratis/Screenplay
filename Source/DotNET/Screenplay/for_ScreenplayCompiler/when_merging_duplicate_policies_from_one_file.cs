// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_merging_duplicate_policies_from_one_file : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = PlayFolderMerge.Merge([
        _compiler.Parse("policy IsAdmin\n  require authenticated\npolicy IsAdmin\n  require role \"Auditor\"", "only.play")
    ]);

    [Fact] void should_report_only_the_same_file_duplicate() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateDeclaration);
    [Fact] void should_locate_the_file() => _result.Diagnostics.Single().Location.Path.ShouldEqual("only.play");
    [Fact] void should_locate_the_second_declaration() => _result.Diagnostics.Single().Location.Line.ShouldEqual(3);
}
