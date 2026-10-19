// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Samples;

// The older binder specification uses a legacy embedded compiler fixture.
// This specification pins the new forms in the maintained root sample.
public class when_binding_the_invoicing_sample : Specification
{
    Diagnostic[] _diagnostics;
    ApplicationSyntax _application;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Samples/Invoicing/invoicing.play"));
        _application = new ScreenplayCompiler().Compile(source).Value!;
        var workspace = ScreenplayWorkspace.Create("Invoicing",
            [WorkspaceDocument.Create("sample", PortablePlayPath.Parse("invoicing.play"), Encoding.UTF8.GetBytes(source))],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing")));
        _diagnostics = [.. workspace.Compilation.Diagnostics];
    }

    [Fact]
    void should_parse_the_anonymous_policy() => _application.Policies.Single(policy => policy.Name == "Anyone").Condition.ShouldBeOfExactType<AnonymousConditionSyntax>();

    [Fact]
    void should_not_ask_the_anonymous_query_specification_for_a_caller() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.MissingSpecificationCaller).ShouldBeFalse();

    [Fact]
    void should_keep_the_anonymous_gate_alone_on_its_path() => _diagnostics.Any(diagnostic =>
        diagnostic.Code is DiagnosticCodes.AnonymousPolicyUnderEnclosingGate or DiagnosticCodes.AnonymousPolicyInCompositeGate).ShouldBeFalse();

    [Fact]
    void should_pin_the_described_command_information() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.DescribedCommandNotImplemented && diagnostic.Severity == DiagnosticSeverity.Information &&
        diagnostic.Message.Contains("ApplyLateFee", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_admit_the_composite_key_shape()
    {
        var readModel = SyntaxNodes.Under(_application).OfType<ReadModelSyntax>().Single(model => model.Name == "InvoicePaymentMethodTotals");
        _diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Location.Line == readModel.Location.Line).ShouldBeEmpty();
    }

    [Fact]
    void should_keep_the_explicit_single_key_out_of_identity_ambiguity_dispositions() => _diagnostics.Any(diagnostic =>
        diagnostic.Message.Contains("Read model 'InvoiceListReadModel' must have one unambiguous keyed query", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_pin_the_payload_key_read_refusal() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.UnprotectableDecisionRead && diagnostic.Message.Contains("InvoiceBalanceReadModel", StringComparison.Ordinal) &&
        diagnostic.Message.Contains("PayloadKey", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_pin_the_composite_read_refusal() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.UnprotectableDecisionRead && diagnostic.Message.Contains("InvoicePaymentMethodTotals", StringComparison.Ordinal) &&
        diagnostic.Message.Contains("ByParts", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_pin_the_path_of_the_refused_read() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.UnknownReadPath && diagnostic.Message.Contains("balance.outstanding", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_not_treat_opted_in_command_reads_as_legacy() => _diagnostics.Any(diagnostic =>
        diagnostic.Code == DiagnosticCodes.PreservedLegacySemanticSyntax && diagnostic.Message.StartsWith("Command '", StringComparison.Ordinal) &&
        diagnostic.Message.Contains("reads", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_select_the_protected_read_mode() => _application.CommandReadMode.ShouldEqual(CommandReadMode.Protected);

    [Fact]
    void should_not_refuse_the_new_route_or_filter_constructs() => _diagnostics.Where(diagnostic => diagnostic.Code is
        DiagnosticCodes.ProductionRouteOutsideCommand or DiagnosticCodes.RedundantProductionRoute or DiagnosticCodes.InvalidObserverFilter or DiagnosticCodes.ObserverFilterExcludesEveryProducer).ShouldBeEmpty();

    [Fact]
    void should_not_refuse_refusal_branches_or_values() => _diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("#433", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_admit_the_proven_generated_receipt_rule() => _diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule || diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule).ShouldBeFalse();

    [Fact]
    void should_show_each_literal_text_rule() => new[] { ValidationRuleKind.Contains, ValidationRuleKind.NotContains, ValidationRuleKind.StartsWith, ValidationRuleKind.EndsWith }.ShouldEachConformTo(kind =>
        SyntaxNodes.Under(_application).OfType<ValidationRuleSyntax>().Any(rule => rule.Rule == kind));

    [Fact]
    void should_not_refuse_literal_text_rules() => _diagnostics.Any(diagnostic => diagnostic.Message.Contains("text comparison requires", StringComparison.Ordinal) || diagnostic.Code == DiagnosticCodes.DeprecatedStudioTextRulePattern).ShouldBeFalse();

    [Fact]
    void should_show_the_admitted_date_literal_rule() => SyntaxNodes.Under(_application).OfType<ValidationRuleSyntax>().Any(rule =>
        rule.Property == "dueDate" && rule.Rule == ValidationRuleKind.GreaterThanOrEqual && rule.Value is LiteralExpressionSyntax literal && literal.Value is string text && text == "2020-01-01").ShouldBeTrue();

    [Fact]
    void should_not_refuse_the_date_literal_rule()
    {
        var rule = SyntaxNodes.Under(_application).OfType<ValidationRuleSyntax>().Single(rule =>
            rule.Property == "dueDate" && rule.Rule == ValidationRuleKind.GreaterThanOrEqual && rule.Value is LiteralExpressionSyntax literal && literal.Value is string text && text == "2020-01-01");
        _diagnostics.Any(diagnostic => diagnostic.Location.Line == rule.Location.Line && (diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax || diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding)).ShouldBeFalse();
    }

    [Fact]
    void should_show_the_admitted_uuid_requirement() => SyntaxNodes.Under(_application).OfType<RequirementSyntax>().Any(requirement =>
        requirement.Condition is ComparisonConditionSyntax { Left: "customerId", Operator: ComparisonOperator.NotEqual, Right: LiteralExpressionSyntax { Value: "00000000-0000-0000-0000-000000000000" } }).ShouldBeTrue();

    [Fact]
    void should_not_refuse_the_uuid_requirement()
    {
        var requirement = SyntaxNodes.Under(_application).OfType<RequirementSyntax>().Single(requirement =>
            requirement.Condition is ComparisonConditionSyntax { Left: "customerId", Right: LiteralExpressionSyntax { Value: "00000000-0000-0000-0000-000000000000" } });
        _diagnostics.Any(diagnostic => diagnostic.Location.Line == requirement.Location.Line && (diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax || diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding)).ShouldBeFalse();
    }

    [Fact]
    void should_keep_today_refused() => _diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax &&
        diagnostic.Message.Contains("'dueDate'", StringComparison.Ordinal) && diagnostic.Message.Contains("date", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_show_both_key_shapes_and_production_routes() => SyntaxNodes.Under(_application).OfType<ReadModelSyntax>().Count(model => model.Properties.Any(property => property.IsKey)).ShouldEqual(3);
}
