// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

/// <summary>
/// A consumer navigating a document back to the code it describes reaches every file reference through the
/// walker. A declaration that carries one and is not descended into is invisible to it - the tree grew and the
/// consumer silently stopped covering the document.
/// </summary>
public class when_walking_declarations_that_name_a_file : Specification
{
    const string Source =
        """
        concept InvoiceId : Uuid
          file Invoicing/InvoiceId.cs

        type InvoiceLine
          file Invoicing/InvoiceLine.cs
          amount Decimal

        trigger LedgerFileArrived
          file Integrations/LedgerFileTrigger.cs

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              file Invoicing/RegisterInvoice/RegisterInvoice.cs

              event InvoiceRegistered
                file Invoicing/RegisterInvoice/RegisterInvoice.cs
                invoiceId InvoiceId

              readmodel Invoice
                file Invoicing/RegisterInvoice/Invoice.cs
                invoiceId InvoiceId

              projection Invoices => Invoice
                file Invoicing/RegisterInvoice/Invoices.cs
                from InvoiceRegistered
                  invoiceId = invoiceId

              specification RegisteringAnInvoice
                file Invoicing/RegisterInvoice/when_registering_an_invoice.cs
                then InvoiceRegistered
        """;

    ApplicationSyntax _document;
    given.a_counting_walker _walker;
    IReadOnlyList<SyntaxNode> _expected;

    void Establish()
    {
        _document = new ScreenplayCompiler().Compile(Source).Value!;
        _walker = new();
        _expected = given.SyntaxNodes.Under(_document);
    }

    void Because() => _walker.VisitApplication(_document);

    [Fact] void should_reach_every_node_the_tree_holds() => _walker.Nodes.Count.ShouldEqual(_expected.Count);
    [Fact] void should_reach_every_file_reference() => _walker.Nodes.OfType<FileReferenceSyntax>().Count().ShouldEqual(8);
    [Fact] void should_reach_the_one_each_declaration_names() =>
        _walker.Nodes.OfType<FileReferenceSyntax>().Select(_ => _.Path).Distinct().Count().ShouldEqual(7);
}
