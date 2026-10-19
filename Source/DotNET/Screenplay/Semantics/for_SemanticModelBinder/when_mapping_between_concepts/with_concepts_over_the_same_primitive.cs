// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_mapping_between_concepts;

// Decision 0098 (#861): ids that cross a context boundary keep their own concept on each side.
public class with_concepts_over_the_same_primitive : given.a_semantic_binder
{
    static string Model(string sourceConcept, string targetConcept, string sourceType = "OrderId", string targetType = "MovementId") => $$"""
        {{sourceConcept}}
        {{targetConcept}}
        module M
          feature F
            slice StateChange S
              event Moved
                movementId {{targetType}}
              command Move
                id String identifier
                orderId {{sourceType}}
                produces Moved
                  for id
                  movementId = orderId
        """;

    const string Order = "concept OrderId : Uuid";
    const string Movement = "concept MovementId : Uuid";

    [Fact] void should_map_one_uuid_concept_to_another() => AssertBinds(Bind(Model(Order, Movement)));
    [Fact] void should_select_esm_v29() => Bind(Model(Order, Movement)).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V29);
    [Fact] void should_map_a_concept_to_its_primitive() => AssertBinds(Bind(Model(Order, Movement, targetType: "Uuid")));
    [Fact] void should_map_a_primitive_to_a_concept_without_rules() => AssertBinds(Bind(Model(Order, Movement, sourceType: "Uuid")));
    [Fact] void should_map_one_string_concept_to_another() => AssertBinds(Bind(Model("concept OrderId : String", "concept MovementId : String")));
    [Fact] void should_map_when_the_source_proves_the_rules_of_the_target() => AssertBinds(Bind(Model("concept OrderId : String\n  validate\n    length == 3\n    not empty", "concept MovementId : String\n  validate\n    length == 3")));
    [Fact] void should_refuse_a_target_rule_the_source_does_not_prove() => Bind(Model("concept OrderId : String", "concept MovementId : String\n  validate\n    length == 3")).Success.ShouldBeFalse();
    [Fact] void should_refuse_a_different_primitive() => Bind(Model("concept OrderId : Uuid", "concept MovementId : String")).Success.ShouldBeFalse();
    [Fact] void should_refuse_an_enumeration() => Bind(Model("concept OrderId : Enum\n  a\n  b", "concept MovementId : Enum\n  a\n  b")).Success.ShouldBeFalse();
    [Fact] void should_refuse_widening_a_personal_value_into_a_plain_concept() => Bind(Model("concept OrderId : String pii", "concept MovementId : String")).Success.ShouldBeFalse();

    static void AssertBinds(CompilationResult<SemanticCompilation> result) => Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(_ => _.Message)));
}
