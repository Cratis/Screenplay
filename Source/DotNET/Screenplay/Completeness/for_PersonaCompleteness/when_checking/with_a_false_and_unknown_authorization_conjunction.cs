// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness.when_checking;

public class with_a_false_and_unknown_authorization_conjunction : Specification
{
    ApplicationSyntax _application;
    Diagnostic[] _findings;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            policy Member
              require role "A"
            policy Restricted
              require role "Other" and claim "owner" matches subject
            persona Person
              policy Member
            module M
              feature F
                slice StateChange S
                  command C
                    authorize Restricted
            """);
        parsed.Success.ShouldBeTrue();
        _application = parsed.Value!;
    }

    void Because() => _findings = [.. PersonaCompleteness.Check(_application)];

    [Fact] void should_report_a_definite_denial_despite_the_unknown_conjunct() => _findings.Select(finding => finding.Code).ShouldEqual([DiagnosticCodes.PersonaWithoutGate, DiagnosticCodes.GateWithoutPersona]);
}
