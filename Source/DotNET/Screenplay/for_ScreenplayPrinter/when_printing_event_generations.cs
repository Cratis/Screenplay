// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_event_generations : given.a_printer
{
    const string Source =
        "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n" +
        "      event ProjectRegistered generation 1\n        projectId Uuid\n" +
        "      event ProjectRegistered generation 2\n        name String\n";

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_both_generations() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_both_generations() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_both_markers() => _roundtrip.Printed.ShouldContain("event ProjectRegistered generation 1");
    [Fact] void should_print_the_current_marker() => _roundtrip.Printed.ShouldContain("event ProjectRegistered generation 2");
    [Fact] void should_print_identically_after_round_trip() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact]
    void should_keep_an_unmarked_first_generation_unmarked()
    {
        var roundtrip = RoundTrip(Source.Replace("event ProjectRegistered generation 1", "event ProjectRegistered", StringComparison.Ordinal));
        roundtrip.Printed.ShouldContain("event ProjectRegistered\n");
        roundtrip.Printed.ShouldContain("event ProjectRegistered generation 2");
        roundtrip.Printed.ShouldNotContain("event ProjectRegistered generation 1");
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    }
}
