// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.given;

public static class query_validation_source
{
    public const string Source = """
        concept InvoiceNumber : String
          validate
            min 2 message "Invoice number is too short"
        module Billing
          feature Invoices
            slice StateView Lookup
              readmodel Invoice
                number InvoiceNumber
              query GetInvoice => Invoice optional
                by number InvoiceNumber
              specification MalformedNumber
                when query GetInvoice
                  number = "A"
                then error "Invoice number is too short"
        """;

    public static CompilationResult<SemanticCompilation> Compile(string source = Source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("QueryValidation"));
        const string key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        return new SemanticModelCompiler().Compile("QueryValidation", SemanticDocumentSet.Create([document], catalog));
    }
}
