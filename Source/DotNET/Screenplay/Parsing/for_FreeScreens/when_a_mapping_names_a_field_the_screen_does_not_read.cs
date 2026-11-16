// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_mapping_names_a_field_the_screen_does_not_read : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("uses screen InvoiceList\n  data Invoice via query ListInvoices\n    number from invoiceNumber\n    colour from amount"));

    [Fact] void should_report_the_mapping() => With(DiagnosticCodes.UnknownProvisionMapping).Single().Message.ShouldContain("'colour'");
}
