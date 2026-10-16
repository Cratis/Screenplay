// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static SemanticComparisonOperator ConditionOperator(ComparisonOperator op) => op switch
        {
            ComparisonOperator.Equal => SemanticComparisonOperator.Equal,
            ComparisonOperator.NotEqual => SemanticComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan => SemanticComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterThanOrEqual => SemanticComparisonOperator.GreaterThanOrEqual,
            ComparisonOperator.LessThan => SemanticComparisonOperator.LessThan,
            _ => SemanticComparisonOperator.LessThanOrEqual
        };

        SemanticComparison? BindDateCondition(
            ComparisonConditionSyntax comparison,
            SemanticProperty property,
            SemanticConditionOperand left,
            Dictionary<string, SemanticProperty> properties,
            string position)
        {
            var primitive = CommandValidationSubject(property.Name, property.Type).Primitive;
            var admitted = (position == "require" || position == "produces when") && left.Read is null &&
                !property.Type.IsCollection && !property.Type.IsOptional &&
                comparison.Operator is ComparisonOperator.Equal or ComparisonOperator.NotEqual or
                    ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual or ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual;
            SemanticConditionOperand? right = null;
            if (comparison.Right is PathExpressionSyntax path && properties.TryGetValue(path.Path, out var other))
            {
                admitted &= !other.Type.IsCollection && !other.Type.IsOptional && CommandValidationSubject(other.Name, other.Type).Primitive == primitive;
                right = new(other.Id, null);
            }
            else
            {
                admitted &= comparison.Right is LiteralExpressionSyntax;
            }

            // Gate the whole comparison before checking literals, so refused positions report only PLAY0268.
            if (!admitted)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Date comparisons require same-primitive scalar required command properties or a literal in require or produces when; read paths, context and today are not admitted.", comparison.Location);
                return null;
            }

            if (right is null)
            {
                var value = BindDateLiteral((LiteralExpressionSyntax)comparison.Right, primitive);
                if (value is null) return null;
                right = new(default, value);
            }

            return new SemanticComparison(left, ConditionOperator(comparison.Operator), right);
        }

        SemanticValidationRule? BindDateRule(ValidationRuleSyntax rule, SemanticId property, ValidationSubject subject)
        {
            var kind = Kind(rule.Rule);
            if (!property.IsSet || subject.IsCollection || !SemanticDateComparison.IsComparison(kind) || rule.Value is not LiteralExpressionSyntax literal)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Date validation comparisons require a command or property rule with a literal operand; concept rules, property references and today are not admitted.", rule.Location);
                return null;
            }

            var value = BindDateLiteral(literal, subject.Primitive);
            return value is null ? null : new(property, kind, value, rule.Message) { Severity = Severity(rule.Severity) };
        }

        SemanticValue? BindDateLiteral(LiteralExpressionSyntax literal, SemanticPrimitiveType primitive)
        {
            if (literal.Value is string text && SemanticDateComparison.Canonical(text, primitive) is { } canonical)
                return SemanticValue.Text(canonical);

            Error(DiagnosticCodes.InvalidSemanticBinding, "A date comparison literal must be a valid yyyy-MM-dd date or a DateTime with an explicit offset or Z.", literal.Location);
            return null;
        }
    }
}
