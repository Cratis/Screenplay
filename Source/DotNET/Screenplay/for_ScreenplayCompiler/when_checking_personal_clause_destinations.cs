// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_personal_clause_destinations : given.a_compiler
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reject_personal_clause_values_for_undeclared_and_registered_triggers(bool registered)
    {
        var compiler = registered ? new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: [new TriggerDefinition("External", ["patient"])])) : _compiler;
        var result = compiler.Compile("""
            concept PatientId : Uuid @pii
            module M
              feature F
                slice Automation S
                  event Recorded
                  reaction R
                    when External
                      patient PatientId
                      produces Recorded
                        for patient
            """);
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier);
        diagnostic.Code.ShouldEqual(DiagnosticCodes.PiiNotSupportedOnIdentifier);
        diagnostic.Location.Line.ShouldEqual(10);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact]
    void should_keep_typed_values_local_to_each_trigger_clause()
    {
        var result = _compiler.Compile("""
            concept PatientId : Uuid @pii
            module M
              feature F
                slice Automation S
                  event Recorded
                  reaction R
                    when External
                      patient PatientId
                      produces Recorded
                        for patient
                    when Other
                      patient Uuid
                      produces Recorded
                        for patient
                  reaction Another
                    when External
                      patient
                      produces Recorded
                        for patient
            """);
        result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier)
            .Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldContainOnly("PLAY0515@10");
    }
}
