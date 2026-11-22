// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_ui_profile_shell_sections;

public class and_an_icon_logo_has_no_value : given.a_printer
{
    string _printed;

    void Because() => _printed = _printer.Print(new ApplicationSyntax([], [], [], [], SourceLocation.Start)
    {
        UiProfiles = [new UiProfileSyntax("Shop", [], null, [], SourceLocation.Start)
        {
            Identity = new(null, SourceLocation.Start) { LogoKind = "icon" }
        }]
    });

    [Fact] void should_fall_back_to_a_quoted_empty_string_without_a_name() => _printed.ShouldEqual("ui profile Shop\n  identity\n    logo icon \"\"\n");
}
