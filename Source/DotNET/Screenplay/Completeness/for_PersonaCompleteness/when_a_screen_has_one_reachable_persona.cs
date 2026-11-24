// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness;

public class when_a_screen_has_one_reachable_persona : Specification
{
    ApplicationSyntax _application;
    Diagnostic[] _findings;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            policy Member
              require role "A"
            policy Restricted
              require role "Other"
            persona Person
              policy Member
            persona Other
              policy Restricted
            module M
              feature F
                slice StateChange S
                  command C
                    authorize Restricted
                  screen View
                    action C
            """);
        parsed.Success.ShouldBeTrue();
        _application = parsed.Value!;
    }

    void Because() => _findings = [.. PersonaCompleteness.Check(_application)];

    [Fact] void should_not_report_the_screen_or_its_binding_as_denied_to_everyone() => _findings.Select(finding => finding.Code).ShouldEqual([DiagnosticCodes.PersonaWithoutGate]);
}
