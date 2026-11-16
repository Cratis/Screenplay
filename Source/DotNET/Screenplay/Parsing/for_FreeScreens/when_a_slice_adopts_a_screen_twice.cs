// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_slice_adopts_a_screen_twice : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("uses screen InvoiceList\n  data Invoice via query ListInvoices\n    number from invoiceNumber\nuses screen InvoiceList"));

    [Fact] void should_report_the_second_adoption() => With(DiagnosticCodes.DuplicateScreenAdoption).Count().ShouldEqual(1);
}
