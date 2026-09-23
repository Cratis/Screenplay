// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_every_validation_kind : a_valid_semantic_model
{
    static readonly SemanticValidationRuleKind[] _numberKinds =
    [
        SemanticValidationRuleKind.Maximum,
        SemanticValidationRuleKind.Minimum,
        SemanticValidationRuleKind.Equal,
        SemanticValidationRuleKind.NotEqual,
        SemanticValidationRuleKind.GreaterThan,
        SemanticValidationRuleKind.GreaterThanOrEqual,
        SemanticValidationRuleKind.LessThan,
        SemanticValidationRuleKind.LessThanOrEqual
    ];

    ExecutableSemanticModel _source;
    ExecutableSemanticModel _roundTripped;
    byte[] _json;
    byte[] _reserialized;
    string _text;

    void Because()
    {
        var quantity = new SemanticConcept(
            Id(SemanticKind.Concept, "Quantity"),
            "Quantity",
            SemanticPrimitiveType.WholeNumber,
            [],
            [.. _numberKinds.Select(kind => new SemanticValidationRule(default, kind, SemanticValue.Number(1), $"{kind} one"))]);
        _source = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            _application with { Concepts = [.. _application.Concepts, quantity] });
        _json = SemanticModelSerializer.Serialize(_source);
        _text = Encoding.UTF8.GetString(_json);
        _roundTripped = SemanticModelSerializer.Deserialize(_json);
        _reserialized = SemanticModelSerializer.Serialize(_roundTripped);
    }

    [Fact] void should_write_greater_than() => _text.Contains("\"kind\":\"greaterThan\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_greater_than_or_equal() => _text.Contains("\"kind\":\"greaterThanOrEqual\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_less_than() => _text.Contains("\"kind\":\"lessThan\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_less_than_or_equal() => _text.Contains("\"kind\":\"lessThanOrEqual\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_read_every_kind_back_in_order() => Rules(_roundTripped).Select(_ => _.Kind).SequenceEqual(_numberKinds).ShouldBeTrue();
    [Fact] void should_read_every_operand_back() => Rules(_roundTripped).All(_ => _.Operand == SemanticValue.Number(1)).ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => _roundTripped.Revision.ShouldEqual(_source.Revision);
    [Fact] void should_be_byte_identical() => _reserialized.SequenceEqual(_json).ShouldBeTrue();

    static IEnumerable<SemanticValidationRule> Rules(ExecutableSemanticModel model) =>
        model.Application.Concepts.Single(_ => _.Name == "Quantity").Validations;
}
