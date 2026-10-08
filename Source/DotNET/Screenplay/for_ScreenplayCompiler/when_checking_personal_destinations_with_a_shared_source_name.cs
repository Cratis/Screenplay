// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_personal_destinations_with_a_shared_source_name : given.a_compiler
{
    [Theory]
    [InlineData("PatientId", "Uuid", 0)]
    [InlineData("Uuid", "PatientId", 1)]
    [InlineData("PatientId", "PatientId", 1)]
    [InlineData("Uuid", "Uuid", 0)]
    void should_check_the_event_shape_instead_of_the_shadowed_trigger(string triggerType, string eventType, int count)
    {
        var result = _compiler.Compile($$"""
            concept PatientId : Uuid @pii
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
                      patient
                      produces Recorded
                        for patient
            """);
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier).ToArray();
        diagnostics.Length.ShouldEqual(count);
        foreach (var diagnostic in diagnostics)
        {
            diagnostic.Location.Line.ShouldEqual(14);
            diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        }
    }
}
