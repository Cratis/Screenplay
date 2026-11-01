// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_onboarding : given.a_printer
{
    const string Source =
        """
        persona Owner
        persona Accountant

        authentication
          provider GitHub

        onboarding
          signup open joins tenant
          invite tenant as Owner
          invite member
            by Owner
            grant Accountant
          legal terms
            file Legal/terms.md
          offering Starter
          emails
            invitation file Emails/invitation.html
            images Emails/Images
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_accept_the_source() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_print_the_block() => _roundtrip.Printed.ShouldContain("onboarding\n  signup open joins tenant\n  invite tenant as Owner\n  invite member\n    by Owner\n    grant Accountant\n  legal terms\n    file Legal/terms.md\n  offering Starter\n  emails\n    invitation file Emails/invitation.html\n    images Emails/Images");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
