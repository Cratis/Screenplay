// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_walking_composites_and_collections : Specification
{
    SemanticApplication _application;
    SemanticConcept _code;
    SemanticConcept _quantity;
    SemanticTypeReference _type;
    SemanticValue _value;
    ImmutableArray<SemanticRuleCheck> _results;

    void Establish()
    {
        _code = new(SemanticId.Create(SemanticKind.Concept, "Code"), "Code", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.NotEmpty, null, null)]);
        _quantity = new(SemanticId.Create(SemanticKind.Concept, "Quantity"), "Quantity", SemanticPrimitiveType.DecimalNumber, [], [new(default, SemanticValidationRuleKind.Minimum, SemanticValue.Number(1), null)]);
        var code = new SemanticProperty(SemanticId.Create(SemanticKind.Property, "codes"), "codes", SemanticTypeReference.ForConcept(_code.Id, isCollection: true), false);
        var quantity = new SemanticProperty(SemanticId.Create(SemanticKind.Property, "quantity"), "quantity", SemanticTypeReference.ForConcept(_quantity.Id), false);
        var line = new SemanticCompositeType(SemanticId.Create(SemanticKind.CompositeType, "Line"), "Line", [code, quantity]);
        var lines = new SemanticProperty(SemanticId.Create(SemanticKind.Property, "lines"), "lines", SemanticTypeReference.ForCompositeType(line.Id, isCollection: true), false);
        var order = new SemanticCompositeType(SemanticId.Create(SemanticKind.CompositeType, "Order"), "Order", [lines]);
        _application = new(SemanticId.Create(SemanticKind.Application, "Orders"), "Orders", [_code, _quantity], [line, order], []);
        _type = SemanticTypeReference.ForCompositeType(order.Id);
        _value = SemanticValue.Composite([new(lines.Id, SemanticValue.Array([
            SemanticValue.Composite([new(code.Id, SemanticValue.Array([SemanticValue.Text("A"), SemanticValue.Text("")])), new(quantity.Id, SemanticValue.Number(0))]),
            SemanticValue.Composite([new(code.Id, SemanticValue.Array([SemanticValue.Text("B")])), new(quantity.Id, SemanticValue.Number(1))])]))]);
    }

    void Because() => _results = SemanticValidationRules.Check(_application, _type, _value);

    [Fact] void should_walk_every_nested_value_in_order() => _results.Select(check => check.Concept).ShouldEqual([_code.Id, _code.Id, _quantity.Id, _code.Id, _quantity.Id]);
    [Fact] void should_keep_satisfied_and_failed_results() => _results.Select(check => check.Status).ShouldEqual([SemanticRuleStatus.Satisfied, SemanticRuleStatus.Failed, SemanticRuleStatus.Failed, SemanticRuleStatus.Satisfied, SemanticRuleStatus.Satisfied]);
    [Fact] void should_keep_concept_messages() => _results[1].Message.ShouldEqual("A required concept value is empty.");
}
