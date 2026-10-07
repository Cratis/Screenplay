// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_expanding_typed_examples : Specification
{
    const string Source =
        """
        example AFact : Recorded
          amount = 10
          for "original"
        module Billing
          example AnInput : Record
            id = "original"
            amount = 10
            generated token = "11111111-1111-1111-1111-111111111111"
          feature Invoicing
            example AView : Balance
              id = "original"
              amount = 10
            slice StateChange Recording
              command Record
                id String identifier
                amount Int
                token Uuid generated
              event Recorded
                amount Int
              readmodel Balance
                id String identifier
                amount Int
              specification ChangingTheAmount
                given AFact amount = 20
                  for "replacement"
                given readmodel AView
                when AnInput amount = 20
                  generated token = "22222222-2222-2222-2222-222222222222"
                then AFact
                then readmodel AView exactly
                  amount = 20
              specification AppendingAFact
                when append AFact
                then readmodel AView
        """;

    ApplicationSyntax _authored;
    EffectiveSpecificationApplication _result;

    void Establish() => _authored = new ScreenplayCompiler().Parse(Source).Value!;
    void Because() => _result = SpecificationExamples.Expand(_authored);

    [Fact] void should_expand_all_six_slots() => _result.Specifications.SelectMany(specification => specification.Steps).Select(step => step.Role).Distinct().Count().ShouldEqual(6);
    [Fact] void should_not_report_errors() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_authored_names() => _result.Specifications[0].Authored.When!.CommandType.ShouldEqual("AnInput");
    [Fact] void should_resolve_the_underlying_type_in_declaration_scope() => _result.Specifications[0].Effective.When!.CommandType.ShouldEqual("Billing.Invoicing.Recording.Record");
    [Fact] void should_merge_the_inline_override() => ((LiteralExpressionSyntax)_result.Specifications[0].Effective.When!.Values.Single(value => value.Property == "amount").Source).Value.ShouldEqual(20d);
    [Fact] void should_keep_subset_and_exact_matching() => _result.Specifications[0].Effective.ThenReadModels!.Single().Exactly.ShouldBeTrue();
    [Fact] void should_preserve_the_step_location() => _result.Specifications[0].Effective.When!.Location.ShouldEqual(_result.Specifications[0].Authored.When!.Location);
    [Fact] void should_record_inherited_values() => _result.Specifications[0].Steps.Single(step => step.Role == "when").Values.Single(value => value.Property == "id").Origin.ShouldEqual(SpecificationValueOrigin.Example);
    [Fact] void should_record_overridden_values() => _result.Specifications[0].Steps.Single(step => step.Role == "when").Values.Single(value => value.Property == "amount").Origin.ShouldEqual(SpecificationValueOrigin.Override);
    [Fact] void should_record_overridden_generated_values() => _result.Specifications[0].Steps.Single(step => step.Role == "when").Values.Single(value => value.Property == "generated token").Origin.ShouldEqual(SpecificationValueOrigin.Override);
    [Fact] void should_record_the_replaced_expression() => _result.Specifications[0].Steps[0].Values.Single(value => value.Property == "for").OverriddenValue.ShouldNotBeNull();
    [Fact] void should_be_syntax_idempotent() => SpecificationExamples.Expand(_result.Application).Specifications[0].Effective.When!.Values.ShouldContainOnly(_result.Specifications[0].Effective.When!.Values);

    [Fact]
    void should_expand_document_scoped_examples_in_a_standalone_specification()
    {
        var standalone = new ScreenplayCompiler().CompileSpecification("example Standalone : Record\n  id = \"key\"\n  amount = 10\nspecification Recording\n  when Standalone amount = 20\n  then Recorded amount = 20").Value!;
        var expanded = SpecificationExamples.Expand(standalone, _authored, ["Billing", "Invoicing", "Recording"]);
        expanded.Success.ShouldBeTrue();
        expanded.Value!.Effective.When!.CommandType.ShouldEqual("Billing.Invoicing.Recording.Record");
        expanded.Value.Steps.Single(step => step.Role == "when").Values.Single(value => value.Property == "amount").Origin.ShouldEqual(SpecificationValueOrigin.Override);
    }
}
