// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_is_outside_a_children_block : for_ScreenplayCompiler.given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("""
        projection Models => ModelReadModel
          nested details
            insert first
            from DetailsChanged
              name = name
        """);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_ordering() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidChildrenOrdering);
}
