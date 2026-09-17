// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_projection_with_an_invalid_variant;

public class and_the_enters_on_line_is_malformed : given.a_compiler
{
    const string Source =
        """
        projection WorkItem
          variant BacklogItem
            enters
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_report_the_invalid_enters_on_declaration() =>
        _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEntersOnDeclaration);
}
