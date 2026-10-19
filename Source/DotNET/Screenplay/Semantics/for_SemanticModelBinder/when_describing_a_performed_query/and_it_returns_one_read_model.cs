// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_it_returns_one_read_model : given.a_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("CurrentFigures");

    [Fact] void should_name_the_read_model() => _shape.ReadModelName.ShouldEqual("InvoiceFigures");
    [Fact] void should_identify_the_read_model() => _shape.ReadModel.ShouldEqual(_result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().ReadModels.Single(_ => _.Name == "InvoiceFigures").Id);
    [Fact] void should_address_the_read_model() => _shape.ReadModelAddress!.Kind.ShouldEqual(SemanticKind.ReadModel);
    [Fact] void should_address_the_read_model_by_name() => _shape.ReadModelAddress!.Name.ShouldEqual("InvoiceFigures");
    [Fact] void should_return_exactly_one() => _shape.Cardinality.ShouldEqual(SemanticQueryCardinality.One);
    [Fact] void should_be_a_snapshot() => _shape.Delivery.ShouldEqual(SemanticQueryDelivery.Snapshot);
    [Fact] void should_describe_the_by_key() => _shape.Arguments.Single().ShouldEqual(new SemanticPerformedQueryArgument("invoiceId", SemanticPerformedQueryArgumentKind.Key, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, false, false), false));
}
