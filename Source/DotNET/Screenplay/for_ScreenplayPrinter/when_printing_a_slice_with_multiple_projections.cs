// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_slice_with_multiple_projections : given.a_printer
{
    const string Source =
        """
        module CustomerPortal
          feature Report
            slice StateView CustomerPortalReport
              event ReportGenerated
                reportId Uuid

              event TokenRevoked
                reportId Uuid

              projection PortalReport => PortalReport
                from ReportGenerated
                  reportId = reportId

              projection RevokedCustomerPortalToken => RevokedCustomerPortalToken
                from TokenRevoked
                  reportId = reportId
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_both_projections() => Slice(_roundtrip.Reparsed).Projections.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_read_models() => Slice(_roundtrip.Reparsed).Projections.Select(_ => _.ReadModel).ShouldContainOnly(["PortalReport", "RevokedCustomerPortalToken"]);

    static SliceSyntax Slice(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single();
}
