// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_finding_comparison_participants : given.a_date_command
{
    IReadOnlyDictionary<SemanticId, SemanticPrimitiveType> _result;
    SemanticProperty _date;
    SemanticProperty _concept;
    SemanticProperty _read;
    SemanticProperty _number;
    SemanticProperty _presence;

    void Establish()
    {
        var concept = new SemanticConcept(SemanticId.Create(SemanticKind.Concept, "birthday"), "Birthday", SemanticPrimitiveType.Date, [], []);
        Application = Application with { Concepts = [concept] };
        _date = new(SemanticId.Create(SemanticKind.Property, "date"), "date", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Date), false);
        _concept = new(SemanticId.Create(SemanticKind.Property, "concept"), "birthday", SemanticTypeReference.ForConcept(concept.Id), false);
        _read = new(SemanticId.Create(SemanticKind.Property, "read"), "readDate", _date.Type, false);
        _number = new(SemanticId.Create(SemanticKind.Property, "number"), "count", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false);
        _presence = new(SemanticId.Create(SemanticKind.Property, "presence"), "requiredDate", _date.Type, false);
        Command = Command with
        {
            Properties = [First, Second, Unused, _date, _concept, _read, _number, _presence],
            Validations = [new(_date.Id, SemanticValidationRuleKind.GreaterThan, SemanticValue.Text("2026-01-01"), null), new(_presence.Id, SemanticValidationRuleKind.NotEmpty, null, null)],
            Produces = [new(default, null, null, [])
            {
                When = new SemanticLogicalCondition(
                    new SemanticComparison(new(_concept.Id, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text("2026-01-01"))),
                    SemanticLogicalOperator.Or,
                    new SemanticComparison(new(_read.Id, null) { Read = "calendar" }, SemanticComparisonOperator.Equal, new(_number.Id, null)))
            }]
        };
    }

    void Because() => _result = SemanticDateComparisons.Participants(Application, Command);

    [Fact] void should_include_both_requirement_operands() => _result.Keys.ShouldContain(First.Id);
    [Fact] void should_include_the_other_requirement_operand() => _result[Second.Id].ShouldEqual(SemanticPrimitiveType.DateTime);
    [Fact] void should_include_a_date_validation_subject() => _result[_date.Id].ShouldEqual(SemanticPrimitiveType.Date);
    [Fact] void should_resolve_a_concept_in_a_conditional_production() => _result[_concept.Id].ShouldEqual(SemanticPrimitiveType.Date);
    [Fact] void should_exclude_read_operands() => _result.Keys.ShouldNotContain(_read.Id);
    [Fact] void should_exclude_non_date_operands() => _result.Keys.ShouldNotContain(_number.Id);
    [Fact] void should_exclude_presence_only_rules() => _result.Keys.ShouldNotContain(_presence.Id);
    [Fact] void should_exclude_unused_date_properties() => _result.Keys.ShouldNotContain(Unused.Id);
    [Fact] void should_find_exactly_four_participants() => _result.Count.ShouldEqual(4);
}
