// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidateCondition(SemanticCondition condition, Dictionary<SemanticId, SemanticProperty> properties, int depth = 0)
        {
            if (depth > 32) throw new InvalidSemanticContract("Condition nesting exceeds the portable limit of 32.");
            switch (condition)
            {
                case SemanticLogicalCondition logical:
                    if (!Enum.IsDefined(logical.Operator)) throw new InvalidSemanticContract("Unknown logical condition operator.");
                    ValidateCondition(logical.Left, properties, depth + 1);
                    ValidateCondition(logical.Right, properties, depth + 1);
                    break;
                case SemanticComparison comparison:
                    if (!Enum.IsDefined(comparison.Operator)) throw new InvalidSemanticContract("Unknown comparison operator.");
                    var left = ConditionOperandType(comparison.Left, properties);
                    var right = ConditionOperandType(comparison.Right, properties);
                    var leftPrimitive = UnderlyingPrimitive(left);
                    var rightPrimitive = UnderlyingPrimitive(right);
                    if (left.IsCollection || right.IsCollection || left.IsOptional || right.IsOptional ||
                        (leftPrimitive != rightPrimitive && !(leftPrimitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber && rightPrimitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber)) ||
                        (leftPrimitive is not (SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber)) ||
                        (comparison.Operator is not (SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual) &&
                         leftPrimitive is not (SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber)))
                    {
                        throw new InvalidSemanticContract("Condition operands must have compatible scalar types; ordering requires numbers and equality requires text, enum, number or boolean.");
                    }

                    break;
                default: throw new InvalidSemanticContract("Unknown condition node.");
            }
        }

        SemanticTypeReference ConditionOperandType(SemanticConditionOperand operand, Dictionary<SemanticId, SemanticProperty> properties)
        {
            if (operand is null || operand.Property.IsSet == (operand.Value is not null))
                throw new InvalidSemanticContract("A condition operand must set exactly one property or constant.");
            if (operand.Property.IsSet)
            {
                if (!properties.TryGetValue(operand.Property, out var property)) throw new InvalidSemanticContract("Condition property is not a command property.");
                return property.Type;
            }

            return operand.Value switch
            {
                SemanticTextValue => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
                SemanticBooleanValue => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean),
                SemanticNumberValue => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber),
                _ => throw new InvalidSemanticContract("Condition constants must be text, number or boolean.")
            };
        }

        void ValidateTags(ImmutableArray<string> tags)
        {
            if (tags.IsDefault || tags.Any(string.IsNullOrWhiteSpace)) throw new InvalidSemanticContract("Append tags must be nonempty literal text.");
        }
    }
}
