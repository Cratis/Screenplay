// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_declaring_free_screens : given.a_free_screens_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Establish() => _result = new ScreenplayCompiler().Compile(Model);

    [Fact] void should_report_no_warning_or_error() => string.Join("\n", _result.Diagnostics.Where(d => d.Severity != DiagnosticSeverity.Information).Select(d => $"{d.Code} {d.Severity} {d.Message}")).ShouldEqual(string.Empty);
    [Fact] void should_inform_that_the_unadopted_screen_is_design_only() => _result.Diagnostics.Single(d => d.Severity == DiagnosticSeverity.Information).Message.ShouldContain("InvoiceDetails");
    [Fact] void should_keep_the_shape() => _result.Value!.Shapes.Single().Properties.Select(property => property.Name).ShouldEqual(["number", "amount"]);
    [Fact] void should_keep_both_screens() => _result.Value!.Screens.Select(screen => screen.Name).ShouldEqual(["InvoiceList", "InvoiceDetails"]);
    [Fact] void should_keep_the_need() => _result.Value!.Screens.First().Directives.OfType<ScreenNeedSyntax>().Single().Type.Name.ShouldEqual("Invoice");
    [Fact] void should_keep_the_adoption() => _result.Value!.Modules.Single().Features.Single().Slices.Single().UsedScreens.Single().Provisions.Single().Mappings.Single().Path.ShouldEqual("invoiceNumber");
    [Fact] void should_print_every_declaration_where_it_was_authored() => new ScreenplayPrinter().Print(_result.Value!).ShouldContain("uses screen InvoiceList\n        data Invoice via query ListInvoices\n          number from invoiceNumber");
    [Fact] void should_round_trip_through_the_printer() => new ScreenplayPrinter().Print(new ScreenplayCompiler().Compile(new ScreenplayPrinter().Print(_result.Value!)).Value!).ShouldEqual(new ScreenplayPrinter().Print(_result.Value!));
}
