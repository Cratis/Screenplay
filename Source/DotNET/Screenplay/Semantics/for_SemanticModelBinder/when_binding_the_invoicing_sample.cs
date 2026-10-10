// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.for_ScreenplayCompiler.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

// The invoicing sample is the language's largest example, so it exercises constructs the executable semantic model does not
// admit yet. Each of those is a disposition the sample shows on purpose and is pinned here. Anything else the binder reports
// is the sample drifting from the language, and fails this specification.
public class when_binding_the_invoicing_sample : given.a_semantic_binder
{
    const string Unsupported = DiagnosticCodes.UnsupportedSemanticSyntax;

    static readonly string[] _importedNames = ["CustomerRegistered", "CustomerAccountClosed", "InvoiceShipped", "InvoiceShippingCleared"];

    static readonly (string Code, string Fragment)[] _dispositions =
    [

        // Other applications' events, which this document imports rather than declares, and what refers to them.
        (Unsupported, "Import 'Customers.CustomerRegistered'"),
        (Unsupported, "Import 'Customers.CustomerAccountClosed'"),
        (Unsupported, "Import 'Shipping.InvoiceShipped'"),
        (Unsupported, "Import 'Shipping.InvoiceShippingCleared'"),
        (DiagnosticCodes.InvalidSemanticBinding, "Specification event 'CustomerRegistered' is unresolved"),
        (DiagnosticCodes.InvalidSemanticBinding, "Event type 'CustomerRegistered' not found"),
        (DiagnosticCodes.InvalidSemanticBinding, "Event type 'CustomerAccountClosed' not found"),
        (DiagnosticCodes.InvalidSemanticBinding, "Event type 'InvoiceShipped' not found"),
        (DiagnosticCodes.InvalidSemanticBinding, "Event type 'InvoiceShippingCleared' not found"),

        // Identity, personas and compliance outside the executable model.
        (Unsupported, "Concept 'PersonName' compliance attributes"),
        (Unsupported, "Concept 'BankAccount' compliance attributes"),
        (Unsupported, "Concept 'EmailAddress' compliance attributes"),

        // Code attachments (#139).
        (Unsupported, "Command 'ProcessInvoiceBatch' handler"),
        (Unsupported, "Command 'ArchiveOldInvoices' handler"),
        (Unsupported, "Constraint 'InvoiceStatusTransition' file implementation"),

        // Command details the executable model does not carry.
        (DiagnosticCodes.PreservedLegacySemanticSyntax, "Command 'RegisterInvoice' concurrency metadata"),
        (Unsupported, "Validation rule on 'lines.quantity'"),
        (Unsupported, "Validation rule on 'lines.unitPrice'"),
        (Unsupported, "Validation rule '>' on 'dueDate'"),
        (Unsupported, "Validation rule '<' on 'olderThan'"),
        (Unsupported, "Tag is not admitted: $context tag values"),
        (Unsupported, "$context.tenant has no scalar counterpart"),
        (Unsupported, "$context.causation.type has no scalar counterpart"),
        (Unsupported, "mapping expression 'EnvironmentExpressionSyntax'"),

        // Projection expressions Chronicle cannot resolve.
        (Unsupported, "use '$eventContext.causedBy.name' instead"),
        (Unsupported, "use '$eventContext.causedBy.userName' instead"),
        (Unsupported, "use '$eventContext.causedBy.subject' instead"),
        (Unsupported, "mapping expression 'TemplateExpressionSyntax'"),

        // Query filters, explicit scopes and performer blocks are still outside the admitted executable query shapes.
        (Unsupported, "Query 'ListInvoices' uses filtering"),
        (Unsupported, "Query 'ListLineItems' uses filtering"),
        (Unsupported, "Query 'GetOverdueInvoices' uses filtering"),
        (Unsupported, "Query 'GetInvoiceSummary' uses filtering"),
        (Unsupported, "Query 'GetInvoiceSummary' must"),
        (Unsupported, "Query 'GetSystemActivity' must"),
        (Unsupported, "Read model 'InvoiceListReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'InvoiceLineReportReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'InvoiceSummaryReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'OverdueInvoicesReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'SystemActivityReadModel' must have one unambiguous keyed query"),

        // A clock reaction appends to no event source unless it names one with 'for' (decision 0022).
        (DiagnosticCodes.InvalidSemanticBinding, "Reaction 'OverdueChaser' must say with 'for' which event source")
    ];

    Diagnostic[] _errors;
    Diagnostic[] _incompleteSteps;

    void Because()
    {
        var errors = Bind(Samples.Invoicing).Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

        // The design-mode sample deliberately states partial fixtures. Binding now reports those at each step
        // instead of waiting for the executable model's generic shape failure.
        _incompleteSteps = [.. errors.Where(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationProperty)];
        _errors = [.. errors.Where(diagnostic => diagnostic.Code != DiagnosticCodes.MissingSpecificationProperty)];
    }

    [Fact]
    void should_not_refuse_the_living_samples_declared_identity()
    {
        // Samples.Invoicing is the frozen embedded fixture, not the living language showcase.
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Invoicing/invoicing.play"));
        Bind(source).Diagnostics.Any(diagnostic => diagnostic.Code == Unsupported && diagnostic.Message.StartsWith("Reaction command identity ('runs as')", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact] void should_report_partial_design_fixtures_at_their_steps() => _incompleteSteps.ShouldNotBeEmpty();

    [Fact] void should_report_nothing_but_the_pinned_dispositions() =>
        _errors.Where(error => !_dispositions.Any(_ => Matches(error, _))).Select(Describe).ShouldBeEmpty();
    [Fact] void should_report_every_pinned_disposition_once() =>
        _dispositions.Where(disposition => _errors.Count(_ => Matches(_, disposition)) != 1).Select(_ => $"{_.Code}: {_.Fragment}").ShouldBeEmpty();
    [Fact] void should_leave_only_imported_names_unresolved() =>
        _errors.Where(_ => _.Code == DiagnosticCodes.InvalidSemanticBinding && !_importedNames.Any(name => _.Message.Contains($"'{name}'", StringComparison.Ordinal)) &&
            !_dispositions.Any(disposition => Matches(_, disposition))).Select(Describe).ShouldBeEmpty();
    [Fact] void should_report_no_warnings() => Bind(Samples.Invoicing).Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Warning).Select(Describe).ShouldBeEmpty();
    [Fact] void should_report_personas_as_information() => Bind(Samples.Invoicing).Diagnostics.Where(_ => _.Message.StartsWith("Persona '", StringComparison.Ordinal)).Select(_ => _.Severity).ShouldContainOnly(DiagnosticSeverity.Information, DiagnosticSeverity.Information);

    static bool Matches(Diagnostic error, (string Code, string Fragment) disposition) =>
        error.Code == disposition.Code && error.Message.Contains(disposition.Fragment, StringComparison.Ordinal);

    static string Describe(Diagnostic diagnostic) => $"{diagnostic.Code} line {diagnostic.Location.Line}: {diagnostic.Message}";
}
