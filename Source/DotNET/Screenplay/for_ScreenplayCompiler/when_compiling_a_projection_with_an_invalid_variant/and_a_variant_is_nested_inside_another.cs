// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_projection_with_an_invalid_variant;

public class and_a_variant_is_nested_inside_another : given.a_compiler
{
    const string Source =
        """
        projection WorkItem
          variant BacklogItem
            enters on IssueCreated

            variant PullRequestItem
              enters on PullRequestCreated
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_report_that_variants_do_not_nest() =>
        _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.NestedProjectionVariantNotAllowed);

    [Fact] void should_still_parse_the_outer_variant() =>
        _result.Value!.Blocks.OfType<ProjectionVariantSyntax>().Single().Name.ShouldEqual("BacklogItem");
}
