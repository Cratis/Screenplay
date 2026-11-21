// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_provision_names_a_need_the_screen_lacks : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("uses screen InvoiceList\n  data Customer via query ListInvoices"));

    [Fact] void should_report_the_unknown_need() => With(DiagnosticCodes.UnknownScreenNeed).Single().Message.ShouldContain("'Customer'");
    [Fact] void should_report_an_error() => With(DiagnosticCodes.UnknownScreenNeed).Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
