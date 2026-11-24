// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness;

public class when_multiple_personas_precede_denied_gates_and_screens : Specification
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
        "Persona 'First' has no policy referenced by an authorization gate",
        "Persona 'First' policy 'Member' has unchosen alternative role \"B\"; pin a required policy or use an explicit caller",
        "Persona 'Second' has no policy referenced by an authorization gate",
        "Persona 'Second' policy 'Visitor' has unchosen alternative role \"D\"; pin a required policy or use an explicit caller",
        "Authorization on command 'Save' denies every declared persona",
        "Authorization on query 'Read' denies every declared persona",
        "Screen 'Locked' has no bound query or action usable by any declared persona"
    ]);
}
