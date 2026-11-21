// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_screen_is_not_adopted : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen);

    [Fact] void should_report_each_screen_as_design_only() => With(DiagnosticCodes.DesignOnlyScreen).Count().ShouldEqual(2);
    [Fact] void should_only_inform() => With(DiagnosticCodes.DesignOnlyScreen).All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_not_fail_the_model() => Result.Success.ShouldBeTrue();
}
