// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_reprinting_canonical_source_with_comments : given.a_printer
{
    const string Source =
        """
        // @public
        module Sales // module owner

          feature Orders

            slice StateChange Place

              // comment on command
              command Place // command owner
                id Uuid // property owner
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source + "\n");

    [Fact] void should_keep_canonical_bytes_exactly() => _roundtrip.Printed.ShouldEqual(Source + "\n");
    [Fact] void should_be_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
