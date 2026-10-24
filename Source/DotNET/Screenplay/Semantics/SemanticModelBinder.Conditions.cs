// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticCondition? BindCondition(ConditionSyntax syntax, Dictionary<string, SemanticProperty> properties, bool allowReadPaths = false, string position = "condition")
        {
            if (syntax is LogicalConditionSyntax logical)
            {
                var left = BindCondition(logical.Left, properties, allowReadPaths, position);
                var rightCondition = BindCondition(logical.Right, properties, allowReadPaths, position);
                return left is null || rightCondition is null ? null : new SemanticLogicalCondition(
                    left, logical.Operator == LogicalOperator.And ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, rightCondition);
            }

            var comparison = AdmitClockFreeComparison(syntax);
            if (comparison is null) return null;

            if (comparison.Left.StartsWith('$'))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Context, identity and environment operands are not admitted as a command comparison subject.", comparison.Location);
                return null;
            }

            SemanticConditionOperand leftOperand;
            if (!properties.TryGetValue(comparison.Left, out var property))
            {
                if (allowReadPaths && comparison.Left.Contains('.'))
                {
                    if (BindReadPath(comparison.Left, comparison.Location, position) is not { } read) return null;
                    property = read.Property;
                    leftOperand = new(property.Id, null) { Read = read.Read };
                }
                else
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition operand '{comparison.Left}' must be a declared command property in {position}; read-model paths require the 'command reads protected' preamble (#129).", comparison.Location, DiagnosticArguments.ForIssue("Cratis/Screenplay#129"));
                    return null;
                }
            }
            else
            {
                leftOperand = new(property.Id, null);
            }

            var subject = CommandValidationSubject(property.Name, property.Type);
            if (subject.IsDate) return BindDateCondition(comparison, property, leftOperand, properties, position);
            if (subject.Primitive == SemanticPrimitiveType.Uuid) return BindUuidCondition(comparison, property, leftOperand, properties, allowReadPaths, position);
            if (property.Type.IsOptional)
            {
                OptionalConditionOperand(property, comparison.Location);
                return null;
            }

            var right = comparison.Right switch
            {
                LiteralExpressionSyntax literal => new(default, BindLiteral(literal)),
                PathExpressionSyntax path when properties.TryGetValue(path.Path, out var other) => new(other.Id, null),

                // A bare name that is no property of the command but a member of the compared enumeration is that
                // member - 'status == sent' means the same as 'status == "sent"', as it does in a validation rule.
                PathExpressionSyntax path when subject.Values.Contains(path.Path, StringComparer.Ordinal) => new(default, SemanticValue.Text(path.Path)),
                PathExpressionSyntax path when allowReadPaths && path.Path.Contains('.') => BindReadConditionOperand(path, position),
                _ => null
            };
            if (right is null)
            {
                var reason = comparison.Right switch
                {
                    EnvironmentExpressionSyntax => "$env operands are non-deterministic across realizations",
                    ContextExpressionSyntax => "$context operands require ESM v2 (#226)",
                    IdentityExpressionSyntax => "$identity operands require ESM v2 (#226)",
                    PathExpressionSyntax => ProtectedCommandReads
                        ? $"read-model paths are not admitted in {position}; 'today' and undeclared command properties are not portable operands"
                        : "read-model paths require decision-consistent reads (#129); 'today' and undeclared command properties are not portable operands",
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
                var other = right.Read is { } readName
                    ? _commandReads[readName].View.Properties.Values.Single(value => value.Id == right.Property)
                    : properties.Values.Single(value => value.Id == right.Property);
                if (other.Type.IsOptional)
                {
                    OptionalConditionOperand(other, comparison.Location);
                    return null;
                }

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
            return new SemanticComparison(leftOperand, op, right);
        }

        void OptionalConditionOperand(SemanticProperty property, SourceLocation location) =>
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Condition on '{property.Name}' is not admitted: comparison operands must be required; optional properties have no portable comparison.", location);

        SemanticConditionOperand? BindReadConditionOperand(PathExpressionSyntax path, string position) =>
            BindReadPath(path.Path, path.Location, position) is { } read ? new(read.Property.Id, null) { Read = read.Read } : null;
    }
}
