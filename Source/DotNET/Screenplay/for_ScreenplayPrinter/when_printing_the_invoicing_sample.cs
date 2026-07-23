// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_the_invoicing_sample : given.a_printer
{
    CompilationResult<ApplicationSyntax> _original;
    string _printed;
    CompilationResult<ApplicationSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _original = _compiler.Compile(for_ScreenplayCompiler.given.Samples.Invoicing);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.Compile(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_preserve_the_domain() => _reparsed.Value!.Domain!.Name.ShouldEqual(_original.Value!.Domain!.Name);
    [Fact] void should_preserve_the_imports() => _reparsed.Value!.Imports.Count().ShouldEqual(_original.Value!.Imports.Count());
    [Fact] void should_preserve_the_concepts() => _reparsed.Value!.Concepts.Count().ShouldEqual(_original.Value!.Concepts.Count());
    [Fact] void should_preserve_the_enum_values() => Concept(_reparsed, "InvoiceStatus").Values.Count().ShouldEqual(Concept(_original, "InvoiceStatus").Values.Count());
    [Fact] void should_preserve_the_pii_attribute() => Concept(_reparsed, "EmailAddress").Attributes.ShouldContain("pii");
    [Fact] void should_preserve_the_policies() => _reparsed.Value!.Policies.Count().ShouldEqual(_original.Value!.Policies.Count());
    [Fact] void should_preserve_the_personas() => _reparsed.Value!.Personas!.Count().ShouldEqual(_original.Value!.Personas!.Count());
    [Fact] void should_preserve_the_persona_policies() => _reparsed.Value!.Personas!.First().Policies.Count().ShouldEqual(_original.Value!.Personas!.First().Policies.Count());
    [Fact] void should_preserve_the_authentication_providers() => _reparsed.Value!.Authentication!.Providers.Count().ShouldEqual(_original.Value!.Authentication!.Providers.Count());
    [Fact] void should_preserve_the_provider_settings() => _reparsed.Value!.Authentication!.Providers.First().Settings.Count().ShouldEqual(_original.Value!.Authentication!.Providers.First().Settings.Count());
    [Fact] void should_keep_the_secret_settings_symbolic() => ((SecretExpressionSyntax)_reparsed.Value!.Authentication!.Providers.First().Settings.Single(_ => _.Name == "clientId").Value).Name.ShouldEqual("azureAdClientId");
    [Fact] void should_preserve_the_slices() => Slices(_reparsed).Count().ShouldEqual(Slices(_original).Count());
    [Fact] void should_preserve_the_module_description() => _reparsed.Value!.Modules.Single().Description.ShouldEqual(_original.Value!.Modules.Single().Description);
    [Fact] void should_preserve_the_feature_description() => _reparsed.Value!.Modules.Single().Features.Single().Description.ShouldEqual(_original.Value!.Modules.Single().Features.Single().Description);
    [Fact] void should_preserve_the_slice_description() => Slices(_reparsed).Single(_ => _.Name == "RegisterInvoice").Description.ShouldEqual(Slices(_original).Single(_ => _.Name == "RegisterInvoice").Description);
    [Fact] void should_preserve_the_conditional_produces() => Command(_reparsed, "RegisterInvoice").Produces.Count(_ => _.When is not null).ShouldEqual(Command(_original, "RegisterInvoice").Produces.Count(_ => _.When is not null));
    [Fact] void should_preserve_the_concurrency_event_source() => Concurrency(_reparsed).EventSource.ShouldEqual(Concurrency(_original).EventSource);
    [Fact] void should_preserve_the_concurrency_source_type() => Concurrency(_reparsed).EventSourceType.ShouldEqual(Concurrency(_original).EventSourceType);
    [Fact] void should_preserve_the_concurrency_stream_type() => Concurrency(_reparsed).EventStreamType.ShouldEqual(Concurrency(_original).EventStreamType);
    [Fact] void should_preserve_the_concurrency_stream_id() => Concurrency(_reparsed).EventStreamId.ShouldEqual(Concurrency(_original).EventStreamId);
    [Fact] void should_preserve_the_concurrency_events() => Concurrency(_reparsed).EventTypes.Count().ShouldEqual(Concurrency(_original).EventTypes.Count());
    [Fact] void should_preserve_the_validation_rules() => Rules(_reparsed).Count().ShouldEqual(Rules(_original).Count());
    [Fact] void should_preserve_the_strings_validation_message() => Command(_reparsed, "CancelInvoice").Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.First().Message.ShouldEqual("$strings.invoices.validation.reasonRequired");
    [Fact] void should_preserve_the_strings_action_label() => FooterAction(_reparsed).Label.ShouldEqual(FooterAction(_original).Label);
    [Fact] void should_preserve_the_concept_validation_rules() => ConceptRules(_reparsed).Count().ShouldEqual(ConceptRules(_original).Count());
    [Fact] void should_preserve_the_concept_value_subject() => ConceptRules(_reparsed).All(_ => _.Property == ValidationRuleSyntax.ConceptValue).ShouldBeTrue();
    [Fact] void should_preserve_the_event_tags() => RegisteredEvent(_reparsed).Tags!.Count().ShouldEqual(RegisteredEvent(_original).Tags!.Count());
    [Fact] void should_preserve_the_produces_tags() => Command(_reparsed, "RegisterInvoice").Produces.First().Tags!.Count().ShouldEqual(Command(_original, "RegisterInvoice").Produces.First().Tags!.Count());
    [Fact] void should_preserve_the_capture_append_tags() => AppendTags(_reparsed).Count().ShouldEqual(AppendTags(_original).Count());
    [Fact] void should_preserve_the_given_readmodels() => StatusSpecification(_reparsed).GivenReadModels!.Count().ShouldEqual(StatusSpecification(_original).GivenReadModels!.Count());
    [Fact] void should_preserve_the_then_readmodels() => StatusSpecification(_reparsed).ThenReadModels!.Count().ShouldEqual(StatusSpecification(_original).ThenReadModels!.Count());
    [Fact] void should_preserve_the_readmodel_properties() => StatusSpecification(_reparsed).GivenReadModels!.Single().Properties.Count().ShouldEqual(StatusSpecification(_original).GivenReadModels!.Single().Properties.Count());
    [Fact] void should_preserve_the_seed_groups() => Seed(_reparsed).Groups.Count().ShouldEqual(Seed(_original).Groups.Count());
    [Fact] void should_preserve_the_seeded_events() => Seed(_reparsed).Groups.First().Events.Count().ShouldEqual(Seed(_original).Groups.First().Events.Count());
    [Fact] void should_preserve_the_seeded_event_properties() => Seed(_reparsed).Groups.First().Events.First().Properties.Count().ShouldEqual(Seed(_original).Groups.First().Events.First().Properties.Count());

    static ConceptSyntax Concept(CompilationResult<ApplicationSyntax> result, string name) =>
        result.Value!.Concepts.Single(_ => _.Name == name);

    static IEnumerable<SliceSyntax> Slices(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices;

    static CommandSyntax Command(CompilationResult<ApplicationSyntax> result, string slice) =>
        Slices(result).Single(_ => _.Name == slice).Commands.Single();

    static ConcurrencySyntax Concurrency(CompilationResult<ApplicationSyntax> result) =>
        Command(result, "RegisterInvoice").Concurrency!;

    static EventSyntax RegisteredEvent(CompilationResult<ApplicationSyntax> result) =>
        Slices(result).Single(_ => _.Name == "RegisterInvoice").Events.Single(_ => _.Name == "InvoiceRegistered");

    static SeedSyntax Seed(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Seeds!.Single();

    static SpecificationSyntax StatusSpecification(CompilationResult<ApplicationSyntax> result) =>
        Slices(result).Single(_ => _.Name == "ChangeInvoiceStatus").Specifications.Single();

    static ScreenActionSyntax FooterAction(CompilationResult<ApplicationSyntax> result) =>
        Slices(result).Single(_ => _.Name == "InvoiceDashboard").Screens.Single().Directives
            .OfType<ScreenLayoutSyntax>().Single().Slots.Single(_ => _.Name == "footer").Directives
            .OfType<ScreenSectionSyntax>().Single().Directives.OfType<ScreenActionSyntax>().Single();

    static IEnumerable<TagSyntax> AppendTags(CompilationResult<ApplicationSyntax> result) =>
        Slices(result).Single(_ => _.Name == "LegacyInvoiceSync").Captures.Single()
            .Appends.First(_ => _.Event == "InvoiceStatusChanged").Tags!;

    static IEnumerable<ValidationRuleSyntax> Rules(CompilationResult<ApplicationSyntax> result) =>
        Command(result, "RegisterInvoice").Validations.OfType<DeclarativeValidateSyntax>().Single().Rules;

    static IEnumerable<ValidationRuleSyntax> ConceptRules(CompilationResult<ApplicationSyntax> result) =>
        Concept(result, "EmailAddress").Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules;
}
