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
    [Fact] void should_have_the_import() => _result.Value!.Imports.Single().Name.ShouldEqual("CustomerRegistered");
    [Fact] void should_have_all_concepts() => _result.Value!.Concepts.Count().ShouldEqual(10);
    [Fact] void should_have_the_enum_concept_values() => _result.Value!.Concepts.Single(_ => _.Name == "InvoiceStatus").Values.Count().ShouldEqual(5);
    [Fact] void should_capture_pii_attributes() => _result.Value!.Concepts.Single(_ => _.Name == "EmailAddress").Attributes.ShouldContain("pii");
    [Fact] void should_have_all_policies() => _result.Value!.Policies.Count().ShouldEqual(4);
    [Fact] void should_parse_the_code_based_policy() => _result.Value!.Policies.Single(_ => _.Name == "IsAdultCustomer").Code.ShouldNotBeNull();
    [Fact] void should_have_the_invoicing_module() => _result.Value!.Modules.Single().Name.ShouldEqual("Invoicing");
    [Fact] void should_have_the_module_description() => _result.Value!.Modules.Single().Description.ShouldEqual("Everything related to invoicing customers");
    [Fact] void should_have_the_feature_description() => _feature.Description.ShouldEqual("Registering and managing the lifecycle of invoices");
    [Fact] void should_have_the_slice_description() => Slice("RegisterInvoice").Description.ShouldEqual("Registers a new invoice");
    [Fact] void should_have_both_layouts() => _result.Value!.Modules.Single().Layouts.Count().ShouldEqual(2);
    [Fact] void should_have_the_master_detail_slots() => _result.Value!.Modules.Single().Layouts.First().Slots.ShouldContainOnly("sidebar", "main");
    [Fact] void should_have_all_slices() => _feature.Slices.Count().ShouldEqual(10);
    [Fact] void should_parse_conditional_produces() => RegisterCommand.Produces.Count(_ => _.When is not null).ShouldEqual(4);
    [Fact] void should_parse_unconditional_produces_mappings() => RegisterCommand.Produces.First().Mappings.Count().ShouldEqual(11);
    [Fact] void should_parse_the_validation_rules() => RegisterCommand.Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Count().ShouldEqual(6);
    [Fact] void should_parse_the_code_validation() => RegisterCommand.Validations.OfType<CodeValidateSyntax>().Count().ShouldEqual(1);
    [Fact] void should_parse_the_authorize_policies() => RegisterCommand.Authorize!.Policies.Select(_ => _.Name).ShouldContainOnly("CanManageInvoice", "IsAdultCustomer");
    [Fact] void should_parse_the_batch_handler() => Slice("ProcessInvoiceBatch").Commands.Single().Handler!.Code.ShouldNotBeNull();
    [Fact] void should_parse_the_capture() => Slice("LegacyInvoiceSync").Captures.Single().Children.Single().Appends.Count().ShouldEqual(2);
    [Fact] void should_parse_capture_translations() => Slice("LegacyInvoiceSync").Captures.Single().Map.OfType<CaptureMapEntrySyntax>().Single().Translations.Count().ShouldEqual(4);
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
    [Fact] void should_parse_the_summary_counters() => Slice("InvoiceDashboard").Projection!.Blocks.OfType<FromSyntax>().First().Mappings.OfType<IncrementMappingSyntax>().Count().ShouldEqual(2);
    [Fact] void should_parse_the_dashboard_screen_layout() => Slice("InvoiceDashboard").Screens.Single().Directives.OfType<ScreenLayoutSyntax>().Single().Slots.Count().ShouldEqual(4);
    [Fact] void should_parse_the_reactors() => Slice("NotifyCustomerOnInvoiceRegistered").Reactors.Single().Triggers.Single().File!.Path.ShouldEqual("Reactors/NotifyCustomerReactor.cs");
    [Fact] void should_parse_the_inline_reactor_code() => Slice("DetectOverdueInvoices").Reactors.Single().Triggers.Single().Code.ShouldNotBeNull();
    [Fact] void should_parse_the_constraints() => Slice("RegisterInvoice").Constraints.Count().ShouldEqual(2);
    [Fact] void should_parse_the_specifications() => Slice("RegisterInvoice").Specifications.Count().ShouldEqual(2);
    [Fact] void should_parse_the_given_of_the_first_specification() => FirstSpecification.Given.Single().EventType.ShouldEqual("CustomerRegistered");
    [Fact] void should_parse_the_when_of_the_first_specification() => FirstSpecification.When!.CommandType.ShouldEqual("RegisterInvoice");
    [Fact] void should_parse_the_then_of_the_first_specification() => FirstSpecification.ThenEvents.Single().EventType.ShouldEqual("InvoiceRegistered");
    [Fact] void should_parse_the_then_error_of_the_second_specification() => SecondSpecification.ThenErrors.Single().Name.ShouldEqual("An invoice must have at least one line");
    [Fact] void should_parse_no_given_on_the_second_specification() => SecondSpecification.Given.ShouldBeEmpty();

    CommandSyntax RegisterCommand => Slice("RegisterInvoice").Commands.Single();

    SpecificationSyntax FirstSpecification => Slice("RegisterInvoice").Specifications.First();

    SpecificationSyntax SecondSpecification => Slice("RegisterInvoice").Specifications.Last();

    SliceSyntax Slice(string name) => _feature.Slices.Single(_ => _.Name == name);
}
