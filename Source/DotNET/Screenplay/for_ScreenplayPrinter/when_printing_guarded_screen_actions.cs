// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_guarded_screen_actions : given.a_printer
{
    const string Source =
        """
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                itemId Uuid
                status String
              query ItemDetails => Item
              command Retry
                itemId Uuid
              screen Details
                data Item via query ItemDetails
                action "Again"
                  when item.status == "open" or (item.status == "failed" and item.status != null) execute Retry
                    with itemId from item.itemId
                  otherwise hidden
                  navigate to Details
                action $strings.retry
                  when item.status starts with "fail" execute Retry
                  otherwise execute Retry
                    with itemId from item.itemId
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_printed_source_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_indent_alternative_arguments() => _roundtrip.Printed.ShouldContain("execute Retry\n            with itemId from item.itemId");
    [Fact] void should_indent_execute_fallback_arguments() => _roundtrip.Printed.ShouldContain("          otherwise execute Retry\n            with itemId from item.itemId");
    [Fact] void should_preserve_explicit_hidden() => _roundtrip.Printed.ShouldContain("otherwise hidden");
    [Fact] void should_preserve_the_localized_label() => _roundtrip.Printed.ShouldContain("action $strings.retry");
}
