// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_consistency_depends_on_opaque_mapping_expressions : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("note = note", "note = Transform(note)", StringComparison.Ordinal));

    [Fact] void should_not_execute_or_guess_the_mapping_expression() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_consistency_error() => _result.Diagnostics.ShouldBeEmpty();
}
