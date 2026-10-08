// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_first_line_placed_members : Specification
{
    const string Source = """
        contribute to Navigation // First member
          navigate to List
        screen template Shell
          main
        feature View
          slice StateView List
        """;

    string _printed = null!;

    void Because()
    {
        var application = ScreenplayCompiler.ParsePlaced(Source, "module-body.play", new(["Example"]), ScreenplayLanguageRegistry.Default).Value!;
        _printed = new ScreenplayPrinter().Print(application);
    }

    [Fact] void should_keep_the_first_line_contribution_before_the_template() => (_printed.IndexOf("contribute to Navigation", StringComparison.Ordinal) < _printed.IndexOf("screen template Shell", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_first_members_trailing_comment_on_its_header() => _printed.ShouldContain("contribute to Navigation // First member");
}
