// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_two_scopes_declare_the_screen_an_adopter_names : given.a_free_screens_model
{
    void Establish() => Compile("module A\n  screen Shared\n    title \"A\"\n\nmodule B\n  screen Shared\n    title \"B\"\n\nmodule C\n  feature F\n    slice StateView S\n      readmodel R\n        v String\n      query Q => R[]\n      uses screen Shared\n");

    [Fact] void should_warn_that_the_reference_is_ambiguous() => With(DiagnosticCodes.AmbiguousReference).Count().ShouldEqual(1);
}
