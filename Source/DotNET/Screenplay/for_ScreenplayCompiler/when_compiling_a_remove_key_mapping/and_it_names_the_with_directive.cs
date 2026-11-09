// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_remove_key_mapping;

public class and_it_names_the_with_directive : given.a_projection_removing_a_key
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("remove with"));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_mapping() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidProjectionMapping);
    [Fact] void should_point_at_the_escape() => _result.Diagnostics.Single().Message.ShouldContain("remove @with");
}
