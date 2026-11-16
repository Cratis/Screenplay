// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_by_parameter_is_unknown : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("uses screen InvoiceList\n  data Invoice via query ListInvoices by nothing\n    number from invoiceNumber"));

    [Fact] void should_report_the_parameter() => With(DiagnosticCodes.UnknownProvisionParameter).Single().Message.ShouldContain("'nothing'");
}
