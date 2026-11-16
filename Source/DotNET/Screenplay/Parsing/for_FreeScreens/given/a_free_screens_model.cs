// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens.given;

public class a_free_screens_model : Specification
{
    protected const string Shapes =
        """
        shape Invoice
          description "What the invoice list reads"
          number String
          amount Decimal

        """;

    protected const string FreeScreen =
        """
        screen InvoiceList
          title "Invoices"
          data Invoice[]
          table Invoice
            column number label "#"
            column amount
            on row-click navigate to InvoiceDetails by number

        screen InvoiceDetails
          data Invoice
          summary Invoice
            field number label "Number"

        """;

    protected const string Module =
        """
        module Invoicing
          feature Paying
            slice StateView ListInvoices
              readmodel InvoiceRow
                invoiceNumber String
                amount Decimal
              query ListInvoices => InvoiceRow[]
              uses screen InvoiceList
                data Invoice via query ListInvoices
                  number from invoiceNumber

        """;

    protected const string Model = Shapes + "\n" + FreeScreen + "\n" + Module;

    protected CompilationResult<ApplicationSyntax> Result { get; private set; }

    protected IEnumerable<Diagnostic> Reported => Result.Diagnostics;

    protected IEnumerable<Diagnostic> With(string code) => Result.Diagnostics.Where(diagnostic => diagnostic.Code == code);

    protected void Compile(string source) => Result = new ScreenplayCompiler().Compile(source);

    protected static string Slice(string body) =>
        "module Invoicing\n  feature Paying\n    slice StateView ListInvoices\n      readmodel InvoiceRow\n        invoiceNumber String\n        amount Decimal\n      query ListInvoices => InvoiceRow[]\n" +
        string.Join('\n', body.Split('\n').Select(line => "      " + line)) + "\n";
}
