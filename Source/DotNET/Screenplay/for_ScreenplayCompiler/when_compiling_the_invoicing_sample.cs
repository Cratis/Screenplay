// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_the_invoicing_sample : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    FeatureSyntax _feature;

    void Because()
    {
        _result = _compiler.Compile(given.Samples.Invoicing);
        _feature = _result.Value!.Modules.Single().Features.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_domain() => _result.Value!.Domain!.Name.ShouldEqual("Sales");
    [Fact] void should_have_all_imports() => _result.Value!.Imports.Count().ShouldEqual(4);
    [Fact] void should_have_the_customer_import() => _result.Value!.Imports.Select(_ => _.Name).ShouldContain("CustomerRegistered");
    [Fact] void should_have_all_concepts() => _result.Value!.Concepts.Count().ShouldEqual(12);
    [Fact] void should_have_the_enum_concept_values() => _result.Value!.Concepts.Single(_ => _.Name == "InvoiceStatus").Values.Count().ShouldEqual(5);
    [Fact] void should_capture_pii_attributes() => _result.Value!.Concepts.Single(_ => _.Name == "EmailAddress").Attributes.ShouldContain("pii");
    [Fact] void should_parse_the_concept_validation_rules() => EmailConceptRules.Count().ShouldEqual(2);
    [Fact] void should_imply_the_concept_value_subject() => EmailConceptRules.All(_ => _.Property == ValidationRuleSyntax.ConceptValue).ShouldBeTrue();
    [Fact] void should_have_all_policies() => _result.Value!.Policies.Count().ShouldEqual(7);
    [Fact] void should_have_the_sensitive_concept_attributes() => _result.Value!.Concepts.Single(_ => _.Name == "BankAccount").Attributes.ShouldContainOnly("pii", "sensitive");
    [Fact] void should_parse_the_code_based_policy() => _result.Value!.Policies.Single(_ => _.Name == "IsAdultCustomer").Code.ShouldNotBeNull();
    [Fact] void should_have_both_personas() => _result.Value!.Personas!.Count().ShouldEqual(2);
    [Fact] void should_have_the_seed_block() => _result.Value!.Seeds!.Single().Groups.Count().ShouldEqual(2);
    [Fact] void should_parse_the_seeded_events_of_the_first_group() => _result.Value!.Seeds!.Single().Groups.First().Events.Select(_ => _.Event).ShouldContainOnly("CustomerRegistered", "InvoiceRegistered");
    [Fact] void should_parse_the_seeded_event_properties() => _result.Value!.Seeds!.Single().Groups.First().Events.First().Properties.Count().ShouldEqual(2);
    [Fact] void should_have_the_accountant_persona_description() => _result.Value!.Personas!.First().Description.ShouldEqual("Keeps the books and approves invoices");
    [Fact] void should_have_the_accountant_persona_policies() => _result.Value!.Personas!.First().Policies.ShouldContainOnly("IsAccountant", "CanManageInvoice");
    [Fact] void should_have_the_invoicing_module() => _result.Value!.Modules.Single().Name.ShouldEqual("Invoicing");
    [Fact] void should_have_the_module_description() => _result.Value!.Modules.Single().Description.ShouldEqual("Everything related to invoicing customers.\nRegistration, lifecycle and payment tracking of invoices.");
    [Fact] void should_have_the_feature_description() => _feature.Description.ShouldEqual("Registering and managing the lifecycle of invoices");
    [Fact] void should_have_the_slice_description() => Slice("RegisterInvoice").Description.ShouldEqual("Registers a new invoice");
    [Fact] void should_have_the_command_description() => RegisterCommand.Description.ShouldEqual("Registers a new invoice with its lines and payment terms");
    [Fact] void should_have_both_layouts() => _result.Value!.Modules.Single().Layouts.Count().ShouldEqual(2);
    [Fact] void should_have_the_master_detail_slots() => _result.Value!.Modules.Single().Layouts.First().Slots.ShouldContainOnly("sidebar", "main");
    [Fact] void should_have_all_slices() => _feature.Slices.Count().ShouldEqual(14);
    [Fact] void should_have_the_fully_auto_mapped_projection() => Slice("CancelledInvoices").Projection!.Blocks.OfType<FromSyntax>().Single().Mappings.ShouldBeEmpty();
    [Fact] void should_have_the_nested_feature() => _feature.Features.Single().Name.ShouldEqual("Adjustments");
    [Fact] void should_have_the_nested_feature_slices() => _feature.Features.Single().Slices.Select(_ => _.Name).ShouldContainOnly("ApplyDiscount", "WriteOffInvoice");
    [Fact] void should_parse_conditional_produces() => RegisterCommand.Produces.Count(_ => _.When is not null).ShouldEqual(4);
    [Fact] void should_parse_unconditional_produces_mappings() => RegisterCommand.Produces.First().Mappings.Count().ShouldEqual(11);
    [Fact] void should_parse_the_produces_tag() => ((LiteralExpressionSyntax)RegisterCommand.Produces.First().Tags!.Single().Value).Value.ShouldEqual("audit");
    [Fact] void should_parse_the_event_tags() => RegisteredEvent.Tags!.Count().ShouldEqual(3);
    [Fact] void should_parse_the_static_event_tags() => RegisteredEvent.Tags!.Take(2).Select(_ => ((LiteralExpressionSyntax)_.Value).Value).ShouldContainOnly("invoicing", "billing");
    [Fact] void should_parse_the_dynamic_event_tag() => ((ContextExpressionSyntax)RegisteredEvent.Tags!.Last().Value).Path.ShouldEqual("identity.id");
    [Fact] void should_parse_the_validation_rules() => RegisterCommand.Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Count().ShouldEqual(7);
    [Fact] void should_parse_the_code_validation() => RegisterCommand.Validations.OfType<CodeValidateSyntax>().Count().ShouldEqual(1);
    [Fact] void should_parse_the_authorize_policies() => RegisterCommand.Authorize!.Policies.Select(_ => _.Name).ShouldContainOnly("CanManageInvoice", "IsAdultCustomer");
    [Fact] void should_parse_the_concurrency_event_source() => RegisterCommand.Concurrency!.EventSource.ShouldBeTrue();
    [Fact] void should_parse_the_concurrency_source_type() => RegisterCommand.Concurrency!.EventSourceType.ShouldEqual("Invoice");
    [Fact] void should_parse_the_concurrency_stream_type() => RegisterCommand.Concurrency!.EventStreamType.ShouldEqual("Invoicing");
    [Fact] void should_parse_the_concurrency_stream_id() => RegisterCommand.Concurrency!.EventStreamId.ShouldEqual("Primary");
    [Fact] void should_parse_the_concurrency_events() => RegisterCommand.Concurrency!.EventTypes.ShouldContainOnly("InvoiceRegistered", "InvoiceCancelled");
    [Fact] void should_parse_the_batch_handler() => Slice("ProcessInvoiceBatch").Commands.Single().Handler!.Code.ShouldNotBeNull();
    [Fact] void should_parse_the_file_handler() => Slice("ArchiveOldInvoices").Commands.Single().Handler!.File!.Path.ShouldEqual("Handlers/ArchiveOldInvoicesHandler.cs");
    [Fact] void should_parse_the_capture() => Slice("LegacyInvoiceSync").Captures.Single().Children.Single().Appends.Count().ShouldEqual(2);
    [Fact] void should_parse_the_capture_append_tag() => ((LiteralExpressionSyntax)Capture.Appends.First(_ => _.Event == "InvoiceStatusChanged").Tags!.Single().Value).Value.ShouldEqual("legacy");
    [Fact] void should_parse_capture_translations() => Capture.Map.OfType<CaptureMapEntrySyntax>().Single(_ => _.Property == "status").Translations.Count().ShouldEqual(4);
    [Fact] void should_parse_the_capture_template_map_entry() => Capture.Map.OfType<CaptureMapEntrySyntax>().Single(_ => _.Property == "summary").Source.ShouldBeOfExactType<TemplateExpressionSyntax>();
    [Fact] void should_parse_the_capture_logical_or_when() => Capture.Appends.Single(_ => _.Event == "InvoiceNeedsAttention").When!.Kind.ShouldEqual(CaptureWhenKind.LogicalOr);
    [Fact] void should_parse_the_capture_logical_and_when() => Capture.Appends.Single(_ => _.Event == "InvoiceFullyApproved").When!.Kind.ShouldEqual(CaptureWhenKind.LogicalAnd);
    [Fact] void should_parse_the_capture_expression_when() => Capture.Appends.Single(_ => _.Event == "InvoiceFlagged").When!.Kind.ShouldEqual(CaptureWhenKind.Expression);
    [Fact] void should_parse_the_capture_split() => Capture.Map.OfType<CaptureSplitSyntax>().Single().Targets.ShouldContainOnly("firstName", "lastName");
    [Fact] void should_parse_the_capture_value_transition_when() => ValueTransitionWhen.Kind.ShouldEqual(CaptureWhenKind.ValueTransition);
    [Fact] void should_parse_the_capture_value_transition_from() => ValueTransitionWhen.FromValue.ShouldEqual("sent");
    [Fact] void should_parse_the_capture_value_transition_to() => ValueTransitionWhen.ToValue.ShouldEqual("paid");
    [Fact] void should_parse_the_capture_children_map() => Capture.Children.Single().Map.OfType<CaptureMapEntrySyntax>().Single().Property.ShouldEqual("productName");
    [Fact] void should_parse_the_capture_nested_block() => Capture.Nested.Single().Property.ShouldEqual("billingContact");
    [Fact] void should_parse_the_capture_nested_map() => Capture.Nested.Single().Map.OfType<CaptureMapEntrySyntax>().Single().Property.ShouldEqual("contactName");
    [Fact] void should_parse_the_capture_nested_append() => Capture.Nested.Single().Appends.Single().Event.ShouldEqual("BillingContactUpdated");

    CaptureSyntax Capture => Slice("LegacyInvoiceSync").Captures.Single();

    CaptureWhenSyntax ValueTransitionWhen => Capture.Appends.Single(_ => _.Event == "InvoicePaidFromSent").When!;
    [Fact] void should_parse_the_list_projection() => Slice("InvoiceList").Projection!.Blocks.OfType<RemoveWithSyntax>().Single().Event.ShouldEqual("InvoiceCancelled");
    [Fact] void should_parse_the_details_projection_join() => Slice("InvoiceDetails").Projection!.Blocks.OfType<JoinSyntax>().Single().On.ShouldEqual("customerId");
    [Fact] void should_parse_the_details_projection_children() => Slice("InvoiceDetails").Projection!.Blocks.OfType<ChildrenSyntax>().Single().Property.ShouldEqual("lineItems");
    [Fact] void should_parse_the_details_projection_every() => Slice("InvoiceDetails").Projection!.Blocks.OfType<EverySyntax>().Single().IncludeChildren.ShouldBeFalse();
    [Fact] void should_parse_the_details_projection_remove_via_join() => Slice("InvoiceDetails").Projection!.Blocks.OfType<RemoveViaJoinSyntax>().Single().Event.ShouldEqual("CustomerAccountClosed");
    [Fact] void should_parse_the_details_projection_nested() => Slice("InvoiceDetails").Projection!.Blocks.OfType<NestedSyntax>().Single().Property.ShouldEqual("shipping");
    [Fact] void should_parse_the_line_report_composite_key() => ((CompositeKeySyntax)Slice("InvoiceLineReport").Projection!.Blocks.OfType<FromSyntax>().Single().Key!).Type.ShouldEqual("InvoiceLineKey");
    [Fact] void should_parse_the_summary_counters() => Slice("InvoiceDashboard").Projection!.Blocks.OfType<FromSyntax>().First().Mappings.OfType<IncrementMappingSyntax>().Count().ShouldEqual(2);
    [Fact] void should_parse_the_dashboard_screen_layout() => Slice("InvoiceDashboard").Screens.Single().Directives.OfType<ScreenLayoutSyntax>().Single().Slots.Count().ShouldEqual(4);
    [Fact] void should_parse_the_reactors() => Slice("NotifyCustomerOnInvoiceRegistered").Reactors.Single().Triggers.Single().File!.Path.ShouldEqual("Reactors/NotifyCustomerReactor.cs");
    [Fact] void should_parse_the_inline_reactor_code() => Slice("DetectOverdueInvoices").Reactors.Single().Triggers.First().Code.ShouldNotBeNull();
    [Fact] void should_parse_the_multiple_reactor_triggers() => Slice("DetectOverdueInvoices").Reactors.Single().Triggers.Select(_ => _.Event).ShouldContainOnly("InvoiceRegistered", "InvoiceSent");
    [Fact] void should_parse_the_constraints() => Slice("RegisterInvoice").Constraints.Count().ShouldEqual(2);
    [Fact] void should_parse_the_specifications() => Slice("RegisterInvoice").Specifications.Count().ShouldEqual(2);
    [Fact] void should_parse_the_given_of_the_first_specification() => FirstSpecification.Given.Single().EventType.ShouldEqual("CustomerRegistered");
    [Fact] void should_parse_the_when_of_the_first_specification() => FirstSpecification.When!.CommandType.ShouldEqual("RegisterInvoice");
    [Fact] void should_parse_the_then_of_the_first_specification() => FirstSpecification.ThenEvents.Single().EventType.ShouldEqual("InvoiceRegistered");
    [Fact] void should_parse_the_then_error_of_the_second_specification() => SecondSpecification.ThenErrors.Single().Name.ShouldEqual("An invoice must have at least one line");
    [Fact] void should_parse_no_given_on_the_second_specification() => SecondSpecification.Given.ShouldBeEmpty();
    [Fact] void should_parse_the_given_readmodel_of_the_status_specification() => StatusSpecification.GivenReadModels!.Single().Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_parse_the_given_readmodel_properties() => StatusSpecification.GivenReadModels!.Single().Properties.Count().ShouldEqual(2);
    [Fact] void should_parse_the_then_readmodel_of_the_status_specification() => StatusSpecification.ThenReadModels!.Single().Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_parse_the_then_readmodel_properties() => StatusSpecification.ThenReadModels!.Single().Properties.Single().Property.ShouldEqual("status");

    IEnumerable<ValidationRuleSyntax> EmailConceptRules => _result.Value!.Concepts.Single(_ => _.Name == "EmailAddress")
        .Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules;

    CommandSyntax RegisterCommand => Slice("RegisterInvoice").Commands.Single();

    EventSyntax RegisteredEvent => Slice("RegisterInvoice").Events.Single(_ => _.Name == "InvoiceRegistered");

    SpecificationSyntax FirstSpecification => Slice("RegisterInvoice").Specifications.First();

    SpecificationSyntax SecondSpecification => Slice("RegisterInvoice").Specifications.Last();

    SpecificationSyntax StatusSpecification => Slice("ChangeInvoiceStatus").Specifications.Single();

    SliceSyntax Slice(string name) => _feature.Slices.Single(_ => _.Name == name);
}
