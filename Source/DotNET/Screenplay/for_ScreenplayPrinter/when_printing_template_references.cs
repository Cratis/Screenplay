// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_template_references : given.a_printer
{
    const string Source =
        """
        module Sales
          screen template OrderList
            category List
            scopes feature, slice stateview
            body

          dialog template ConfirmOrder
            category Dialog
            scopes slice statechange|stateview
            body

          screen template Workspace
            category Workspace
            scopes none
            body

          template blueprint Crm Workspace version "^1.4"

          feature Orders
            template blueprint Crm Pipeline

            slice StateView OrderList
              template OrderList

              screen Orders
                template blueprint Crm CustomerDetail version "~1.4.2"
                  body
                    title "Orders"

        ui profile Desktop
          packages
            Crm
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_a_module_blueprint_assignment_with_its_version() => _roundtrip.Printed.ShouldContain("  template blueprint Crm Workspace version \"^1.4\"");
    [Fact] void should_print_a_feature_blueprint_assignment_without_a_version() => _roundtrip.Printed.ShouldContain("    template blueprint Crm Pipeline\n");
    [Fact] void should_print_a_screen_blueprint_reference_with_its_version() => _roundtrip.Printed.ShouldContain("template blueprint Crm CustomerDetail version \"~1.4.2\"");
    [Fact] void should_print_an_own_assignment_plainly() => _roundtrip.Printed.ShouldContain("      template OrderList\n");
    [Fact] void should_print_slice_kinds_beside_the_slice_scope() => _roundtrip.Printed.ShouldContain("scopes feature, slice stateview");
    [Fact] void should_print_several_slice_kinds_in_declaration_order() => _roundtrip.Printed.ShouldContain("scopes slice statechange|stateview");
    [Fact] void should_keep_scopes_none() => _roundtrip.Printed.ShouldContain("scopes none");
    [Fact] void should_preserve_the_blueprint_reference_after_reparsing() => _roundtrip.Reparsed.Value!.Modules.Single().Templates.Single().Blueprint.ShouldEqual("Crm");
    [Fact] void should_preserve_the_version_after_reparsing() => _roundtrip.Reparsed.Value!.Modules.Single().Templates.Single().Version.ShouldEqual("^1.4");
    [Fact] void should_preserve_the_slice_kinds_after_reparsing() => _roundtrip.Reparsed.Value!.Modules.Single().DialogTemplates!.Single().SliceKinds.ShouldEqual(["StateChange", "StateView"]);
}
