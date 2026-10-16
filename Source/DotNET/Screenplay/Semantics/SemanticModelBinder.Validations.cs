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
    /// <param name="IsStringConcept">Whether the subject is the value of a String concept, not a command property.</param>
    sealed record ValidationSubject(
        string Description,
        SemanticPrimitiveType Primitive,
        bool IsCollection,
        ImmutableArray<string> Values,
        bool IsStringConcept = false)
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
            SemanticValidationRuleKind.Checksum when !subject.IsStringConcept =>
                "checksum requires a String concept",
            SemanticValidationRuleKind.Checksum => null,
            SemanticValidationRuleKind.NotEmpty when !subject.IsCollection && subject.Primitive != SemanticPrimitiveType.Text =>
                "not empty requires text or a collection",
            SemanticValidationRuleKind.NotEmpty => null,
            _ when subject.IsDate => NoDateValue,
            SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum when subject.IsCollection || !(subject.IsNumber || subject.IsText) =>
                "a bound constrains the length of one text value or the size of one number",
            SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual
                when subject.IsCollection || !(subject.IsNumber || subject.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean) =>
                "equality compares one text, enumeration, number or boolean value",
            SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
                SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual when subject.IsCollection || !subject.IsNumber =>
                "ordering compares one whole or decimal number",
            SemanticValidationRuleKind.Length when subject.IsCollection || !subject.IsText =>
                "a length measures one text value that is not an enumeration",
            SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
                SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith when subject.IsCollection || !subject.IsText =>
                "text comparison requires one text value that is not an enumeration",
            SemanticValidationRuleKind.Matches when subject.IsCollection || !subject.IsText =>
                "matches requires one text value that is not an enumeration",
            SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual when !subject.IsCollection || !subject.IsNumber =>
                "'all' quantifies over a collection of whole or decimal numbers",
            _ => null
        };

        static string? OperandMismatch(SemanticValue operand, SemanticValidationRuleKind kind, ValidationSubject subject)
        {
            if (kind == SemanticValidationRuleKind.Checksum)
            {
                return operand is SemanticTextValue algorithm && (algorithm.Value == "luhn" || algorithm.Value == "mod11" || algorithm.Value == "mod97") ? null : "requires luhn, mod11 or mod97";
            }
            if (operand is SemanticNullValue)
            {
                return "cannot be null - use 'not empty' to require a value";
            }

            if (kind == SemanticValidationRuleKind.Length || (subject.IsText && kind is SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum))
            {
                return operand is SemanticNumberValue { Value: >= 0 } length && decimal.Truncate(length.Value) == length.Value
                    ? null
                    : "must be a non-negative whole number because it is a text length";
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
            ValidationRuleKind.Length => SemanticValidationRuleKind.Length,
            ValidationRuleKind.Matches => SemanticValidationRuleKind.Matches,
            ValidationRuleKind.Contains => SemanticValidationRuleKind.Contains,
            ValidationRuleKind.NotContains => SemanticValidationRuleKind.NotContains,
            ValidationRuleKind.StartsWith => SemanticValidationRuleKind.StartsWith,
            ValidationRuleKind.EndsWith => SemanticValidationRuleKind.EndsWith,
            ValidationRuleKind.Checksum => SemanticValidationRuleKind.Checksum,
            ValidationRuleKind.AllGreaterThan => SemanticValidationRuleKind.AllGreaterThan,
            ValidationRuleKind.AllGreaterThanOrEqual => SemanticValidationRuleKind.AllGreaterThanOrEqual,
            _ => SemanticValidationRuleKind.Unknown
        };

        static SemanticValidationSeverity Severity(ValidationSeverity severity) => severity switch
        {
            ValidationSeverity.Error => SemanticValidationSeverity.Error,
            ValidationSeverity.Information => SemanticValidationSeverity.Information,
            ValidationSeverity.Warning => SemanticValidationSeverity.Warning,
            _ => throw new InvalidSemanticContract($"Unknown validation severity '{severity}'.")
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
            ValidationRuleKind.Contains => "contains",
            ValidationRuleKind.NotContains => "not contains",
            ValidationRuleKind.StartsWith => "starts with",
            ValidationRuleKind.EndsWith => "ends with",
            ValidationRuleKind.Checksum => "checksum",
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
            concept.IsEnum ? [.. concept.Values] : [],
            concept.Type == "String");

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
        SemanticValidationRule? BindValidationRule(ValidationRuleSyntax rule, SemanticId property, ValidationSubject subject, string? requirementId = null)
        {
            ValidateStringKey(rule.Message, rule.Location);
            var spelling = Spelling(rule.Rule);
            if (rule.Rule == ValidationRuleKind.Rule)
            {
                var name = (rule.Value as PathExpressionSyntax)?.Path ?? string.Empty;
                if (requirementId is not null)
                {
                    UsesV3 = true;
                    return new(property, SemanticValidationRuleKind.RulePredicate, null, rule.Message)
                    {
                        Name = name,
                        RequirementId = requirementId,
                        Severity = Severity(rule.Severity)
                    };
                }

                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Named validation rule '{name}' on {subject.Description} has no portable meaning - its logic lives outside the document.",
                    rule.Location);
                return null;
            }

            var kind = Kind(rule.Rule);
            if (subject.IsDate && property.IsSet && SemanticDateComparison.IsComparison(kind) && rule.Value is LiteralExpressionSyntax) return BindDateRule(rule, property, subject);
            if (Inadmissible(kind, subject) is { } reason)
            {
                Error(
                    kind == SemanticValidationRuleKind.NotEmpty ? DiagnosticCodes.InvalidSemanticBinding : DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Validation rule '{spelling}' on {subject.Description} is not admitted: {reason}.",
                    rule.Location);
                return null;
            }

            if (kind == SemanticValidationRuleKind.NotEmpty)
            {
                return new(property, kind, null, rule.Message) { Severity = Severity(rule.Severity) };
            }

            if (kind == SemanticValidationRuleKind.Matches)
            {
                string pattern;
                switch (rule.Value)
                {
                    case LiteralExpressionSyntax { Value: string text }:
                        pattern = text;
                        break;
                    case PathExpressionSyntax { Path: "email" }:
                        pattern = SemanticMatchPattern.Email;
                        break;
                    case PathExpressionSyntax path:
                        Error(DiagnosticCodes.UnknownMatchPattern, $"Named match pattern '{path.Path}' on {subject.Description} is not defined; use 'email' or a quoted ECMAScript regular expression.", rule.Location);
                        return null;
                    default:
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"The 'matches' operand on {subject.Description} must be a quoted ECMAScript regular expression or the named pattern 'email'.", rule.Location);
                        return null;
                }

                var check = PortableMatchPattern.Check(pattern);
                if (check.Replacement is { } replacement)
                {
                    Warning(DiagnosticCodes.DeprecatedStudioTextRulePattern, check.Description, rule.Location);
                    return new(property, replacement.Kind, SemanticValue.Text(replacement.Value), rule.Message) { Severity = Severity(rule.Severity) };
                }

                var parsed = SemanticMatchPattern.Parse(pattern);
                if (!parsed.Accepted)
                {
                    Error(
                        parsed.Invalid ? DiagnosticCodes.InvalidMatchPattern : DiagnosticCodes.PatternOutsidePortableMatchDialect,
                        $"Match pattern on {subject.Description}: {parsed.Description}",
                        MatchPatternLocation(rule, parsed.Offset));
                    return null;
                }

                return new(property, kind, SemanticValue.Text(pattern), rule.Message) { Severity = Severity(rule.Severity) };
            }

            var operand = BindValidationOperand(rule, kind, subject, spelling);
            return operand is null ? null : new(property, kind, operand, rule.Message) { Severity = Severity(rule.Severity) };
        }

        SourceLocation MatchPatternLocation(ValidationRuleSyntax rule, int patternOffset)
        {
            if (DocumentAt(rule.Location) is not { } document) return rule.Location;
            var start = OffsetAt(document.Text, rule.Location);
            var end = document.Text.IndexOf('\n', start);
            if (end < 0) end = document.Text.Length;
            var quote = document.Text.IndexOf('"', start, end - start);
            if (quote < 0) return rule.Location;
            var offset = quote + 1;
            for (var decoded = 0; decoded < patternOffset && offset < end; decoded++, offset++)
            {
                if (document.Text[offset] == '\\' && offset + 1 < end && "\\\"nrt".Contains(document.Text[offset + 1], StringComparison.Ordinal)) offset++;
            }

            return rule.Location with { Column = rule.Location.Column + (offset - start) };
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
