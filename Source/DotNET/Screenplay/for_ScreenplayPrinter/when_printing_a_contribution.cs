// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_contribution : given.a_printer
{
    const string Source =
        """
        module Invoicing
          screen template AppShell
            navbar contributes Navigation
            main

          contribute to Navigation
            navigate to InvoiceList
            label "Invoices"
            order 10

          feature InvoiceManagement
            slice StateView InvoiceList
              screen InvoiceList

            contribute to Navigation
              navigate to InvoiceList
              label "Adjustments"
              order 20
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_slot_contribution_point() => _roundtrip.Printed.ShouldContain("navbar contributes Navigation");
    [Fact] void should_print_the_module_contribution_header() => _roundtrip.Printed.ShouldContain("contribute to Navigation");
    [Fact] void should_print_the_contribution_navigation() => _roundtrip.Printed.ShouldContain("navigate to InvoiceList");
    [Fact] void should_print_the_contribution_label() => _roundtrip.Printed.ShouldContain("label \"Invoices\"");
    [Fact] void should_print_the_contribution_order() => _roundtrip.Printed.ShouldContain("order 10");
    [Fact] void should_print_the_feature_contribution_label() => _roundtrip.Printed.ShouldContain("label \"Adjustments\"");
    [Fact] void should_preserve_the_module_contribution() => Module.Contributions!.Single().Label.ShouldEqual("Invoices");
    [Fact] void should_preserve_the_feature_contribution() => Module.Features.Single().Contributions!.Single().Label.ShouldEqual("Adjustments");

    ModuleSyntax Module => _roundtrip.Reparsed.Value!.Modules.Single();
}
