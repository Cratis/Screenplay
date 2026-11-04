// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_second_onboarding_block : given.a_compiler
{
    const string Source =
        """
        authentication
          provider GitHub

        onboarding
          signup open joins tenant

        onboarding
          signup open creates tenant
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateOnboarding);
    [Fact] void should_keep_the_first_block() => _result.Value!.Onboarding!.Signup!.Outcome.ShouldEqual(OnboardingSignupOutcome.JoinsTenant);
}
