// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_free_screen_navigates_to_a_screen_nothing_declares : given.a_free_screens_model
{
    void Establish() => Compile("screen Lost\n  data Invoice[]\n  table Invoice\n    column amount\n    on row-click navigate to Nowhere by amount\n");

    [Fact] void should_warn_about_the_unknown_screen() => With(DiagnosticCodes.UnknownScreen).Single().Message.ShouldContain("'Nowhere'");
}
