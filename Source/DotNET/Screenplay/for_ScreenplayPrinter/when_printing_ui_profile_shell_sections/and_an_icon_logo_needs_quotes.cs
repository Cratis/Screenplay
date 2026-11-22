// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_ui_profile_shell_sections;

public class and_an_icon_logo_needs_quotes : given.a_printer
{
    string _printed;

    void Because() => _printed = _printer.Print(new ApplicationSyntax([], [], [], [], SourceLocation.Start)
    {
        UiProfiles = [new UiProfileSyntax("Shop", [], null, [], SourceLocation.Start)
        {
            Identity = new("", SourceLocation.Start) { LogoKind = "icon", LogoValue = "shop \"bag\"" }
        }]
    });

    [Fact] void should_quote_and_escape_the_icon_and_preserve_the_empty_name() => _printed.ShouldEqual("ui profile Shop\n  identity\n    name \"\"\n    logo icon \"shop \\\"bag\\\"\"\n");
}
