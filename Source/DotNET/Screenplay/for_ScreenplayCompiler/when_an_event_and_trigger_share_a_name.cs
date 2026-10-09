// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_an_event_and_trigger_share_a_name : given.a_compiler
{
    [Theory]
    [InlineData("eventValue", 0)]
    [InlineData("triggerValue", 1)]
    void should_validate_values_against_the_event(string value, int warnings)
    {
        var result = _compiler.Compile($$"""
            trigger Changed
              triggerValue String
            module M
              feature F
                slice Automation S
                  event Changed
                    eventValue Uuid
                  reaction R
                    when Changed
                      {{value}}
            """);

        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownTriggerData).ShouldEqual(warnings);
    }

    [Theory]
    [InlineData("eventValue", 0)]
    [InlineData("triggerValue", 1)]
    void should_prefer_an_event_over_a_registered_trigger(string value, int warnings)
    {
        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: [new TriggerDefinition("Changed", ["triggerValue"])]));
        var result = compiler.Compile($$"""
            module M
              feature F
                slice Automation S
                  event Changed
                    eventValue Uuid
                  reaction R
                    when Changed
                      {{value}}
            """);

        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownTriggerData).ShouldEqual(warnings);
    }

    [Theory]
    [InlineData("PatientId", "Uuid", "Uuid", 0)]
    [InlineData("PatientId", "Uuid", "PatientId", 1)]
    [InlineData("Uuid", "Uuid", "PatientId", 1)]
    [InlineData("Uuid", "PatientId", "Uuid", 1)]
    [InlineData("PatientId", "PatientId", "PatientId", 1)]
    void should_check_clause_local_types_without_borrowing_the_shadowed_trigger_shape(string triggerType, string eventType, string clauseType, int errors)
    {
        var result = _compiler.Compile($$"""
            concept PatientId : Uuid pii
            trigger External
              patient {{triggerType}}
            module M
              feature F
                slice Automation S
                  event External
                    patient {{eventType}}
                  event Recorded
                  reaction R
                    when External
                      patient {{clauseType}}
                      produces Recorded
                        for patient
            """);

        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}")
            .ShouldContainOnly(Enumerable.Repeat("PLAY0515@14", errors).ToArray());
    }

    [Fact]
    void should_check_personal_trigger_values_when_the_event_shape_is_ambiguous()
    {
        var result = _compiler.Compile("""
            concept PatientId : Uuid pii
            trigger Changed
              patient PatientId
            module M
              feature F
                slice Automation S
                  event Recorded
                  reaction R
                    when Changed
                      produces Recorded
                        for patient
                slice StateChange First
                  event Changed
                    patient Uuid
                slice StateChange Second
                  event Changed
                    patient Uuid
            """);

        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier).ShouldEqual(1);
    }

    [Fact]
    void should_not_use_a_declared_trigger_shape_for_an_imported_event()
    {
        var result = _compiler.Compile("""
            import External.Changed
            concept PatientId : Uuid pii
            trigger Changed
              triggerValue String
              patient PatientId
            module M
              feature F
                slice Automation S
                  event Recorded
                  reaction R
                    when Changed
                      eventValue
                      patient
                      produces Recorded
                        for patient
            """);

        result.Diagnostics.ShouldBeEmpty();
    }
}
