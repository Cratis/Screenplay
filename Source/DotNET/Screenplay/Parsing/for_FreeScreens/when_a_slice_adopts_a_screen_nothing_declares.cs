// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_slice_adopts_a_screen_nothing_declares : given.a_free_screens_model
{
    void Establish() => Compile(Slice("uses screen Missing"));

    [Fact] void should_warn_about_the_unknown_screen() => With(DiagnosticCodes.UnknownScreen).Single().Message.ShouldContain("'Missing'");
    [Fact] void should_only_warn() => With(DiagnosticCodes.UnknownScreen).Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
