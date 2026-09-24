// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
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

        // Identity, personas, compliance and occurrences outside the executable model.
        (Unsupported, "Persona 'Accountant'"),
        (Unsupported, "Persona 'InvoiceManager'"),
        (Unsupported, "Trigger 'DirectoryChanged'"),
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

        // Queries beyond one optional instance by key (#140), and the read models only such queries identify.
        (Unsupported, "Query 'ListInvoices' uses delivery"),
        (Unsupported, "Query 'ListInvoices' must declare one caller-supplied 'by' argument"),
        (Unsupported, "Query 'ListLineItems' uses delivery"),
        (Unsupported, "Query 'ListLineItems' must declare one caller-supplied 'by' argument"),
        (Unsupported, "Query 'ListCancelledInvoices' must declare one caller-supplied 'by' argument"),
        (Unsupported, "Query 'GetInvoiceSummary' uses delivery"),
        (Unsupported, "Query 'GetInvoiceSummary' must declare one caller-supplied 'by' argument"),
        (Unsupported, "Query 'GetOverdueInvoices' uses delivery"),
        (Unsupported, "Query 'GetOverdueInvoices' must declare one caller-supplied 'by' argument"),
        (Unsupported, "Query 'GetSystemActivity' must declare one caller-supplied 'by' argument"),
        (Unsupported, "Read model 'InvoiceListReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'InvoiceLineReportReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'CancelledInvoiceReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'InvoiceSummaryReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'OverdueInvoicesReadModel' must have one unambiguous keyed query"),
        (Unsupported, "Read model 'SystemActivityReadModel' must have one unambiguous keyed query"),

        // Translation and automation slices.
        (Unsupported, "Slice 'LegacyInvoiceSync' of type 'Translate'"),
        (Unsupported, "Capture 'LegacyInvoiceCapture'"),
        (Unsupported, "Slice 'NotifyCustomerOnInvoiceRegistered' of type 'Automation'"),
        (Unsupported, "Reaction 'NotifyCustomer' requires"),
        (Unsupported, "Slice 'DetectOverdueInvoices' of type 'Automation'"),
        (Unsupported, "Reaction 'OverdueInvoiceDetector' requires"),
        (Unsupported, "Slice 'ReconcilePayments' of type 'Automation'"),
        (Unsupported, "Reaction 'PaymentReconciler' requires"),
        (Unsupported, "Slice 'ChaseOverdueInvoices' of type 'Automation'"),
        (Unsupported, "Reaction 'OverdueChaser' requires"),
        (Unsupported, "Slice 'SyncBillingDirectory' of type 'Automation'"),
        (Unsupported, "Reaction 'BillingDirectorySync' requires")
    ];

    Diagnostic[] _errors;

    void Because() => _errors = [.. Bind(Samples.Invoicing).Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error)];

    [Fact] void should_report_nothing_but_the_pinned_dispositions() =>
        _errors.Where(error => !_dispositions.Any(_ => Matches(error, _))).Select(Describe).ShouldBeEmpty();
    [Fact] void should_report_every_pinned_disposition_once() =>
        _dispositions.Where(disposition => _errors.Count(_ => Matches(_, disposition)) != 1).Select(_ => $"{_.Code}: {_.Fragment}").ShouldBeEmpty();
    [Fact] void should_leave_only_imported_names_unresolved() =>
        _errors.Where(_ => _.Code == DiagnosticCodes.InvalidSemanticBinding && !_importedNames.Any(name => _.Message.Contains($"'{name}'", StringComparison.Ordinal))).Select(Describe).ShouldBeEmpty();
    [Fact] void should_report_no_warnings() => Bind(Samples.Invoicing).Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Warning).Select(Describe).ShouldBeEmpty();

    static bool Matches(Diagnostic error, (string Code, string Fragment) disposition) =>
        error.Code == disposition.Code && error.Message.Contains(disposition.Fragment, StringComparison.Ordinal);

    static string Describe(Diagnostic diagnostic) => $"{diagnostic.Code} line {diagnostic.Location.Line}: {diagnostic.Message}";
}
