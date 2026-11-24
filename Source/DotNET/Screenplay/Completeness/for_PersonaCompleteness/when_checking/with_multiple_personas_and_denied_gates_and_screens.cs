// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness.when_checking;

public class with_multiple_personas_and_denied_gates_and_screens : Specification
{
    ApplicationSyntax _application;
    Diagnostic[] _findings;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            policy Member
              require role "A" or role "B"
            policy Visitor
              require role "C" or role "D"
            policy Restricted
              require role "Other"
            persona First
              policy Member
            persona Second
              policy Visitor
            module M
              feature F
                slice StateChange S
                  command Save
                    authorize Restricted
                  readmodel R
                  query Read => R[]
                    authorize Restricted
                  screen Locked
                    data R via query Read
                    action Save
            """);
        parsed.Success.ShouldBeTrue();
        _application = parsed.Value!;
    }

    void Because() => _findings = [.. PersonaCompleteness.Check(_application)];

    [Fact]
    void should_keep_persona_then_gate_then_screen_diagnostic_order() => _findings.Select(finding => finding.Code).ShouldEqual([
        DiagnosticCodes.PersonaWithoutGate,
        DiagnosticCodes.AmbiguousPersonaCaller,
        DiagnosticCodes.PersonaWithoutGate,
        DiagnosticCodes.AmbiguousPersonaCaller,
        DiagnosticCodes.GateWithoutPersona,
        DiagnosticCodes.GateWithoutPersona,
        DiagnosticCodes.ScreenWithoutPersona
    ]);

    [Fact]
    void should_keep_authored_persona_and_declaration_order() => _findings.Select(finding => finding.Message).ShouldEqual([
        "Persona 'First' has no policy used by a command, query, or inherited screen gate.",
        "Persona 'First' policy 'Member' has an unchosen buildable alternative 'role \"B\"'; add a policy that pins it.",
        "Persona 'Second' has no policy used by a command, query, or inherited screen gate.",
        "Persona 'Second' policy 'Visitor' has an unchosen buildable alternative 'role \"D\"'; add a policy that pins it.",
        "Command 'Save' is definitely denied to every declared persona's synthesized caller.",
        "Query 'Read' is definitely denied to every declared persona's synthesized caller.",
        "Screen 'Locked' offers nothing to any declared persona: every command and query it binds is definitely denied to every synthesized caller."
    ]);
}
