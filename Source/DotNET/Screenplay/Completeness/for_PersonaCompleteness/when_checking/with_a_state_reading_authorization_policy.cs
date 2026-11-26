// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness.when_checking;

public class with_a_state_reading_authorization_policy : Specification
{
    ApplicationSyntax _application;
    Diagnostic[] _findings;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            policy Member
              require role "A"
            policy Enabled
              reads Plan as plan by assetId
              require plan.enabled == true
            persona Person
              policy Member
            module M
              feature F
                slice StateChange S
                  command C
                    assetId String identifier
                    authorize Enabled
                slice StateView Plans
                  readmodel Plan
                    assetId String key
                    enabled Bool
            """);
        parsed.Success.ShouldBeTrue();
        _application = parsed.Value!;
    }

    void Because() => _findings = [.. PersonaCompleteness.Check(_application)];

    [Fact] void should_leave_the_state_dependent_gate_unknown() => _findings.Select(finding => finding.Code).ShouldEqual([DiagnosticCodes.PersonaWithoutGate]);
}
