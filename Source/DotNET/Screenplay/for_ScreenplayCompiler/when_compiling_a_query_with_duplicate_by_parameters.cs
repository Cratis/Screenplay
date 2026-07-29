// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_query_with_duplicate_by_parameters : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateView InvoiceList
              query GetInvoice => InvoiceDetailsReadModel
                by invoiceId InvoiceId
                by customerId CustomerId
        """;

    CompilationResult<ApplicationSyntax> _result;
    QuerySyntax _query;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _query = _result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
    }

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_point_at_the_second_parameter() => _result.Diagnostics.Single().Location.Line.ShouldEqual(6);
    [Fact] void should_keep_the_first_parameter() => _query.By!.Name.ShouldEqual("invoiceId");
}
