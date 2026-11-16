// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_shape_is_declared_twice : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + Shapes);

    [Fact] void should_report_the_second_shape() => With(DiagnosticCodes.DuplicateShape).Count().ShouldEqual(1);
}
