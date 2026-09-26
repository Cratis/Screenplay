// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_fits_slot_comments : given.a_printer
{
    const string Source = """
        type CatalogEntry
          // generated implementation
          file Types/CatalogEntry.cs
          name String
        module Shop
          screen template Shell
            // shell goes in the content region
            fits slot content // target slot
            main contributes Navigation
          feature Ordering
            slice StateView Landing
              screen Home
                // source of the home screen
                file Screens/Home.cs
          contribute to Navigation
            // open the landing page
            navigate to Home
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_leading_comment_inside_the_template() => _roundtrip.Printed.ShouldContain("  screen template Shell\n    // shell goes in the content region\n    fits slot content // target slot");
    [Fact] void should_keep_the_type_file_comment_inside_the_type() => _roundtrip.Printed.ShouldContain("type CatalogEntry\n  // generated implementation\n  file Types/CatalogEntry.cs");
    [Fact] void should_keep_the_screen_file_comment_inside_the_screen() => _roundtrip.Printed.ShouldContain("      screen Home\n        // source of the home screen\n        file Screens/Home.cs");
    [Fact] void should_keep_the_navigation_comment_inside_the_contribution() => _roundtrip.Printed.ShouldContain("  contribute to Navigation\n    // open the landing page\n    navigate to Home");
    [Fact] void should_stay_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_print_a_changed_fits_slot_name_without_losing_its_comments()
    {
        var original = _roundtrip.Original!.Value!;
        var module = original.Modules.Single();
        var changed = original with
        {
            Modules = [module with { ScreenTemplates = [module.ScreenTemplates.Single() with { FitsSlot = "sidebar" }] }]
        };
        var printed = _printer.Print(changed);
        printed.ShouldContain("// shell goes in the content region\n    fits slot sidebar // target slot");
        printed.ShouldNotContain("fits slot content");
    }
}
