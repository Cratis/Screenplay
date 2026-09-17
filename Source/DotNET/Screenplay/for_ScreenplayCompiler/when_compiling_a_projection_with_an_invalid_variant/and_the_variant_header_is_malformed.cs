// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_projection_with_an_invalid_variant;

public class and_the_variant_header_is_malformed : given.a_compiler
{
    const string Source =
        """
        projection WorkItem
          variant
            enters on IssueCreated
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_report_the_invalid_variant_declaration() =>
        _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidProjectionVariantDeclaration);
}
