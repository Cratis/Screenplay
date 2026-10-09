// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness;

public class when_checking_persona_coverage : Specification
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
        """;

    [Fact] void should_report_a_persona_that_gates_nothing() => Findings(Source).Any(diagnostic => diagnostic.Code == DiagnosticCodes.PersonaWithoutGate).ShouldBeTrue();
    [Fact] void should_report_an_unreachable_command() => Findings(Source).Any(diagnostic => diagnostic.Code == DiagnosticCodes.GateWithoutPersona && diagnostic.Message.Contains("Command 'C'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_buildable_ambiguity_as_information() => Findings(Source).Single(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousPersonaCaller).Severity.ShouldEqual(DiagnosticSeverity.Information);
    [Fact] void should_name_the_unchosen_alternative() => Findings(Source).Single(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousPersonaCaller).Message.ShouldContain("role \"B\"");
    [Fact] void should_not_warn_about_a_used_policy() => Findings(Source.Replace("authorize Restricted", "authorize Member", StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.PersonaWithoutGate).ShouldBeFalse();
    [Fact] void should_include_inherited_gates() => Findings(Source.Replace("module M", "module M\n  authorize Member", StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.PersonaWithoutGate).ShouldBeFalse();
    [Fact] void should_treat_an_unsynthesizable_persona_as_unknown() => Findings(Source.Replace("role \"A\" or role \"B\"", "not role \"Other\"", StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.GateWithoutPersona).ShouldBeFalse();
    [Fact] void should_treat_a_subject_comparison_as_unknown() => Findings(Source.Replace("require role \"Other\"", "require claim \"owner\" matches subject", StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.GateWithoutPersona).ShouldBeFalse();
    [Fact] void should_stay_silent_without_personas() => Findings(Source.Replace("persona Person\n  policy Member\n", string.Empty, StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_not_run_without_selection() => ModelCompleteness.Check(new ScreenplayCompiler().Compile(Source), CompletenessChecks.None).ShouldBeEmpty();

    static IEnumerable<Diagnostic> Findings(string source)
    {
        CompletenessChecks.TryParse("personas", out var checks).ShouldBeTrue();
        return ModelCompleteness.Check(new ScreenplayCompiler().Compile(source), checks);
    }
}
