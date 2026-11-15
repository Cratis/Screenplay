// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_need_is_not_provided : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("uses screen InvoiceList"));

    [Fact] void should_warn_about_the_first_need() => With(DiagnosticCodes.UnprovidedScreenNeed).Count().ShouldEqual(1);
    [Fact] void should_warn_and_not_fail() => With(DiagnosticCodes.UnprovidedScreenNeed).Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_need() => With(DiagnosticCodes.UnprovidedScreenNeed).Single().Message.ShouldContain("needs 'Invoice'");
    [Fact] void should_still_compile() => Result.Success.ShouldBeTrue();
}
