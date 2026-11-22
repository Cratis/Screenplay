// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_policy_implementations;

public class and_the_policy_has_no_implementation : given.a_printer
{
    const string Source = "policy Staff\n  description \"Staff access\"";
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_report_the_missing_implementation() => _roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0114");
    [Fact] void should_not_invent_an_implementation() => _roundtrip.Printed.ShouldEqual(Source + "\n");
    [Fact] void should_report_the_same_diagnostic_on_reparse() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0114");
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
