// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_projection_with_an_invalid_variant;

public class and_two_variants_share_a_name : given.a_compiler
{
    const string Source =
        """
        projection WorkItem
          variant BacklogItem
            enters on IssueCreated

          variant BacklogItem
            enters on IssueReopened
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_report_the_duplicate_variant_name() =>
        _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateProjectionVariantName);
}
