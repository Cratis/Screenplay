// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_merging_a_policy_repeated_within_and_across_files : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = PlayFolderMerge.Merge([
        _compiler.Parse("policy Access\n  require authenticated", "a.play"),
        _compiler.Parse("policy Access\n  require authenticated\npolicy Access\n  require authenticated", "b.play")
    ]);

    [Fact] void should_report_each_declaration_of_the_second_file_as_repeated_across_files() =>
        _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Path}:{diagnostic.Location.Line}").ShouldContainOnly("PLAY0173@b.play:1", "PLAY0173@b.play:3");
}
