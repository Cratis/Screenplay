// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_arrangement_header_comments : given.a_printer
{
    [Theory]
    [InlineData("layout Main", "arrangement flow")]
    [InlineData("screen template Main", "arrangement flow")]
    [InlineData("dialog template Main", "arrangement freeform")]
    void should_keep_comment_on_arrangement_header_after_blank_separator(string header, string arrangement)
    {
        var indent = header == "layout Main" ? string.Empty : "  ";
        var source = header == "layout Main"
            ? $"{header}\n  content\n  {arrangement} // arrangement note\n"
            : $"module Shop\n  {header}\n    content\n    {arrangement} // arrangement note\n";
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
        roundtrip.Printed.ShouldContain($"\n{indent}  {arrangement} // arrangement note\n");
        roundtrip.Printed.Split("// arrangement note", StringSplitOptions.None).Length.ShouldEqual(2);
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
