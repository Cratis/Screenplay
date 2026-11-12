// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness;

public class when_checking_that_a_screen_offers_a_persona_something : Specification
{
    const string Source = """
        policy Member
          require role "A"
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

    [Fact] void should_report_a_screen_whose_bindings_are_all_denied() => Findings(Source).Any(diagnostic => diagnostic.Code == DiagnosticCodes.ScreenWithoutPersona && diagnostic.Message.Contains("'Locked'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_stay_silent_when_one_binding_is_available() => Findings(Source.Replace("query Q => Rm\n        authorize Restricted", "query Q => Rm\n        authorize Member", StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.ScreenWithoutPersona).ShouldBeFalse();
    [Fact] void should_stay_silent_for_a_screen_without_bindings() => Findings(Source.Replace("        data Rm via query Q\n        action C", "        title \"T\"", StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.ScreenWithoutPersona).ShouldBeFalse();
    [Fact] void should_stay_silent_without_personas() => Findings(Source.Replace("persona Person\n  policy Member\n", string.Empty, StringComparison.Ordinal)).Any(diagnostic => diagnostic.Code == DiagnosticCodes.ScreenWithoutPersona).ShouldBeFalse();

    static IEnumerable<Diagnostic> Findings(string source)
    {
        CompletenessChecks.TryParse("personas", out var checks).ShouldBeTrue();
        return ModelCompleteness.Check(new ScreenplayCompiler().Compile(source), checks);
    }
}
