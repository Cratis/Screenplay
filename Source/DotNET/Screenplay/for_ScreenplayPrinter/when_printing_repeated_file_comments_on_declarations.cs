// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_repeated_file_comments_on_declarations : given.a_printer
{
    const string Source = """
        concept Id : Uuid // header-concept
          file A.cs // earlier-concept
          file B.cs

        type Entry // header-type
          file A.cs // earlier-type
          file B.cs
          id String

        trigger Started // header-trigger
          file A.cs // earlier-trigger
          file B.cs

        behavior Act // header-behavior
          file A.cs // earlier-behavior
          file B.cs

        module Sales
          feature Orders
            slice StateChange Place // header-slice
              file A.cs // earlier-slice
              file B.cs
              event Created // header-event
                file A.cs // earlier-event
                file B.cs
              readmodel Order // header-readmodel
                file A.cs // earlier-readmodel
                file B.cs
              projection Orders => Order // header-projection
                file A.cs // earlier-projection
                file B.cs
                from Created
                  id = id
              specification Works // header-specification
                file A.cs // earlier-specification
                file B.cs
              screen Main // header-screen
                file A.cs // earlier-screen
                file B.cs
            slice StateView View
              readmodel Summary
              reducer Summarize => Summary
                on Created // header-reducer-rule
                  file A.cs // earlier-reducer-rule
                  file B.cs
            slice Automation Sync
              reaction Update
                when Started // header-reaction-trigger
                  file A.cs // earlier-reaction-trigger
                  file B.cs
        """;

    [Theory]
    [InlineData("concept", "concept Id : Uuid")]
    [InlineData("type", "type Entry")]
    [InlineData("trigger", "trigger Started")]
    [InlineData("behavior", "behavior Act")]
    [InlineData("slice", "slice StateChange Place")]
    [InlineData("event", "event Created")]
    [InlineData("readmodel", "readmodel Order")]
    [InlineData("projection", "projection Orders => Order")]
    [InlineData("specification", "specification Works")]
    [InlineData("screen", "screen Main")]
    [InlineData("reducer-rule", "on Created")]
    [InlineData("reaction-trigger", "when Started")]
    void should_keep_header_and_displaced_file_comments_on_separate_lines(string kind, string header)
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Success.ShouldBeTrue();
        roundtrip.Reparsed.Success.ShouldBeTrue();
        var lines = roundtrip.Printed.Split('\n');
        var headerLine = Array.FindIndex(lines, line => line.Trim() == $"{header} // header-{kind}");
        (headerLine >= 0).ShouldBeTrue();
        var commentLine = Array.FindIndex(lines, line => line.Trim() == $"// earlier-{kind}");
        (commentLine > headerLine).ShouldBeTrue();
        lines.Skip(commentLine + 1).First(line => line.Trim().Length > 0).Trim().ShouldEqual("file B.cs");
        lines.Count(line => line.Contains($"// header-{kind}", StringComparison.Ordinal)).ShouldEqual(1);
        lines.Count(line => line.Contains($"// earlier-{kind}", StringComparison.Ordinal)).ShouldEqual(1);
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
