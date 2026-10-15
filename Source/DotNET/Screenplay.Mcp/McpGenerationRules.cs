// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

static class McpGenerationRules
{
    internal static void Add(Dictionary<string, object?> details, McpSnapshot snapshot, ConceptSyntax concept)
    {
        var inspection = snapshot.Compilation.Value is { } application
            ? SemanticModelBinder.InspectConceptGeneration(application, concept, snapshot.Sources)
            : default;
        var generation = inspection.Concept?.Generation;
        var hasGeneration = inspection.Generates;
        string? shapeReason = null;
        var pattern = hasGeneration ? DerivedGenerationRepairPattern.Create(generation, out shapeReason) : null;
        details.Add("derivedPattern", pattern);
        var unknownReason = inspection.UnknownReason ?? (hasGeneration
            ? $"No regular shape is established for generation strategy '{concept.Generation!.Strategy}' or its authored inputs."
            : "The generation could not be bound; see executable diagnostics.");
        if (pattern is null) details.Add("derivedPatternUnknownReason", generation?.Kind == "pattern" && shapeReason is not null ? shapeReason : unknownReason);

        var rules = (concept.Validations ?? []).SelectMany(validation => validation is DeclarativeValidateSyntax declarative
            ? declarative.Rules.Cast<SyntaxNode>() : [validation]).ToArray();

        if (rules.Length > 0)
        {
            details.Add("rules", rules.Select(source => Describe(source, inspection.Concept, inspection.Rules?.GetValueOrDefault(source), hasGeneration ? null : unknownReason, snapshot.ExecutableCompilation.Diagnostics)).ToArray());
        }
    }

    static RuleConsistencyResult Prove(SemanticConcept? concept, SemanticValidationRule? rule, string? unknownReason)
    {
        if (unknownReason is not null) return RuleConsistencyResult.Unproven(unknownReason);
        if (rule is null) return RuleConsistencyResult.Unproven("The validation rule could not be bound; see executable diagnostics.");

        return RuleConsistency.Check(concept!.Generation, concept.Primitive, rule);
    }

    static Dictionary<string, object?> Describe(
        SyntaxNode source,
        SemanticConcept? concept,
        SemanticValidationRule? rule,
        string? unknownReason,
        IEnumerable<Diagnostic> diagnostics)
    {
        var proof = Prove(concept, rule, unknownReason);
        var kind = rule?.Kind switch
        {
            SemanticValidationRuleKind.RulePredicate => "predicate",
            not null => JsonNamingPolicy.CamelCase.ConvertName(rule.Kind.ToString()),
            _ => source is ValidationRuleSyntax syntax ? SyntaxKind(syntax.Rule) : "codeValidation"
        };
        var result = new Dictionary<string, object?>
        {
            ["rule"] = kind,
            ["line"] = source.Location.Line,
            ["column"] = source.Location.Column,
            ["status"] = JsonNamingPolicy.CamelCase.ConvertName(proof.Status.ToString())
        };
        if (proof.Witness is not null) result.Add("witness", proof.Witness);
        if (proof.Reason is not null) result.Add("reason", proof.Reason);
        var code = proof.Status switch
        {
            RuleConsistencyStatus.Refuted => DiagnosticCodes.RefutedGeneratedConceptRule,
            RuleConsistencyStatus.Unproven => DiagnosticCodes.UnprovenGeneratedConceptRule,
            _ => null
        };

        if (code is not null && diagnostics.Any(diagnostic => string.Equals(diagnostic.Code, code, StringComparison.Ordinal) && diagnostic.Location == source.Location))
        {
            result.Add("diagnosticCode", code);
            if (string.Equals(code, DiagnosticCodes.RefutedGeneratedConceptRule, StringComparison.Ordinal) && source is ValidationRuleSyntax) result.Add("repair", code);
        }

        return result;
    }

    static string SyntaxKind(ValidationRuleKind kind) => kind switch
    {
        ValidationRuleKind.Min => "minimum",
        ValidationRuleKind.Max => "maximum",
        ValidationRuleKind.Rule => "predicate",
        _ => JsonNamingPolicy.CamelCase.ConvertName(kind.ToString())
    };
}
