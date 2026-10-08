// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_folder_merge_of_first_line_fragments : Specification
{
    string _printed = null!;

    void Because()
    {
        const string first = """
            contribute to Navigation // first header
              navigate to List
            screen template Shell
              main
            """;
        const string second = """
            contribute to Navigation // second header
              navigate to Other
            // between members
            screen template Frame
              main
            """;
        var application = PlayFolderMerge.Merge(
        [
            ScreenplayCompiler.ParsePlaced(first, "first.play", new(["Example"]), ScreenplayLanguageRegistry.Default),
            ScreenplayCompiler.ParsePlaced(second, "second.play", new(["Example"]), ScreenplayLanguageRegistry.Default)
        ]).Value!;
        _printed = new ScreenplayPrinter().Print(application);
    }

    [Theory]
    [InlineData("// first header")]
    [InlineData("// second header")]
    [InlineData("// between members")]
    void should_print_every_comment_once(string comment) => (_printed.Split(comment).Length - 1).ShouldEqual(1);
}
