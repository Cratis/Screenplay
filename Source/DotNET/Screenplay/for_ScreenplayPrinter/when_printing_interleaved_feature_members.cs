// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_interleaved_feature_members : given.a_printer
{
    const string Source =
        """
        module Sales
          feature Orders
            slice StateView First
            feature Nested
              slice StateView Inner
            slice StateView Second
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_slice_and_nested_feature_order() =>
        (_roundtrip.Printed.IndexOf("slice StateView First", StringComparison.Ordinal) <
         _roundtrip.Printed.IndexOf("feature Nested", StringComparison.Ordinal) &&
         _roundtrip.Printed.IndexOf("feature Nested", StringComparison.Ordinal) <
         _roundtrip.Printed.IndexOf("slice StateView Second", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_print_identically_again() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
