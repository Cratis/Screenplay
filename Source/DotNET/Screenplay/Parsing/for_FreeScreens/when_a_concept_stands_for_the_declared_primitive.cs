// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_concept_stands_for_the_declared_primitive : given.a_free_screens_model
{
    void Establish() => Compile("concept InvoiceNumber : String\n\n" + Shapes + "\n" + FreeScreen + "\nmodule Invoicing\n  feature Paying\n    slice StateView ListInvoices\n      readmodel InvoiceRow\n        invoiceNumber InvoiceNumber\n        amount Decimal\n      query ListInvoices => InvoiceRow[]\n      uses screen InvoiceList\n        data Invoice via query ListInvoices\n          number from invoiceNumber\n");

    [Fact] void should_accept_the_concept() => With(DiagnosticCodes.ProvisionFieldTypeMismatch).ShouldBeEmpty();
}
