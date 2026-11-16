// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_screen_is_declared_twice_in_one_scope : given.a_free_screens_model
{
    void Establish() => Compile("screen Dup\n  title \"One\"\n\nscreen Dup\n  title \"Two\"\n");

    [Fact] void should_report_the_second_screen() => With(DiagnosticCodes.DuplicateScreen).Count().ShouldEqual(1);
}
