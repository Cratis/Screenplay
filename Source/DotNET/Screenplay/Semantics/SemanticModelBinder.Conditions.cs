// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticCondition? BindCondition(ConditionSyntax syntax, Dictionary<string, SemanticProperty> properties)
        {
            if (syntax is LogicalConditionSyntax logical)
            {
                var left = BindCondition(logical.Left, properties);
                var rightCondition = BindCondition(logical.Right, properties);
                return left is null || rightCondition is null ? null : new SemanticLogicalCondition(
                    left, logical.Operator == LogicalOperator.And ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, rightCondition);
            }

            if (syntax is not ComparisonConditionSyntax comparison)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "This condition has no portable ESM v1 meaning.", syntax.Location);
                return null;
            }

            if (!properties.TryGetValue(comparison.Left, out var property))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition operand '{comparison.Left}' must be a declared command property; read-model paths require decision-consistent reads (#129).", comparison.Location);
                return null;
            }

            var subject = CommandValidationSubject(property.Name, property.Type);
            SemanticConditionOperand? right = comparison.Right switch
            {
                LiteralExpressionSyntax literal => new(default, BindLiteral(literal)),
                PathExpressionSyntax path when properties.TryGetValue(path.Path, out var other) => new(other.Id, null),
                _ => null
            };
            if (right is null)
            {
                var reason = comparison.Right switch
                {
                    EnvironmentExpressionSyntax => "$env operands are non-deterministic across realizations",
                    ContextExpressionSyntax => "$context operands require ESM v2 (#226)",
                    PathExpressionSyntax => "read-model paths require decision-consistent reads (#129); 'today' and undeclared command properties are not portable operands",
                    _ => "only command properties and constants are portable operands"
                };
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition operand is not admitted: {reason}.", comparison.Right.Location);
                return null;
            }

            var ordering = comparison.Operator is ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual or ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual;
            if (comparison.Operator is not (ComparisonOperator.Equal or ComparisonOperator.NotEqual or ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual or ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual) ||
                subject.IsCollection || subject.IsDate || (ordering ? !subject.IsNumber : !(subject.IsNumber || subject.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean)))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition on '{property.Name}' is not admitted: ordering requires numbers; equality requires scalar text, enumeration, number or boolean (dates and today are not supported).", comparison.Location);
                return null;
            }

            if (right.Value is { } value && (value is SemanticNullValue || OperandMismatch(value, SemanticValidationRuleKind.Equal, subject) is not null))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition operand for '{property.Name}' must match its scalar type and declared enumeration values.", comparison.Right.Location);
                return null;
            }

            if (right.Property.IsSet)
            {
                var other = properties.Values.Single(_ => _.Id == right.Property);
                var otherSubject = CommandValidationSubject(other.Name, other.Type);
                if (otherSubject.Primitive != subject.Primitive || otherSubject.IsCollection || otherSubject.IsDate ||
                    (subject.Primitive == SemanticPrimitiveType.Text && !otherSubject.Values.SequenceEqual(subject.Values)))
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition property '{other.Name}' must have the same scalar type as '{property.Name}'.", comparison.Right.Location);
                    return null;
                }
            }

            var op = comparison.Operator switch
            {
                ComparisonOperator.Equal => SemanticComparisonOperator.Equal,
                ComparisonOperator.NotEqual => SemanticComparisonOperator.NotEqual,
                ComparisonOperator.GreaterThan => SemanticComparisonOperator.GreaterThan,
                ComparisonOperator.GreaterThanOrEqual => SemanticComparisonOperator.GreaterThanOrEqual,
                ComparisonOperator.LessThan => SemanticComparisonOperator.LessThan,
                _ => SemanticComparisonOperator.LessThanOrEqual
            };
            return new SemanticComparison(new(property.Id, null), op, right);
        }
    }
}
