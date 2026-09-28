// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_legacy_code_comments_under_a_handler : given.a_printer
{
    const string Source = """
        module Shop
          feature Orders
            slice StateChange Place
              command Open
                id String
                handler // handler note
                  csharp // language note
                    ```
                    return;
                    ```
        """;

    [Fact]
    void should_keep_the_header_and_language_comments_on_separate_lines()
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Success.ShouldBeTrue();
        roundtrip.Printed.ShouldContain("handler // handler note\n          // language note\n          ```csharp");
        roundtrip.Printed.ShouldNotContain("// handler note // language note");
        roundtrip.Reparsed.Success.ShouldBeTrue();
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
