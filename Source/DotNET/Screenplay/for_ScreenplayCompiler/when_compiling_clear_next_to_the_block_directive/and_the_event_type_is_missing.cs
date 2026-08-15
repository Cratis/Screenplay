// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_clear_next_to_the_block_directive;

/// <summary>
/// A bare <c>clear with</c> is almost always someone who started writing the block directive and stopped. Reading it
/// as a clear of a property named <c>with</c> would do something other than what was meant, without saying so.
/// </summary>
public class and_the_event_type_is_missing : given.a_compiler
{
    const string Source =
        """
        projection Shipping => ShippingReadModel
          from ShippingSet
            clear with
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_it_as_an_invalid_mapping() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidProjectionMapping);
    [Fact] void should_say_what_was_probably_meant() => _result.Diagnostics.Single().Message.ShouldEqual("Invalid mapping 'clear with' - 'clear with' is a block directive and needs an event type; to clear a property named 'with', write 'clear @with'");
    [Fact] void should_not_parse_it_as_a_clear_mapping() => From.Mappings.OfType<ClearMappingSyntax>().ShouldBeEmpty();
    [Fact] void should_not_parse_it_as_a_mapping_at_all() => From.Mappings.ShouldBeEmpty();

    FromSyntax From => _result.Value!.Blocks.OfType<FromSyntax>().Single();
}
