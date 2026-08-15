// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_clear_next_to_the_block_directive;

/// <summary>
/// The two forms share a nested block here on purpose - <c>clear with</c> drops the whole nested object,
/// <c>clear carrier</c> removes one property, and neither reading may drift onto the other.
/// </summary>
public class and_it_is_the_block_directive : given.a_compiler
{
    const string Source =
        """
        projection Shipping => ShippingReadModel
          nested shipping
            from ShippingSet
              carrier = carrier
              clear trackingNumber
            clear with ShippingCleared
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_block_directive_against_its_event() => Nested.Blocks.OfType<ClearWithSyntax>().Single().Event.ShouldEqual("ShippingCleared");
    [Fact] void should_parse_the_mapping_against_its_property() => From.Mappings.OfType<ClearMappingSyntax>().Single().Property.ShouldEqual("trackingNumber");
    [Fact] void should_keep_the_sibling_assignment() => From.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("carrier");

    NestedSyntax Nested => _result.Value!.Blocks.OfType<NestedSyntax>().Single();

    FromSyntax From => Nested.Blocks.OfType<FromSyntax>().Single();
}
