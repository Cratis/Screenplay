// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_it_returns_an_optional_read_model : given.a_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("LedgerPage");

    [Fact] void should_return_zero_or_one() => _shape.Cardinality.ShouldEqual(SemanticQueryCardinality.ZeroOrOne);
    [Fact] void should_describe_every_key_part_in_order() => _shape.Arguments.Select(_ => _.Name).ShouldEqual(["book", "page"]);
    [Fact] void should_describe_key_parts_as_keys() => _shape.Arguments.Select(_ => _.Kind).ShouldEqual([SemanticPerformedQueryArgumentKind.Key, SemanticPerformedQueryArgumentKind.Key]);
    [Fact] void should_describe_the_key_part_types() => _shape.Arguments.Select(_ => _.Type!.Primitive).ShouldEqual([SemanticPrimitiveType.Text, SemanticPrimitiveType.WholeNumber]);
    [Fact] void should_require_every_key_part() => _shape.Arguments.ShouldEachConformTo(_ => !_.IsOptional);
}
