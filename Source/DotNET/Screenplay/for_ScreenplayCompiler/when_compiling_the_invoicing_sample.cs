// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

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
    [Fact] void should_parse_capture_translations() => Slice("LegacyInvoiceSync").Captures.Single().Map.Single().Translations.Count().ShouldEqual(4);
    [Fact] void should_parse_the_list_projection() => Slice("InvoiceList").Projection!.Blocks.OfType<RemoveWithSyntax>().Single().Event.ShouldEqual("InvoiceCancelled");
    [Fact] void should_parse_the_details_projection_join() => Slice("InvoiceDetails").Projection!.Blocks.OfType<JoinSyntax>().Single().On.ShouldEqual("customerId");
    [Fact] void should_parse_the_details_projection_children() => Slice("InvoiceDetails").Projection!.Blocks.OfType<ChildrenSyntax>().Single().Property.ShouldEqual("lineItems");
    [Fact] void should_parse_the_summary_counters() => Slice("InvoiceDashboard").Projection!.Blocks.OfType<FromSyntax>().First().Mappings.OfType<IncrementMappingSyntax>().Count().ShouldEqual(2);
    [Fact] void should_parse_the_dashboard_screen_layout() => Slice("InvoiceDashboard").Screens.Single().Directives.OfType<ScreenLayoutSyntax>().Single().Slots.Count().ShouldEqual(4);
    [Fact] void should_parse_the_reactors() => Slice("NotifyCustomerOnInvoiceRegistered").Reactors.Single().Triggers.Single().File!.Path.ShouldEqual("Reactors/NotifyCustomerReactor.cs");
    [Fact] void should_parse_the_inline_reactor_code() => Slice("DetectOverdueInvoices").Reactors.Single().Triggers.Single().Code.ShouldNotBeNull();
    [Fact] void should_parse_the_constraints() => Slice("RegisterInvoice").Constraints.Count().ShouldEqual(2);

    CommandSyntax RegisterCommand => Slice("RegisterInvoice").Commands.Single();

    SliceSyntax Slice(string name) => _feature.Slices.Single(_ => _.Name == name);
}
