// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_context_sourced_performed_query_arguments : given.a_semantic_binder
{
    const string Declarations =
        """
        module Billing
          feature Invoices
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String
                name String
              query NamedFigures => InvoiceFigures[]
        """;

    [Theory]
    [InlineData("by name String from $context.identity", 8)]
    [InlineData("by\n          name String from $context.identity", 9)]
    [InlineData("filter name String from $context.identity", 8)]
    void should_refuse_the_context_sourced_argument_instead_of_dropping_it(string argument, int line)
    {
        var result = Bind(Declarations + "\n        " + argument + "\n        performer\n          file Queries/NamedFigures.cs");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ContextSourcedPerformedQueryArgument);
        result.Success.ShouldBeFalse();
        diagnostic.Code.ShouldEqual("PLAY0810");
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldEqual("Query 'NamedFigures' argument 'name' is sourced from context, which is not supported for performer queries; use a caller-supplied argument instead.");
        diagnostic.Location.Line.ShouldEqual(line);
        diagnostic.Arguments["query"].ShouldEqual(DiagnosticArgument.Text("NamedFigures"));
        diagnostic.Arguments["argument"].ShouldEqual(DiagnosticArgument.Text("name"));
        result.ImplementationRequirements.Any(requirement => requirement.Role == SemanticImplementationRole.QueryPerformer).ShouldBeFalse();
    }

    [Fact]
    void should_keep_the_refused_query_opaque_to_specifications()
    {
        var result = Bind(Declarations + "\n" +
            """
                    by name String from $context.identity
                    performer
                      file Queries/NamedFigures.cs
                  specification ReadingFigures
                    then query NamedFigures
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ContextSourcedPerformedQueryArgument).ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldContain("Specification 'ReadingFigures' reads query 'NamedFigures', which code performs, so it is not an executable specification.");
    }
}
