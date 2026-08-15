// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_clear_next_to_the_block_directive;

/// <summary>
/// A whole <c>clear with &lt;EventType&gt;</c> inside a <c>from</c> block was rejected before the <c>clear</c> mapping
/// existed, and has to stay rejected - widening the keyword must not turn a misplaced block directive into a mapping.
/// </summary>
public class and_the_block_directive_sits_in_a_mapping_position : given.a_compiler
{
    const string Source =
        """
        projection Shipping => ShippingReadModel
          from ShippingSet
            clear with ShippingCleared
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_it_as_an_invalid_mapping() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidProjectionMapping);
    [Fact] void should_report_the_line_it_could_not_read() => _result.Diagnostics.Single().Message.ShouldEqual("Invalid mapping 'clear with ShippingCleared'");
    [Fact] void should_not_parse_it_as_a_clear_mapping() => From.Mappings.OfType<ClearMappingSyntax>().ShouldBeEmpty();

    FromSyntax From => _result.Value!.Blocks.OfType<FromSyntax>().Single();
}
