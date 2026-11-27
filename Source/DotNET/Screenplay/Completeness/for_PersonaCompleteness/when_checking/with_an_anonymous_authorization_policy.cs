// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness.when_checking;

public class with_an_anonymous_authorization_policy : Specification
{
    ApplicationSyntax _application;
    Diagnostic[] _findings;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            policy Member
              require role "A"
            policy Public
              require anonymous
            persona Person
              policy Member
            module M
              feature F
                slice StateChange S
                  command C
                    authorize Public
            """);
        parsed.Success.ShouldBeTrue();
        _application = parsed.Value!;
    }

    void Because() => _findings = [.. PersonaCompleteness.Check(_application)];

    [Fact] void should_admit_the_authenticated_persona() => _findings.Select(finding => finding.Code).ShouldEqual([DiagnosticCodes.PersonaWithoutGate]);
}
