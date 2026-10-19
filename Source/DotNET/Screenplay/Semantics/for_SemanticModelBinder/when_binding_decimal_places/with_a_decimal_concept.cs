// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_decimal_places;

public class with_a_decimal_concept : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source = """
        concept Money : Decimal
          validate
            >= 0
            decimals <= 2 message "Money has at most two decimals"
        module M
          feature F
            slice StateChange Price
              event Priced
                amount Money
              command SetPrice
                id String identifier
                amount Money
                surcharge Decimal
                validate
                  surcharge decimals <= 3
                produces Priced
                  for id
                  amount = amount
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_select_esm_v29() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V29);
    [Fact] void should_bind_the_concept_rule() => _result.Value!.Model.Application.Concepts.Single().Validations.Single(_ => _.Kind == SemanticValidationRuleKind.DecimalPlaces).Operand.ShouldEqual(SemanticValue.Number(2));
    [Fact] void should_keep_the_message() => _result.Value!.Model.Application.Concepts.Single().Validations.Single(_ => _.Kind == SemanticValidationRuleKind.DecimalPlaces).Message.ShouldEqual("Money has at most two decimals");
    [Fact] void should_bind_the_property_rule() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single(_ => _.Kind == SemanticValidationRuleKind.DecimalPlaces).Operand.ShouldEqual(SemanticValue.Number(3));
    [Fact] void should_round_trip_through_the_canonical_json() => Serialization.SemanticModelSerializer.Deserialize(Serialization.SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
}
