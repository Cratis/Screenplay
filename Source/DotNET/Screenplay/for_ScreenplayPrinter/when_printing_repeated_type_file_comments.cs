// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_repeated_type_file_comments : given.a_printer
{
    const string Source = """
        type Entry // type note
          file A.cs // first file note
          file B.cs // second file note
          id String
        """;

    [Fact]
    void should_keep_the_displaced_file_comment_near_the_retained_file()
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Success.ShouldBeTrue();
        roundtrip.Printed.ShouldContain("type Entry // type note\n  // first file note\n  file B.cs // second file note");
        roundtrip.Printed.ShouldNotContain("// type note // first file note");
        roundtrip.Reparsed.Success.ShouldBeTrue();
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
        roundtrip.PrintedAgain.Split('\n').Count(line => line.Contains("// type note", StringComparison.Ordinal)).ShouldEqual(1);
        roundtrip.PrintedAgain.Split('\n').Count(line => line.Contains("// first file note", StringComparison.Ordinal)).ShouldEqual(1);
    }
}
