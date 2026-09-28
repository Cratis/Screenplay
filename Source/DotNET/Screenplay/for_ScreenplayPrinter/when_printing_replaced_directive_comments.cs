// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_replaced_directive_comments : given.a_printer
{
    const string Source = """
        module Sales
          feature Orders
            slice StateChange Place
              command Open // header-command
                name String
                handler // first-handler
                  file Handlers/Old.cs // old-file
                handler
                  file Handlers/New.cs
              event Opened
                name String
            slice StateView View
              readmodel Order
                id Uuid
              query Find => Order // header-query
                by oldId Uuid // earlier-by
                by id Uuid
        """;

    [Theory]
    [InlineData("header-command", "command Open", "first-handler", "handler")]
    [InlineData("header-command", "command Open", "old-file", "handler")]
    [InlineData("header-query", "query Find => Order", "earlier-by", "by id Uuid")]
    void should_keep_header_and_replaced_directive_comments_apart(string headerComment, string header, string replacedComment, string retained)
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Success.ShouldBeTrue();
        roundtrip.Reparsed.Success.ShouldBeTrue();
        var lines = roundtrip.Printed.Split('\n');
        lines.Count(line => line.Trim() == $"{header} // {headerComment}").ShouldEqual(1);
        var commentLine = Array.FindIndex(lines, line => line.Trim() == $"// {replacedComment}");
        (commentLine >= 0).ShouldBeTrue();
        lines.Skip(commentLine + 1).First(line => line.Trim().Length > 0 && !line.Trim().StartsWith("//", StringComparison.Ordinal))
            .Trim().ShouldEqual(retained);
        lines.Count(line => line.Contains($"// {replacedComment}", StringComparison.Ordinal)).ShouldEqual(1);
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
