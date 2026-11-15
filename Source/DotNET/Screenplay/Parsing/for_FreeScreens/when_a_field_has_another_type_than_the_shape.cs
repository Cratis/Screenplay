// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_field_has_another_type_than_the_shape : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\nmodule Invoicing\n  feature Paying\n    slice StateView ListInvoices\n      readmodel InvoiceRow\n        invoiceNumber String\n        amount String\n      query ListInvoices => InvoiceRow[]\n      uses screen InvoiceList\n        data Invoice via query ListInvoices\n          number from invoiceNumber\n");

    [Fact] void should_report_the_type_mismatch() => With(DiagnosticCodes.ProvisionFieldTypeMismatch).Single().Message.ShouldContain("amount");
}
