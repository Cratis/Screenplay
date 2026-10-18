// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_decimal_places;

public class with_a_subject_that_is_not_a_decimal : for_SemanticModelBinder.given.a_semantic_binder
{
    static string Model(string concept, string rule) => $$"""
        concept Thing : {{concept}}
          validate
            {{rule}}
        module M
          feature F
            slice StateChange S
              event E
                thing Thing
              command C
                id String identifier
                thing Thing
                produces E
                  for id
                  thing = thing
        """;

    [Fact] void should_refuse_a_whole_number() => Bind(Model("Int", "decimals <= 2")).Success.ShouldBeFalse();
    [Fact] void should_refuse_text() => Bind(Model("String", "decimals <= 2")).Success.ShouldBeFalse();
    [Fact] void should_refuse_a_negative_count() => Bind(Model("Decimal", "decimals <= -1")).Success.ShouldBeFalse();
    [Fact] void should_refuse_a_fractional_count() => Bind(Model("Decimal", "decimals <= 1.5")).Success.ShouldBeFalse();
    [Fact] void should_refuse_more_places_than_a_decimal_holds() => Bind(Model("Decimal", "decimals <= 29")).Success.ShouldBeFalse();
    [Fact] void should_bind_a_decimal() => Bind(Model("Decimal", "decimals <= 0")).Success.ShouldBeTrue();
}
