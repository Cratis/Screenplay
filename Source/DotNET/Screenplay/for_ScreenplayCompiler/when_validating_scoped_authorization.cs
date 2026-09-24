// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_scoped_authorization : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _unknown;
    CompilationResult<Syntax.ApplicationSyntax> _known;

    void Because()
    {
        _unknown = new ScreenplayCompiler().Compile(
            """
            module Portal
              authorize Missing
              feature Orders
                authorize AlsoMissing
                feature Returns
                  authorize AnotherMissing
            """);
        _known = new ScreenplayCompiler().Compile(
            """
            policy Access
              require authenticated

            module Portal
              authorize Access
              feature Orders
                authorize Access
                feature Returns
                  authorize Access
            """);
    }

    [Fact] void should_warn_for_all_unknown_scoped_policies() => _unknown.Diagnostics.Where(_ => _.Code == DiagnosticCodes.UnknownPolicy).Select(_ => _.Message).ShouldContainOnly(
        "Unknown policy 'Missing' - declare it with 'policy Missing'",
        "Unknown policy 'AlsoMissing' - declare it with 'policy AlsoMissing'",
        "Unknown policy 'AnotherMissing' - declare it with 'policy AnotherMissing'");
    [Fact] void should_accept_known_policies() => _known.Diagnostics.ShouldBeEmpty();
}
