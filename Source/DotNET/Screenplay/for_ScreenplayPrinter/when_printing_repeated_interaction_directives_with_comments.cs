// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_repeated_interaction_directives_with_comments : given.a_printer
{
    const string Source = """
        behavior Confirm
          order 1 // first order
          order 2 // second order
          on click
            confirm "Continue?" // action
              on success // first success
                notify info "First"
              on success // second success
                notify info "Second"
        """;

    [Fact]
    void should_keep_every_comment_separate_and_near_its_directive()
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
        roundtrip.Printed.ShouldContain("  // first order\n  order 2 // second order");
        roundtrip.Printed.ShouldContain("      // first success\n      on success // second success");
        roundtrip.Printed.ShouldContain("confirm \"Continue?\" // action\n");
        foreach (var comment in new[] { "first order", "second order", "action", "first success", "second success" })
        {
            roundtrip.Printed.Split($"// {comment}", StringSplitOptions.None).Length.ShouldEqual(2);
        }

        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
