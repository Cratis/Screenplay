// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_expanding_specification_cases : Specification
{
    const string Source = """
        example Base : Record
          amount = 1
        module M
          feature F
            slice StateChange S
              command Record
                amount Int
              event Recorded
                amount Int
              specification Recording
                description "Keeps the amount"
                parameter amount Int
                case Small amount = 10
                case Large
                  amount = 100
                when Base amount = case.amount
                then Recorded amount = case.amount
        """;

    ApplicationSyntax _authored;
    EffectiveSpecificationApplication _expanded;

    void Establish() => _authored = new ScreenplayCompiler().Parse(Source).Value;
    void Because() => _expanded = SpecificationExamples.Expand(_authored);

    [Fact] void should_expand_in_case_order() => _expanded.Specifications.Select(specification => specification.Effective.Name).ShouldContainOnly("Recording_Small", "Recording_Large");
    [Fact] void should_not_report_diagnostics() => _expanded.Diagnostics.ShouldBeEmpty();
    [Fact] void should_substitute_after_example_overrides() => ((LiteralExpressionSyntax)_expanded.Specifications[1].Effective.When.Values.Single().Source).Value.ShouldEqual(100d);
    [Fact] void should_keep_description() => _expanded.Specifications[0].Effective.Description.ShouldEqual("Keeps the amount");
    [Fact] void should_keep_case_provenance() => _expanded.Specifications[0].Case.Name.ShouldEqual("Small");
    [Fact] void should_keep_parameter_provenance() => _expanded.Specifications[0].Steps.Single(step => step.Role == "when").Values.Single().CaseParameter.ShouldEqual("amount");
    [Fact] void should_classify_case_origins() => _expanded.Specifications[0].Steps.Single(step => step.Role == "when").Values.Single().Origin.ShouldEqual(SpecificationValueOrigin.Case);
    [Fact] void should_keep_authored_reference() => _authored.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When.Values.Single().Source.ShouldBeOfExactType<CaseValueExpressionSyntax>();
    [Fact] void should_remove_tables_from_effective_syntax() => _expanded.Specifications[0].Effective.Parameters.ShouldBeEmpty();
    [Fact] void should_round_trip_source() => SyntaxJson.StructurallyEqual(_authored, new ScreenplayCompiler().Parse(new ScreenplayPrinter().Print(_authored)).Value).ShouldBeTrue();

    [Fact]
    void should_refuse_singular_expansion()
    {
        var specification = _authored.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        SpecificationExamples.Expand(specification, _authored, ["M", "F", "S"]).Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.SingularSpecificationTableExpansion);
    }

    [Fact]
    void should_offer_plural_standalone_expansion()
    {
        var specification = _authored.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        SpecificationExamples.ExpandAll(specification, _authored, ["M", "F", "S"]).Value.Count.ShouldEqual(2);
    }

    [Theory]
    [InlineData("parameter amount", DiagnosticCodes.InvalidSpecificationParameter)]
    [InlineData("case Bad invalid", DiagnosticCodes.InvalidSpecificationCase)]
    [InlineData("parameter amount Int\n  parameter amount Int\n  case Small amount = 1", DiagnosticCodes.DuplicateSpecificationParameter)]
    [InlineData("parameter amount Int\n  case Small amount = 1\n  case Small amount = 2", DiagnosticCodes.DuplicateSpecificationCase)]
    [InlineData("parameter amount Int", DiagnosticCodes.IncompleteSpecificationTable)]
    [InlineData("case Small amount = 1", DiagnosticCodes.IncompleteSpecificationTable)]
    [InlineData("parameter amount Int\n  case Small", DiagnosticCodes.InvalidSpecificationCaseAssignment)]
    [InlineData("parameter amount Int\n  case Small amount = $context.command.amount", DiagnosticCodes.InvalidSpecificationCaseValue)]
    [InlineData("when Record amount = case.amount", DiagnosticCodes.InvalidSpecificationCaseReference)]
    void should_report_invalid_tables(string body, string code) => new ScreenplayCompiler().CompileSpecification($"specification Table\n  {body}").Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
}
