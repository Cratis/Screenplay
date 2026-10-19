// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query.given;

public class a_performed_query : for_SemanticModelBinder.given.a_semantic_binder
{
    protected const string Source =
        """
        concept Region : String
        module Billing
          feature Invoices
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String
                region Region
                status String
                total Int
              readmodel Ledger
                book String key
                page Int key
                total Int
              query CurrentFigures => InvoiceFigures
                by invoiceId String
                performer
                  file Queries/CurrentFigures.cs
              query LedgerPage => Ledger optional
                by
                  book String
                  page Int
                performer
                  file Queries/LedgerPage.cs
              query FiguresInRegion => InvoiceFigures[]
                filter region Region
                filter status String optional
                filter total Int from 3
                performer
                  file Queries/FiguresInRegion.cs
              query LiveFigures => observable InvoiceFigures[]
                performer
                  file Queries/LiveFigures.cs
        """;

    protected CompilationResult<SemanticCompilation> _result;

    protected SemanticPerformedQueryShape ShapeOf(string query) =>
        _result.ImplementationRequirements.Single(requirement =>
            requirement.Role == SemanticImplementationRole.QueryPerformer && requirement.Owner.Name == query).QueryShape!;

    void Establish() => _result = Bind(Source);
}
