// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticComparison? BindUuidCondition(
            ComparisonConditionSyntax comparison,
            SemanticProperty property,
            SemanticConditionOperand left,
            Dictionary<string, SemanticProperty> properties,
            bool allowReadPaths,
            string position)
        {
            var admitted = (position == "require" || (position == "produces when" && left.Read is null)) &&
                comparison.Operator is ComparisonOperator.Equal or ComparisonOperator.NotEqual &&
                !property.Type.IsCollection && !property.Type.IsOptional;
            SemanticConditionOperand? right = null;
            SemanticProperty? other = null;
            if (admitted && comparison.Right is PathExpressionSyntax path)
            {
                if (properties.TryGetValue(path.Path, out var commandProperty))
                {
                    other = commandProperty;
                    right = new(commandProperty.Id, null);
                }
                else if (allowReadPaths && position == "require" && path.Path.Contains('.'))
                {
                    if (BindReadPath(path.Path, path.Location, position) is not { } read) return null;
                    other = read.Property;
                    right = new(read.Property.Id, null) { Read = read.Read };
                }
                else
                {
                    admitted = false;
                }
            }
            else
            {
                admitted &= comparison.Right is LiteralExpressionSyntax { Value: string };
            }

            if (other is not null)
            {
                var otherSubject = CommandValidationSubject(other.Name, other.Type);
                admitted &= !other.Type.IsCollection && !other.Type.IsOptional && otherSubject.Primitive == SemanticPrimitiveType.Uuid &&
                    !(property.Type.Kind == SemanticTypeReferenceKind.Concept && other.Type.Kind == SemanticTypeReferenceKind.Concept && property.Type.Target != other.Type.Target);
            }

            // Gate the whole comparison before checking the literal, so a refused comparison reports only PLAY0268.
            if (!admitted)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Uuid comparisons require == or != between required scalar Uuid command properties or protected read paths of the same concept, or a canonical Uuid literal, in require or produces when; ordering, optional operands, other concepts, text, context and reaction where are not admitted.", comparison.Location);
                return null;
            }

            if (right is null)
            {
                var literal = (LiteralExpressionSyntax)comparison.Right;
                var text = (string)literal.Value!;
                if (!SemanticValueRules.ValidateText(text, SemanticPrimitiveType.Uuid))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, "A Uuid comparison literal must be a lowercase hyphenated Uuid such as 3f2504e0-4f89-41d3-9a0c-0305e82c3301.", literal.Location);
                    return null;
                }

                right = new(default, SemanticValue.Text(text));
            }

            return new SemanticComparison(left, ConditionOperator(comparison.Operator), right);
        }
    }
}
