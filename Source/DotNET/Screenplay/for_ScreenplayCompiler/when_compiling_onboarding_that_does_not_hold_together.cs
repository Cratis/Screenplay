// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_onboarding_that_does_not_hold_together : given.a_compiler
{
    const string Source =
        """
        persona Owner

        onboarding
          signup open creates tenant
          signup open joins tenant
          invite tenant as Owner
          invite member
            by Owner, Stranger
          invite member
            by Owner
            grant Owner
          legal terms
          offering
          emails
            goodbye file Emails/goodbye.html
            welcome file Emails/welcome.html
            welcome file Emails/again.html
          surprise
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    bool Reported(string code) => _result.Diagnostics.Any(_ => _.Code == code);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_require_authentication() => Reported(DiagnosticCodes.OnboardingWithoutAuthentication).ShouldBeTrue();
    [Fact] void should_report_a_repeated_directive() => Reported(DiagnosticCodes.DuplicateOnboardingDirective).ShouldBeTrue();
    [Fact] void should_report_an_unknown_persona() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnknownOnboardingPersona && _.Message.Contains("Stranger")).ShouldBeTrue();
    [Fact] void should_report_a_malformed_or_incomplete_directive() => _result.Diagnostics.Count(_ => _.Code == DiagnosticCodes.InvalidOnboardingDirective).ShouldBeGreaterThan(3);
    [Fact] void should_report_an_email_onboarding_does_not_send() => Reported(DiagnosticCodes.UnknownOnboardingEmail).ShouldBeTrue();
}
