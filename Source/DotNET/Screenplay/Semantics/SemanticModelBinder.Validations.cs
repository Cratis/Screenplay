// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    /// <summary>
    /// Describes what a validation rule constrains - a command property or a concept's own value.
    /// </summary>
    /// <param name="Description">The subject as it reads in a diagnostic, such as <c>'amount'</c>.</param>
    /// <param name="Primitive">The underlying primitive of the subject, or <see cref="SemanticPrimitiveType.Unknown"/> for a composite.</param>
    /// <param name="IsCollection">Whether the subject is a collection.</param>
    /// <param name="Values">The declared members when the subject is an enumeration.</param>
    sealed record ValidationSubject(
        string Description,
        SemanticPrimitiveType Primitive,
        bool IsCollection,
        ImmutableArray<string> Values)
    {
        public bool IsNumber => Primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;

        public bool IsText => Primitive == SemanticPrimitiveType.Text && Values.IsEmpty;

        public bool IsDate => Primitive is SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;
    }

    private sealed partial class BindingContext
    {
        const string NoDateValue = "ESM v1 has no runtime date value - dates are text in a fixed format, so they cannot be compared";

        static string? Inadmissible(SemanticValidationRuleKind kind, ValidationSubject subject) => kind switch
        {
            SemanticValidationRuleKind.Unknown => "ESM v1 does not define this rule",
            _ when subject.IsDate => NoDateValue,
            SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum when subject.IsCollection || !(subject.IsNumber || subject.IsText) =>
                "a bound constrains the length of one text value or the size of one number",
            SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual
                when subject.IsCollection || !(subject.IsNumber || subject.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean) =>
                "equality compares one text, enumeration, number or boolean value",
            SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
                SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual when subject.IsCollection || !subject.IsNumber =>
                "ordering compares one whole or decimal number",
            _ => null
        };

        static string? OperandMismatch(SemanticValue operand, SemanticValidationRuleKind kind, ValidationSubject subject)
        {
            if (operand is SemanticNullValue)
            {
                return "cannot be null - use 'not empty' to require a value";
            }

            if (subject.IsText && kind is SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum)
            {
                return operand is SemanticNumberValue { Value: >= 0 } length && decimal.Truncate(length.Value) == length.Value
                    ? null
                    : "must be a non-negative whole number because it bounds the text length";
            }

            return (subject.Primitive, operand) switch
            {
                (SemanticPrimitiveType.WholeNumber, SemanticNumberValue number) when decimal.Truncate(number.Value) == number.Value => null,
                (SemanticPrimitiveType.DecimalNumber, SemanticNumberValue) => null,
                (SemanticPrimitiveType.Boolean, SemanticBooleanValue) => null,
                (SemanticPrimitiveType.Text, SemanticTextValue text) when subject.Values.IsEmpty || subject.Values.Contains(text.Value, StringComparer.Ordinal) => null,
                (SemanticPrimitiveType.Text, SemanticTextValue) => "must be a declared member of the enumeration",
                _ => $"must be a {TypeName(subject.Primitive)} to match what it is compared with"
            };
        }

        static string TypeName(SemanticPrimitiveType primitive) => primitive switch
        {
            SemanticPrimitiveType.WholeNumber => "whole number",
            SemanticPrimitiveType.DecimalNumber => "number",
            SemanticPrimitiveType.Boolean => "boolean",
            SemanticPrimitiveType.Text => "text value",
            _ => "value of the same type"
        };

        static SemanticValidationRuleKind Kind(ValidationRuleKind kind) => kind switch
        {
            ValidationRuleKind.NotEmpty => SemanticValidationRuleKind.NotEmpty,
            ValidationRuleKind.Max => SemanticValidationRuleKind.Maximum,
            ValidationRuleKind.Min => SemanticValidationRuleKind.Minimum,
            ValidationRuleKind.Equal => SemanticValidationRuleKind.Equal,
            ValidationRuleKind.NotEqual => SemanticValidationRuleKind.NotEqual,
            ValidationRuleKind.GreaterThan => SemanticValidationRuleKind.GreaterThan,
            ValidationRuleKind.GreaterThanOrEqual => SemanticValidationRuleKind.GreaterThanOrEqual,
            ValidationRuleKind.LessThan => SemanticValidationRuleKind.LessThan,
            ValidationRuleKind.LessThanOrEqual => SemanticValidationRuleKind.LessThanOrEqual,
            _ => SemanticValidationRuleKind.Unknown
        };

        static string Spelling(ValidationRuleKind kind) => kind switch
        {
            ValidationRuleKind.NotEmpty => "not empty",
            ValidationRuleKind.Max => "max",
            ValidationRuleKind.Min => "min",
            ValidationRuleKind.GreaterThan => ">",
            ValidationRuleKind.GreaterThanOrEqual => ">=",
            ValidationRuleKind.LessThan => "<",
            ValidationRuleKind.LessThanOrEqual => "<=",
            ValidationRuleKind.Equal => "==",
            ValidationRuleKind.NotEqual => "!=",
            ValidationRuleKind.Length => "length ==",
            ValidationRuleKind.Matches => "matches",
            ValidationRuleKind.AllGreaterThan => "all >",
            ValidationRuleKind.AllGreaterThanOrEqual => "all >=",
            ValidationRuleKind.Rule => "rule",
            _ => kind.ToString()
        };

        ValidationSubject CommandValidationSubject(string property, SemanticTypeReference type)
        {
            var (primitive, values) = type.Kind switch
            {
                SemanticTypeReferenceKind.Primitive => (type.Primitive, ImmutableArray<string>.Empty),
                SemanticTypeReferenceKind.Concept => ConceptShape(type.Target),
                _ => (SemanticPrimitiveType.Unknown, ImmutableArray<string>.Empty)
            };
            return new($"'{property}'", primitive, type.IsCollection, values);
        }

        ValidationSubject ConceptValidationSubject(ConceptSyntax concept) => new(
            $"concept '{concept.Name}'",
            concept.IsEnum ? SemanticPrimitiveType.Text : Primitive(concept.Type),
            false,
            concept.IsEnum ? [.. concept.Values] : []);

        (SemanticPrimitiveType Primitive, ImmutableArray<string> Values) ConceptShape(SemanticId id)
        {
            var concept = syntax.Concepts.First(_ => _concepts[_.Name].Id == id);
            return concept.IsEnum ? (SemanticPrimitiveType.Text, [.. concept.Values]) : (Primitive(concept.Type), []);
        }

        /// <summary>
        /// Binds one declarative rule to ESM, or reports precisely why it cannot bind.
        /// </summary>
        /// <param name="rule">The rule syntax.</param>
        /// <param name="property">The constrained property, or a default identity for a concept's own value.</param>
        /// <param name="subject">What the rule constrains.</param>
        /// <returns>The bound rule, or <c>null</c> after reporting a diagnostic.</returns>
        /// <remarks>
        /// Chronicle carries no comparison, length or pattern rule algebra - these rules are a Screenplay and
        /// application-layer concern, so ESM defines their meaning itself. Operands are concrete literals typed
        /// against the subject; a bound on text constrains its length and takes a whole-number operand.
        /// </remarks>
        SemanticValidationRule? BindValidationRule(ValidationRuleSyntax rule, SemanticId property, ValidationSubject subject)
        {
            var spelling = Spelling(rule.Rule);
            if (rule.Rule == ValidationRuleKind.Rule)
            {
                var name = (rule.Value as PathExpressionSyntax)?.Path ?? string.Empty;
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    rule.File is not null || rule.Code is not null
                        ? $"Validation rule '{name}' on {subject.Description} has an implementation body; code validation requires a constrained implementation attachment (#139)."
                        : $"Named validation rule '{name}' on {subject.Description} has no portable meaning - its logic lives outside the document.",
                    rule.Location);
                return null;
            }

            if (rule.Rule == ValidationRuleKind.Matches)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Validation rule 'matches' on {subject.Description} awaits a portable pattern definition (#209).", rule.Location);
                return null;
            }

            var kind = Kind(rule.Rule);
            if (kind == SemanticValidationRuleKind.NotEmpty)
            {
                return new(property, kind, null, rule.Message);
            }

            if (Inadmissible(kind, subject) is { } reason)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Validation rule '{spelling}' on {subject.Description} is not admitted: {reason}.", rule.Location);
                return null;
            }

            var operand = BindValidationOperand(rule, kind, subject, spelling);
            return operand is null ? null : new(property, kind, operand, rule.Message);
        }

        SemanticValue? BindValidationOperand(
            ValidationRuleSyntax rule,
            SemanticValidationRuleKind kind,
            ValidationSubject subject,
            string spelling)
        {
            var operand = rule.Value switch
            {
                LiteralExpressionSyntax literal => BindLiteral(literal),
                PathExpressionSyntax path when subject.Values.Contains(path.Path, StringComparer.Ordinal) => SemanticValue.Text(path.Path),
                _ => null
            };

            if (operand is null)
            {
                var reason = rule.Value is PathExpressionSyntax path && string.Equals(path.Path, "today", StringComparison.Ordinal)
                    ? $"'today' has no runtime value because {NoDateValue}"
                    : "the operand must be a literal value, and property references are not admitted";
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Validation rule '{spelling}' on {subject.Description} is not admitted: {reason}.", rule.Location);
                return null;
            }

            if (OperandMismatch(operand, kind, subject) is { } mismatch)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"The '{spelling}' operand on {subject.Description} {mismatch}.", rule.Location);
                return null;
            }

            return operand;
        }
    }
}
