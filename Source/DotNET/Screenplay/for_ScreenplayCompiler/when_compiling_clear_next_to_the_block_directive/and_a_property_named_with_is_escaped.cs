// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_clear_next_to_the_block_directive;

/// <summary>
/// The escape is what makes guarding the bare form free - a read model really holding a property called
/// <c>with</c> can still have it cleared.
/// </summary>
public class and_a_property_named_with_is_escaped : given.a_compiler
{
    const string Source =
        """
        projection Shipping => ShippingReadModel
          from ShippingSet
            clear @with
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_it_as_a_clear_mapping() => From.Mappings.Single().ShouldBeOfExactType<ClearMappingSyntax>();
    [Fact] void should_clear_the_property_the_escape_names() => From.Mappings.OfType<ClearMappingSyntax>().Single().Property.ShouldEqual("with");

    FromSyntax From => _result.Value!.Blocks.OfType<FromSyntax>().Single();
}
