// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_sample_data : given.a_printer
{
    const string Source =
        """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                name String
                revenue Decimal
                active Bool
                notes String optional
              readmodel Tickets
                subject String

        sample data NordicDemo
          title "CRM · Nordic SMB"
          locale nb-NO
          shape Customer
            row
              name = "Fjordline \"AS\""
              revenue = 125000.5
              active = true
              notes = null
            row
              name = "Nordlys Handel AS"

        sample data HelpdeskDemo
          shape Tickets
            row
              subject = "Reset a password"

        ui profile Desktop
          target platform web

          sample data
            active NordicDemo
            shape Customer maps to Customer
            bind Tickets to HelpdeskDemo

        ui profile Phone
          sample data
            active blueprint crm-nordic-smb
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_pack() => _roundtrip.Printed.ShouldContain("sample data NordicDemo\n  title \"CRM · Nordic SMB\"\n  locale nb-NO\n  shape Customer\n    row\n      name = \"Fjordline \\\"AS\\\"\"\n      revenue = 125000.5\n      active = true\n      notes = null\n    row\n      name = \"Nordlys Handel AS\"");
    [Fact] void should_print_the_selection_in_the_ui_profile() => _roundtrip.Printed.ShouldContain("sample data\n    active NordicDemo\n    shape Customer maps to Customer\n    bind Tickets to HelpdeskDemo");
    [Fact] void should_print_a_blueprint_pack() => _roundtrip.Printed.ShouldContain("active blueprint crm-nordic-smb");
    [Fact] void should_preserve_the_packs() => _roundtrip.Reparsed.Value!.SampleDataPacks.Select(pack => pack.Name).ShouldContainOnly("NordicDemo", "HelpdeskDemo");
    [Fact] void should_preserve_the_rows() => _roundtrip.Reparsed.Value!.SampleDataPacks.First().Sets.Single().Rows.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_active_pack() => _roundtrip.Reparsed.Value!.UiProfiles!.First().SampleData!.Active!.Name.ShouldEqual("NordicDemo");
}
