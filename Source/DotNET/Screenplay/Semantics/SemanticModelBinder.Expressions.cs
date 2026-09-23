// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticExpression? BindPropertyExpression(
            ExpressionSyntax expression,
            Dictionary<string, SemanticProperty> properties,
            string description)
        {
            if (expression is PathExpressionSyntax path && properties.TryGetValue(path.Path, out var property))
            {
                return SemanticExpression.Property(SemanticExpressionRootKind.Command, property.Id);
            }

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A {description} must resolve to one command property in ESM v1.", expression.Location);
            return null;
        }

        SemanticExpression? BindExpression(
            ExpressionSyntax expression,
            Dictionary<string, SemanticProperty> properties,
            SemanticExpressionRootKind root,
            string description) => expression switch
        {
            PathExpressionSyntax path when properties.TryGetValue(path.Path, out var property) =>
                SemanticExpression.Property(root, property.Id),
            LiteralExpressionSyntax literal => SemanticExpression.FromValue(BindLiteral(literal)),
            _ => UnsupportedExpression(expression, description)
        };

        SemanticValue BindLiteral(LiteralExpressionSyntax expression) => expression.Value switch
        {
            null => SemanticValue.Null,
            string value => SemanticValue.Text(value),
            bool value => SemanticValue.Boolean(value),
            double value => SemanticValue.Number(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
            _ => throw new InvalidSemanticContract($"Literal value type '{expression.Value.GetType().Name}' is unsupported during semantic binding.")
        };

        SemanticExpression? UnsupportedExpression(ExpressionSyntax expression, string description)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} expression '{expression.GetType().Name}' is not admitted by ESM v1.", expression.Location);
            return null;
        }
    }
}
