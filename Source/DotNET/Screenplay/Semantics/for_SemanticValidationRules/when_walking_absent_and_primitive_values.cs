// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_walking_absent_and_primitive_values : Specification
{
    ImmutableArray<SemanticRuleCheck>[] _results;

    void Because()
    {
        var concept = new SemanticConcept(SemanticId.Create(SemanticKind.Concept, "Code"), "Code", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.NotEmpty, null, null)]);
        var composite = new SemanticCompositeType(SemanticId.Create(SemanticKind.CompositeType, "Optional"), "Optional", []);
        var application = new SemanticApplication(SemanticId.Create(SemanticKind.Application, "App"), "App", [concept], [composite], []);
        _results =
        [
            SemanticValidationRules.Check(application, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("")),
            SemanticValidationRules.Check(application, SemanticTypeReference.ForConcept(concept.Id, isCollection: true), SemanticValue.Array([])),
            SemanticValidationRules.Check(application, SemanticTypeReference.ForConcept(concept.Id, isCollection: true, isOptional: true), SemanticValue.Null),
            SemanticValidationRules.Check(application, SemanticTypeReference.ForCompositeType(composite.Id, isOptional: true), SemanticValue.Null),
            SemanticValidationRules.Check(application, SemanticTypeReference.ForConcept(concept.Id, isOptional: true), SemanticValue.Null)
        ];
    }

    [Fact] void should_have_no_rules_for_primitives_or_absent_containers() => _results.Take(4).ShouldEachConformTo(results => results.IsEmpty);
    [Fact] void should_check_presence_for_an_absent_scalar_concept() => _results[4].Single().Status.ShouldEqual(SemanticRuleStatus.Failed);
}
