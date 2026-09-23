// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_dynamic_dictionary_key;

public class and_it_names_an_unknown_sub_path : given.a_projection_counting_by_key
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("countByType.$eventContext.eventType.name"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_key() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventContextPath);
}
