// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness;

public class when_enumerating_persona_findings : Specification
{
    const string Source = """
        policy Member
          require role "A" or role "B"
        policy Restricted
          require role "Other"
        persona Person
          policy Member
        module M
          feature F
            slice StateChange S
              command C
                authorize Restricted
              readmodel Rm
                value String
              query Q => Rm
                authorize Restricted
              screen Locked
                data Rm via query Q
                action C
        """;

    [Fact]
    void should_report_persona_findings_before_gate_findings_before_screen_findings()
    {
        var codes = Findings().Select(diagnostic => diagnostic.Code).Where(code => code is DiagnosticCodes.PersonaWithoutGate or DiagnosticCodes.AmbiguousPersonaCaller or DiagnosticCodes.GateWithoutPersona or DiagnosticCodes.ScreenWithoutPersona);

        codes.SequenceEqual([
            DiagnosticCodes.PersonaWithoutGate,
            DiagnosticCodes.AmbiguousPersonaCaller,
            DiagnosticCodes.GateWithoutPersona,
            DiagnosticCodes.GateWithoutPersona,
            DiagnosticCodes.ScreenWithoutPersona]).ShouldBeTrue();
    }

    [Fact]
    void should_report_gates_in_declaration_order()
    {
        var gates = Findings().Where(diagnostic => diagnostic.Code == DiagnosticCodes.GateWithoutPersona).Select(diagnostic => diagnostic.Message).ToArray();

        gates[0].ShouldContain("Command 'C'");
        gates[1].ShouldContain("Query 'Q'");
    }

    [Fact] void should_not_examine_the_application_before_enumeration() => Catch.Exception(() => PersonaCompleteness.Check(null!)).ShouldBeNull();
    [Fact] void should_examine_the_application_on_enumeration() => Catch.Exception(() => PersonaCompleteness.Check(null!).ToArray()).ShouldBeOfExactType<NullReferenceException>();

    static IEnumerable<Diagnostic> Findings()
    {
        CompletenessChecks.TryParse("personas", out var checks).ShouldBeTrue();
        return ModelCompleteness.Check(new ScreenplayCompiler().Compile(Source), checks);
    }
}
