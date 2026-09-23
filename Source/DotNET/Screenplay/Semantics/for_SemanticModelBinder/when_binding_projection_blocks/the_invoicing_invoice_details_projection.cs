// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_ScreenplayCompiler.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// The InvoiceDetails projection of the invoicing sample, bound verbatim against declarations for everything it names. Every block
// it uses - key, every/exclude children, multi-mapping from, join, children with parent keys, add/count, nested, clear with,
// remove with, remove via join - is admitted; the only errors left are the expressions ESM v1 cannot execute: '$causedBy'
// (Cratis/Chronicle#4119) and a string template. With those spelled as Chronicle executes them, the projection binds.
public class the_invoicing_invoice_details_projection : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Declarations =
        """
        concept InvoiceId : Uuid
        concept CustomerId : Uuid
        type InvoiceItem
          lineNumber Int
          quantity Int
          unitPrice Decimal
          subtotal Decimal?
          itemCount Int?
        type Shipment
          carrier String
        module Invoicing
          feature Invoices
            slice StateChange Events
              event InvoiceRegistered
                invoiceId InvoiceId
                customerId CustomerId
                invoiceNumber String
                currency String
                paymentTerms Int
                dueDate Date
              event InvoiceSent
                sentAt DateTime
              event InvoicePaid
                paidAt DateTime
              event InvoiceMarkedOverdue
                overdueAt DateTime
              event CustomerRegistered
                name String
              event InvoiceLineItemAdded
                invoiceId InvoiceId
                lineNumber Int
                quantity Int
                unitPrice Decimal
              event InvoiceLineItemRemoved
                invoiceId InvoiceId
                lineNumber Int
              event InvoiceShipped
                carrier String
              event InvoiceShippingCleared
                invoiceId InvoiceId
              event CustomerAccountClosed
                customerId CustomerId
            slice StateView InvoiceDetails
              readmodel InvoiceDetailsReadModel
                invoiceId InvoiceId
                customerId CustomerId
                invoiceNumber String
                currency String
                paymentTerms Int
                status String
                dueDate Date
                registeredAt DateTime?
                registeredBy String?
                registeredByUser String?
                registeredForSubject String?
                displayLabel String?
                sentAt DateTime?
                paidAt DateTime?
                overdueAt DateTime?
                customerName String?
                lineItems InvoiceItem[]
                shipping Shipment?
                lastUpdatedAt DateTime?
              query GetInvoice => InvoiceDetailsReadModel?
                by invoiceId InvoiceId

        """;

    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _executable;
    string _projection;

    void Establish()
    {
        var lines = Samples.Invoicing.Split('\n');
        var start = Array.FindIndex(lines, _ => _.TrimStart().StartsWith("projection InvoiceDetails =>", StringComparison.Ordinal));
        var end = Array.FindIndex(lines, start + 1, _ => _.TrimStart().StartsWith("screen ", StringComparison.Ordinal));
        _projection = string.Join('\n', lines[start..end]).TrimEnd();
    }

    void Because()
    {
        _result = Bind(Declarations + _projection + "\n");
        var executable = _projection
            .Replace("$causedBy.name", "$eventContext.causedBy.name", StringComparison.Ordinal)
            .Replace("$causedBy.userName", "$eventContext.causedBy.userName", StringComparison.Ordinal)
            .Replace("$causedBy.subject", "$eventContext.causedBy.subject", StringComparison.Ordinal)
            .Replace("`${invoiceNumber} (${currency})`", "invoiceNumber", StringComparison.Ordinal);
        _executable = Bind(Declarations + executable + "\n");
    }

    [Fact] void should_report_no_block_admission_error() =>
        Errors(_result).Any(_ => _.Message.Contains("Projection block", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_only_report_the_expressions_esm_v1_cannot_execute() =>
        Errors(_result).All(_ => _.Message.Contains("$causedBy", StringComparison.Ordinal) || _.Message.Contains("TemplateExpressionSyntax", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_each_caused_by_and_the_template() => Errors(_result).Count().ShouldEqual(4);
    [Fact] void should_bind_the_executable_spelling() => _executable.Success.ShouldBeTrue();
    [Fact] void should_bind_one_transition_per_from_event() => Scope.From.Length.ShouldEqual(4);
    [Fact] void should_bind_the_join() => Scope.Joins.Length.ShouldEqual(1);
    [Fact] void should_bind_the_children() => Scope.Children.Single().Scope.Removals.Length.ShouldEqual(1);
    [Fact] void should_bind_the_nested_object() => Scope.Nested.Single().Scope.Removals.Length.ShouldEqual(1);
    [Fact] void should_bind_the_join_removal() => Scope.JoinRemovals.Length.ShouldEqual(1);
    [Fact] void should_exclude_children_from_every() => Scope.Every!.IncludeChildren.ShouldBeFalse();

    SemanticProjectionScope Scope => _executable.Value!.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Projections).Single().Scope!;

    static IEnumerable<Diagnostic> Errors(CompilationResult<SemanticCompilation> result) => result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error);
}
