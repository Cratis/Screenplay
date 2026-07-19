// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_standalone_capture : given.a_compiler
{
    const string Source =
        """
        # A capture using the full feature set of the sub-language
        capture LegacyInvoiceCapture
          source webhook
            path /invoices
          key id
          map
            status = status translate
              "utkast" => draft
              "sendt"  => sent
            split fullName by " "
              firstName
              lastName
            summary = `${status} invoice`
          append InvoiceStatusChanged
            when status
              invoiceId = $.id
              status    = $.status
          append InvoicePaidFromSent
            when status from "sent" to "paid"
              invoiceId = $.id
          append InvoiceAttentionRequired
            when status or note
              invoiceId = $.id
          append InvoiceFullyApproved
            when approvedByManager and approvedByFinance
              invoiceId = $.id
          append InvoiceFlagged
            when `status == "sent" && overdue == true`
              invoiceId = $.id
          children lineItems identified by lineNumber
            map
              productName = name
            append InvoiceLineItemAdded
              when added
                invoiceId  = $.id
                lineNumber = $.lineNumber
            append InvoiceLineItemRemoved
              when removed
                invoiceId  = $.id
                lineNumber = $.lineNumber
          nested billingContact
            map
              contactName = name
            append BillingContactUpdated
              when email
                invoiceId = $.id
                email     = $.email
        """;

    CompilationResult<CaptureSyntax> _result;

    void Because() => _result = _compiler.CompileCapture(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_name() => _result.Value!.Name.ShouldEqual("LegacyInvoiceCapture");
    [Fact] void should_have_the_webhook_source_kind() => _result.Value!.Source!.Kind.ShouldEqual("webhook");
    [Fact] void should_have_the_webhook_path_setting() => _result.Value!.Source!.Settings.Single(_ => _.Name == "path").Value.ShouldEqual("/invoices");
    [Fact] void should_have_the_key() => _result.Value!.Key.ShouldEqual("id");
    [Fact] void should_parse_the_translate_map_entry() => MapEntry("status").Translations.Count().ShouldEqual(2);
    [Fact] void should_parse_the_map_entry_source_as_a_path() => ((PathExpressionSyntax)MapEntry("status").Source).Path.ShouldEqual("status");
    [Fact] void should_parse_the_split_source() => ((PathExpressionSyntax)Split.Source).Path.ShouldEqual("fullName");
    [Fact] void should_parse_the_split_separator() => Split.Separator.ShouldEqual(" ");
    [Fact] void should_parse_the_split_targets() => Split.Targets.ShouldContainOnly("firstName", "lastName");
    [Fact] void should_parse_the_template_map_entry() => MapEntry("summary").Source.ShouldBeOfExactType<TemplateExpressionSyntax>();
    [Fact] void should_parse_the_property_changed_when_kind() => Append("InvoiceStatusChanged").When!.Kind.ShouldEqual(CaptureWhenKind.PropertyChanged);
    [Fact] void should_parse_the_property_changed_property() => Append("InvoiceStatusChanged").When!.Properties.Single().ShouldEqual("status");
    [Fact] void should_parse_the_value_transition_when_kind() => Append("InvoicePaidFromSent").When!.Kind.ShouldEqual(CaptureWhenKind.ValueTransition);
    [Fact] void should_parse_the_value_transition_from() => Append("InvoicePaidFromSent").When!.FromValue.ShouldEqual("sent");
    [Fact] void should_parse_the_value_transition_to() => Append("InvoicePaidFromSent").When!.ToValue.ShouldEqual("paid");
    [Fact] void should_parse_the_logical_or_when_kind() => Append("InvoiceAttentionRequired").When!.Kind.ShouldEqual(CaptureWhenKind.LogicalOr);
    [Fact] void should_parse_the_logical_or_properties() => Append("InvoiceAttentionRequired").When!.Properties.ShouldContainOnly("status", "note");
    [Fact] void should_parse_the_logical_and_when_kind() => Append("InvoiceFullyApproved").When!.Kind.ShouldEqual(CaptureWhenKind.LogicalAnd);
    [Fact] void should_parse_the_logical_and_properties() => Append("InvoiceFullyApproved").When!.Properties.ShouldContainOnly("approvedByManager", "approvedByFinance");
    [Fact] void should_parse_the_expression_when_kind() => Append("InvoiceFlagged").When!.Kind.ShouldEqual(CaptureWhenKind.Expression);
    [Fact] void should_parse_the_expression_when_text() => Append("InvoiceFlagged").When!.Expression.ShouldEqual("`status == \"sent\" && overdue == true`");
    [Fact] void should_parse_the_added_when() => ChildAppend("InvoiceLineItemAdded").When!.Kind.ShouldEqual(CaptureWhenKind.Added);
    [Fact] void should_parse_the_removed_when() => ChildAppend("InvoiceLineItemRemoved").When!.Kind.ShouldEqual(CaptureWhenKind.Removed);
    [Fact] void should_parse_the_children_map() => Children.Map.OfType<CaptureMapEntrySyntax>().Single().Property.ShouldEqual("productName");
    [Fact] void should_parse_the_nested_property() => Nested.Property.ShouldEqual("billingContact");
    [Fact] void should_parse_the_nested_map() => Nested.Map.OfType<CaptureMapEntrySyntax>().Single().Property.ShouldEqual("contactName");
    [Fact] void should_parse_the_nested_append() => Nested.Appends.Single().Event.ShouldEqual("BillingContactUpdated");

    CaptureSplitSyntax Split => _result.Value!.Map.OfType<CaptureSplitSyntax>().Single();

    CaptureChildrenSyntax Children => _result.Value!.Children.Single();

    CaptureNestedSyntax Nested => _result.Value!.Nested.Single();

    CaptureMapEntrySyntax MapEntry(string property) =>
        _result.Value!.Map.OfType<CaptureMapEntrySyntax>().Single(_ => _.Property == property);

    CaptureAppendSyntax Append(string eventType) => _result.Value!.Appends.Single(_ => _.Event == eventType);

    CaptureAppendSyntax ChildAppend(string eventType) => Children.Appends.Single(_ => _.Event == eventType);
}
