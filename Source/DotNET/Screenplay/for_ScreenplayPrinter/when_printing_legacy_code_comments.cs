// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_legacy_code_comments : given.a_printer
{
    const string Source = """
        module Shop
          feature Orders
            slice StateView Orders
              screen Details
                typescript // legacy language note
                  ```
                  const value = 1;
                  ```
        """;

    [Fact]
    void should_keep_comment_before_fence_and_preserve_code_on_reprint()
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Success.ShouldBeTrue();
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
        roundtrip.Printed.ShouldContain("        // legacy language note\n        ```typescript\n");
        roundtrip.Printed.ShouldNotContain("```typescript //");
        roundtrip.Printed.Split("// legacy language note", StringSplitOptions.None).Length.ShouldEqual(2);
        roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.Count().ShouldEqual(1);
        roundtrip.Printed.ShouldContain("const value = 1;");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
