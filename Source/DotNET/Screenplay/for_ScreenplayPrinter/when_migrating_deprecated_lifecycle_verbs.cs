// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_migrating_deprecated_lifecycle_verbs : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(a_lifecycle_source.Source
        .Replace("entering invokes ChargeFee", "entering send ChargeFee", StringComparison.Ordinal)
        .Replace("entering starts Collect", "entering start Collect", StringComparison.Ordinal)
        .Replace("action TagLine invokes", "action TagLine sends", StringComparison.Ordinal)
        .Replace("then invokes ChargeFee", "then sends ChargeFee", StringComparison.Ordinal));

    [Fact] void should_accept_the_old_forms() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_warn_for_every_old_form() => _roundtrip.Original!.Diagnostics.Count(_ => _.Code == DiagnosticCodes.LegacyInlineCodeFence).ShouldBeGreaterThan(4);
    [Fact] void should_warn_for_entering_send() => _roundtrip.Original!.Diagnostics.Any(_ => _.Message.Contains("'entering send' is deprecated", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_warn_for_entering_start() => _roundtrip.Original!.Diagnostics.Any(_ => _.Message.Contains("'entering start' is deprecated", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_warn_for_an_action_sends() => _roundtrip.Original!.Diagnostics.Any(_ => _.Message.Contains("'action ... sends' is deprecated", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_warn_for_then_sends() => _roundtrip.Original!.Diagnostics.Any(_ => _.Message.Contains("'then sends' is deprecated", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_print_entering_invokes() => _roundtrip.Printed.ShouldContain("entering invokes ChargeFee");
    [Fact] void should_print_entering_starts() => _roundtrip.Printed.ShouldContain("entering starts Collect");
    [Fact] void should_print_action_invokes() => _roundtrip.Printed.ShouldContain("action TagLine invokes TagInvoice");
    [Fact] void should_not_print_a_sends_form() => _roundtrip.Printed.ShouldNotContain(" sends ");
    [Fact] void should_not_print_an_old_entering_form() => _roundtrip.Printed.ShouldNotContain("entering send");
    [Fact] void should_reparse_without_warnings() => _roundtrip.Reparsed.Diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
