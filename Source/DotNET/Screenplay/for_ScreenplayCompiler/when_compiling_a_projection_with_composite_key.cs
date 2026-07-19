// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_projection_with_composite_key : given.a_compiler
{
    const string Source =
        """
        projection Order => OrderReadModel
          from OrderCreated
            key OrderKey
              customerId = customerId
              orderNumber = orderNumber
            total = total
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_parse_the_composite_key_type() => Key.Type.ShouldEqual("OrderKey");
    [Fact] void should_parse_both_parts() => Key.Parts.Select(_ => _.Property).ShouldContainOnly("customerId", "orderNumber");
    [Fact] void should_keep_the_sibling_mapping() => From.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("total");

    FromSyntax From => _result.Value!.Blocks.OfType<FromSyntax>().Single();

    CompositeKeySyntax Key => (CompositeKeySyntax)From.Key!;
}
