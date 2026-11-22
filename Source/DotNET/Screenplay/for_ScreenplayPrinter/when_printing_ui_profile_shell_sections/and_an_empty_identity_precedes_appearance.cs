// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_ui_profile_shell_sections;

public class and_an_empty_identity_precedes_appearance : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(new ApplicationSyntax([], [], [], [], SourceLocation.Start)
    {
        UiProfiles = [new UiProfileSyntax("Shop", [], null, [], SourceLocation.Start)
        {
            Identity = new(null, SourceLocation.Start),
            Appearance = new("dark", null, SourceLocation.Start)
        }]
    });

    [Fact] void should_keep_the_empty_section_and_the_following_separator() => _roundtrip.Printed.ShouldEqual("ui profile Shop\n  identity\n\n  appearance\n    color scheme dark\n");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
