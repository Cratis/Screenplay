// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_abilities : given.a_compiler
{
    const string Source = """
        concept ParcelId : Uuid
        concept TrackingNumber : String

        system Carrier
          description "The carrier"
          ability BookPickup idempotent
            description "Books a pickup"
            parcelId ParcelId
            notes String optional
            returns
              trackingNumber TrackingNumber
              pickupAt DateTime
            outcome NoCapacity
              description "No slot"
            outcome AddressRefused
          ability Quote safe
          ability Wait
            answers later
            @outcome String
            implementation
              hint "Use the adapter"
              file Adapters/Wait.cs
          ability Cancel
            ```csharp
            return Done();
            ```
        """;

    AbilitySyntax[] _abilities;
    string _printed;

    IEnumerable<string> Codes(string body) => _compiler.Compile("system S\n" + body).Diagnostics.Select(diagnostic => diagnostic.Code);

    void Because()
    {
        var parsed = _compiler.Compile(Source);
        _abilities = [.. parsed.Value!.Systems.Single().Abilities];
        _printed = new ScreenplayPrinter().Print(parsed.Value!);
    }

    [Fact] void should_keep_the_abilities_in_declaration_order() => _abilities.Select(ability => ability.Name).ShouldEqual(["BookPickup", "Quote", "Wait", "Cancel"]);
    [Fact] void should_keep_the_written_effect_class() => _abilities.Select(ability => ability.EffectClass).ShouldEqual(["idempotent", "safe", null, null]);
    [Fact] void should_read_the_inputs() => _abilities[0].Inputs.Select(input => input.Name).ShouldEqual(["parcelId", "notes"]);
    [Fact] void should_read_the_returned_values() => _abilities[0].Returns.Select(value => value.Name).ShouldEqual(["trackingNumber", "pickupAt"]);
    [Fact] void should_read_the_outcomes() => _abilities[0].Outcomes.Select(outcome => outcome.Name).ShouldEqual(["NoCapacity", "AddressRefused"]);
    [Fact] void should_read_the_outcome_description() => _abilities[0].Outcomes.First().Description.ShouldEqual("No slot");
    [Fact] void should_read_the_answer_that_arrives_later() => _abilities[2].AnswersLater.ShouldBeTrue();
    [Fact] void should_escape_an_input_named_like_a_keyword() => _abilities[2].Inputs.Single().Name.ShouldEqual("outcome");
    [Fact] void should_read_the_implementation_wrapper() => (_abilities[2].Implementation!.Hints.Count(), _abilities[2].File!.Path).ShouldEqual((1, "Adapters/Wait.cs"));
    [Fact] void should_read_an_inline_implementation() => _abilities[3].Code!.Language.ShouldEqual("csharp");
    [Fact] void should_print_what_it_read() => _compiler.Compile(_printed).Value.ShouldNotBeNull();
    [Fact] void should_print_the_source_it_read() => _printed.ShouldContain("ability BookPickup idempotent");
    [Fact] void should_print_an_escaped_input() => _printed.ShouldContain("@outcome String");
    [Fact] void should_round_trip_structurally() => SyntaxJson.StructurallyEqual(_compiler.Compile(Source).Value!, _compiler.Compile(_printed).Value!).ShouldBeTrue();
    [Fact] void should_round_trip_through_the_wire_form() => SyntaxJson.StructurallyEqual(_compiler.Compile(Source).Value!, SyntaxJson.Deserialize(SyntaxJson.Serialize(_compiler.Compile(Source).Value!))).ShouldBeTrue();

    [Fact] void should_default_the_effect_class_to_once() => AbilityEffectClasses.Effective(_abilities[2].EffectClass).ShouldEqual(AbilityEffectClasses.Once);
    [Fact] void should_refuse_a_duplicate_ability_ignoring_case() => Codes("  ability Send\n  ability send\n").ShouldEqual([DiagnosticCodes.DuplicateAbility]);
    [Fact] void should_refuse_an_unknown_effect_class() => Codes("  ability Send often\n").ShouldEqual([DiagnosticCodes.InvalidAbilityEffectClass]);
    [Theory]
    [InlineData("next")]
    [InlineData("Refused")]
    [InlineData("FAILED")]
    [InlineData("returned")]
    [InlineData("expired")]
    void should_refuse_a_reserved_outcome_name(string name) => Codes($"  ability Send\n    outcome {name}\n").ShouldEqual([DiagnosticCodes.ReservedAbilityOutcomeName]);
    [Fact] void should_refuse_an_unknown_type() => Codes("  ability Send\n    parcel ParcelId\n    returns\n      tracking TrackingNumber\n").ShouldEqual([DiagnosticCodes.UnknownAbilityType, DiagnosticCodes.UnknownAbilityType]);
    [Fact] void should_refuse_an_outcome_payload() => Codes("  ability Send\n    outcome Busy\n      reason String\n").ShouldEqual([DiagnosticCodes.InvalidSystemDeclaration]);
    [Fact] void should_refuse_an_empty_returns_block() => Codes("  ability Send\n    returns\n").ShouldEqual([DiagnosticCodes.InvalidSystemDeclaration]);
    [Fact] void should_refuse_an_identifier_input() => Codes("  ability Send\n    parcel Uuid identifier\n").ShouldEqual([DiagnosticCodes.InvalidSystemDeclaration]);
    [Fact] void should_refuse_mixed_implementation_sources() => Codes("  ability Send\n    file A.cs\n    file B.cs\n").ShouldEqual([DiagnosticCodes.ConflictingImplementationSources]);
    [Fact] void should_refuse_a_repeated_answers_later() => Codes("  ability Send\n    answers later\n    answers later\n").ShouldEqual([DiagnosticCodes.InvalidSystemDeclaration]);
}
