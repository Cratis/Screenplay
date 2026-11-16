// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_the_same_shape_is_needed_twice : given.a_free_screens_model
{
    void Establish() => Compile("screen Twice\n  data Invoice[]\n  data Invoice[]\n  table Invoice\n    column amount\n");

    [Fact] void should_ask_for_an_alias() => With(DiagnosticCodes.DuplicateScreenNeed).Single().Message.ShouldContain("as <alias>");
}
