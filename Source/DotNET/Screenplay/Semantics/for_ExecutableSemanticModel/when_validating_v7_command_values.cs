// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_v7_command_values : a_v7_contract
{
    [Fact] void should_accept_generation_and_a_scalar_response() => Catch.Exception(() => Create(Application())).ShouldBeNull();
    [Fact]
    void should_refuse_v7_without_a_v7_construct()
    {
        var application = Application(Command with { Properties = [], Response = null }, Scenario with { When = new(CommandId, []), ThenReturns = null, ThenErrors = [new(null, null)] });
        Refuse(application);
    }
    [Fact]
    void should_refuse_v7_constructs_below_v7()
    {
        var models = new[]
        {
            canonical_serialization_golden_vectors.CreateSemanticModel(),
            canonical_serialization_golden_vectors.CreateSemanticModelV2(),
            canonical_serialization_golden_vectors.CreateSemanticModelV3(),
            canonical_serialization_golden_vectors.CreateSemanticModelV4(),
            canonical_serialization_golden_vectors.CreateSemanticModelV5(),
            canonical_serialization_golden_vectors.CreateSemanticModelV6()
        };
        models.Length.ShouldEqual(6);
        foreach (var model in models)
        {
            var changed = 0;
            SemanticFeature Change(SemanticFeature feature) => feature with
            {
                Features = [.. feature.Features.Select(Change)],
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Commands = [.. slice.Commands.Select(command =>
                    {
                        var property = command.Properties.FirstOrDefault(value => !value.Type.IsCollection);
                        if (property is null) return command;
                        changed++;
                        return command with { Response = new SemanticScalarCommandResponse(property.Id, property.Type) };
                    })]
                })]
            };
            var application = model.Application with { Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(Change)] })] };
            changed.ShouldBeGreaterThan(0);
            var error = Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("require ESM v7", StringComparison.Ordinal).ShouldBeTrue();
        }
    }
    [Fact]
    void should_require_a_uuid_concept_for_generation()
    {
        foreach (var type in new[] { SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), IdentityType with { IsOptional = true }, IdentityType with { IsCollection = true }, NoteType })
        {
            Refuse(Application(Command with { Properties = [Command.Properties[0] with { Type = type }, Command.Properties[1]] }));
        }
    }
    [Fact]
    void should_refuse_a_non_uuid_concept()
    {
        var application = Application();
        Refuse(application with { Concepts = [application.Concepts[0] with { Primitive = SemanticPrimitiveType.Text }] });
    }
    [Fact]
    void should_refuse_any_rules_on_the_generated_concept()
    {
        foreach (var rule in new[]
        {
            new SemanticValidationRule(default, SemanticValidationRuleKind.NotEmpty, null, null),
            new SemanticValidationRule(default, SemanticValidationRuleKind.RulePredicate, null, null) { Name = "Valid", RequirementId = "rule" },
            new SemanticValidationRule(default, SemanticValidationRuleKind.CodeValidation, null, null) { Name = "Valid", RequirementId = "code" }
        })
        {
            Refuse(Application(rules: [rule]));
        }
    }
    [Fact] void should_refuse_a_generated_request_input() => Refuse(Application(scenario: Scenario with { When = Scenario.When with { Values = [new(InputId, SemanticValue.Null), new(GeneratedId, GeneratedValue)] } }));
    [Fact] void should_allow_missing_generation_fixtures() => Catch.Exception(() => Create(Application(scenario: Scenario with { When = Scenario.When with { GeneratedValues = [] } }))).ShouldBeNull();
    [Fact]
    void should_refuse_foreign_input_duplicate_mistyped_and_null_generation_fixtures()
    {
        foreach (var values in new[]
        {
            new[] { new SemanticPropertyValue(Id(99), GeneratedValue) },
            [new(InputId, SemanticValue.Null)],
            [new(GeneratedId, GeneratedValue), new(GeneratedId, GeneratedValue)],
            [new(GeneratedId, SemanticValue.Number(2))],
            [new(GeneratedId, null!)],
            [new(GeneratedId, SemanticValue.Text("0B4F8E6C1D6A4A529A533F5B6A0C1D11"))]
        })
        {
            Refuse(Application(scenario: Scenario with { When = Scenario.When with { GeneratedValues = [.. values] } }));
        }
    }
    [Fact] void should_refuse_an_event_source_with_generated_identifier_fixtures() => Refuse(Application(scenario: Scenario with { When = Scenario.When with { EventSource = new(IdentityType, GeneratedValue) } }));
    [Fact] void should_refuse_generated_identifier_event_source_even_without_a_fixture() => Refuse(Application(scenario: Scenario with { When = Scenario.When with { GeneratedValues = [], EventSource = new(IdentityType, GeneratedValue) } }));
    [Fact]
    void should_refuse_requirements_and_rules_over_generated_values()
    {
        Refuse(Application(Command with { Requirements = [new(new SemanticComparison(new(GeneratedId, null), SemanticComparisonOperator.Equal, new(default, GeneratedValue)), null)] }));
        Refuse(Application(Command with { Validations = [new(GeneratedId, SemanticValidationRuleKind.NotEmpty, null, null)] }));
        Refuse(Application(Command with { Validations = [new(GeneratedId, SemanticValidationRuleKind.RulePredicate, null, null) { Name = "Valid", RequirementId = "rule" }] }));
    }
    [Fact]
    void should_accept_requirements_and_rules_over_inputs()
    {
        var command = Command with
        {
            Properties = [Command.Properties[0], Command.Properties[1] with { Type = NoteType with { IsOptional = false } }],
            Requirements = [new(new SemanticComparison(new(InputId, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text("accepted"))), null)],
            Validations = [new(InputId, SemanticValidationRuleKind.NotEmpty, null, null)]
        };
        var scenario = Scenario with { When = Scenario.When with { Values = [new(InputId, SemanticValue.Text("accepted"))] } };
        Catch.Exception(() => Create(Application(command, scenario))).ShouldBeNull();
    }
}
