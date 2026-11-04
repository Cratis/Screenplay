// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // Clock operands are admitted in a command 'require' and 'produces when' condition, and as the right operand of a validation rule.
        static bool ClockPosition(string position) => string.Equals(position, "require", StringComparison.Ordinal) || string.Equals(position, "produces when", StringComparison.Ordinal);

        static bool IsClockName(string? name) => string.Equals(name, "today", StringComparison.Ordinal) || string.Equals(name, "now", StringComparison.Ordinal);

        static bool IsBareClock(ExpressionSyntax? expression, Dictionary<string, SemanticProperty> properties) =>
            expression is PathExpressionSyntax path && IsClockName(path.Path) && !properties.ContainsKey(path.Path);

        static bool IsClockExpression(ExpressionSyntax? expression, Dictionary<string, SemanticProperty> properties) =>
            expression is ClockOffsetExpressionSyntax or DateOfExpressionSyntax || IsBareClock(expression, properties);

        static bool UsesClockOperand(ComparisonConditionSyntax comparison, Dictionary<string, SemanticProperty> properties) =>
            comparison.LeftExpression is not null || IsClockExpression(comparison.Right, properties) ||
            (IsClockName(comparison.Left) && !properties.ContainsKey(comparison.Left));

        static bool IsClockRuleOperand(ExpressionSyntax? value, Dictionary<string, SemanticProperty> properties) =>
            value is ClockOffsetExpressionSyntax || IsBareClock(value, properties) ||
            value is DateOfExpressionSyntax { Operand: ClockOffsetExpressionSyntax };

        static bool IsRequiredInstant(ValidationSubject subject, SemanticPrimitiveType? required) =>
            !subject.IsCollection && subject.IsDate && (required is null || subject.Primitive == required);

        static bool IsDateComparisonRule(SemanticValidationRuleKind kind, ValidationSubject subject) =>
            SemanticDateComparison.IsComparison(kind) && subject.IsDate && !subject.IsCollection;

        static SemanticPrimitiveType ClockPrimitive(SemanticClockKind kind, bool dateOf) =>
            kind == SemanticClockKind.Today || dateOf ? SemanticPrimitiveType.Date : SemanticPrimitiveType.DateTime;

        static string Describe(SemanticPrimitiveType primitive) => primitive == SemanticPrimitiveType.Date ? "date" : "date-time";

        static SemanticComparisonOperator ComparisonOperatorFor(ComparisonOperator value) => value switch
        {
            ComparisonOperator.Equal => SemanticComparisonOperator.Equal,
            ComparisonOperator.NotEqual => SemanticComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan => SemanticComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterThanOrEqual => SemanticComparisonOperator.GreaterThanOrEqual,
            ComparisonOperator.LessThan => SemanticComparisonOperator.LessThan,
            _ => SemanticComparisonOperator.LessThanOrEqual
        };

        ClockSide? ClockOperandSide(ClockOffsetExpressionSyntax offset, bool dateOf)
        {
            var kind = offset.Clock == "today" ? SemanticClockKind.Today : SemanticClockKind.Now;
            var unit = offset.Unit.TrimEnd('s') switch
            {
                "hour" => SemanticClockUnit.Hour,
                "minute" => SemanticClockUnit.Minute,
                _ => SemanticClockUnit.Day
            };
            if (kind == SemanticClockKind.Today && unit != SemanticClockUnit.Day)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "'today' is offset in whole days; use 'now' for hours or minutes.", offset.Location);
                return null;
            }

            return new(new(default, null) { Clock = new SemanticClockOperand(kind, offset.Amount, unit) { DateOf = dateOf } }, ClockPrimitive(kind, dateOf));
        }

        // The clock binding of a condition the clock-free path declined, or null when it is not a clock comparison.
        SemanticComparison? BindClockConditionOrNull(ConditionSyntax syntax, string position, Dictionary<string, SemanticProperty> properties) =>
            IsClockComparison(syntax, position, properties, out var comparison) ? BindClockCondition(comparison, properties) : null;

        // The comparison the ordinary binding handles, or null when the clock binding does.
        ComparisonConditionSyntax? AdmitOrdinaryComparison(ConditionSyntax syntax, string position, Dictionary<string, SemanticProperty> properties) =>
            IsClockComparison(syntax, position, properties, out _) ? null : AdmitClockFreeComparison(syntax);

        bool IsClockComparison(ConditionSyntax syntax, string position, Dictionary<string, SemanticProperty> properties, [NotNullWhen(true)] out ComparisonConditionSyntax? comparison)
        {
            comparison = syntax as ComparisonConditionSyntax;
            return ClockPosition(position) && comparison is not null && UsesClockOperand(comparison, properties);
        }

        // Binds a command validation rule: a rule whose right operand is a clock operand is bound as a clock rule, any other the ordinary way.
        SemanticValidationRule? BindRule(ValidationRuleSyntax rule, ValidationSubject subject, Dictionary<string, SemanticProperty> properties, string? requirementId)
        {
            var property = properties[rule.Property].Id;
            return IsClockRuleOperand(rule.Value, properties)
                ? BindClockRule(rule, property, subject, properties)
                : BindValidationRule(rule, property, subject, requirementId);
        }

        SemanticComparison? BindClockCondition(ComparisonConditionSyntax comparison, Dictionary<string, SemanticProperty> properties)
        {
            var left = BindClockSide(comparison.LeftExpression ?? new PathExpressionSyntax(comparison.Left, comparison.Location), properties, comparison.Location);
            var right = BindClockSide(comparison.Right, properties, comparison.Location);
            if (left is null || right is null) return null;
            if (!AreComparable(left, right, comparison.Location)) return null;
            return new SemanticComparison(left.Operand, ComparisonOperatorFor(comparison.Operator), right.Operand);
        }

        bool AreComparable(ClockSide left, ClockSide right, SourceLocation location)
        {
            if (left.Operand.Clock is not null && right.Operand.Clock is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "A clock operand is compared with a command property, not with another clock operand.", location);
                return false;
            }

            if (left.Primitive == right.Primitive) return true;
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A {Describe(left.Primitive)} cannot be compared with a {Describe(right.Primitive)}; use 'date of' to reduce an instant to its day.", location);
            return false;
        }

        ClockSide? BindClockSide(ExpressionSyntax expression, Dictionary<string, SemanticProperty> properties, SourceLocation location)
        {
            switch (expression)
            {
                case ClockOffsetExpressionSyntax offset:
                    return ClockOperandSide(offset, dateOf: false);
                case PathExpressionSyntax path when IsBareClock(path, properties):
                    return ClockOperandSide(new ClockOffsetExpressionSyntax(path.Path, 0, "day", path.Location), dateOf: false);
                case DateOfExpressionSyntax { Operand: ClockOffsetExpressionSyntax inner }:
                    return ClockOperandSide(inner, dateOf: true);
                case DateOfExpressionSyntax { Operand: PathExpressionSyntax path } when IsBareClock(path, properties):
                    return ClockOperandSide(new ClockOffsetExpressionSyntax(path.Path, 0, "day", path.Location), dateOf: true);
                case DateOfExpressionSyntax { Operand: PathExpressionSyntax path } when properties.TryGetValue(path.Path, out var property):
                    return BindPropertySide(property, location, dateOf: true);
                case PathExpressionSyntax path when properties.TryGetValue(path.Path, out var property):
                    return BindPropertySide(property, location, dateOf: false);
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, "A clock comparison is between a required date or date-time command property and 'today', 'now', an offset of them, or 'date of' one of them.", expression.Location);
                    return null;
            }
        }

        ClockSide? BindPropertySide(SemanticProperty property, SourceLocation location, bool dateOf)
        {
            if (!ScalarInstant(property, location, dateOf ? SemanticPrimitiveType.DateTime : null)) return null;
            return new(new(property.Id, null) { DateOf = dateOf }, dateOf ? SemanticPrimitiveType.Date : CommandValidationSubject(property.Name, property.Type).Primitive);
        }

        bool ScalarInstant(SemanticProperty property, SourceLocation location, SemanticPrimitiveType? required)
        {
            var subject = CommandValidationSubject(property.Name, property.Type);
            if (property.Type.IsOptional)
            {
                OptionalConditionOperand(property, location);
                return false;
            }

            if (!IsRequiredInstant(subject, required))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Clock operand comparison on '{property.Name}' requires a {(required is null ? "date or date-time" : Describe(required.Value))} command property.", location);
                return false;
            }

            return true;
        }

        SemanticValidationRule? BindClockRule(ValidationRuleSyntax rule, SemanticId property, ValidationSubject subject, Dictionary<string, SemanticProperty> properties)
        {
            ValidateStringKey(rule.Message, rule.Location);
            var kind = Kind(rule.Rule);
            if (!IsDateComparisonRule(kind, subject))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A clock operand is the right operand of a comparison rule on a date or date-time property, not of '{Spelling(rule.Rule)}' on {subject.Description}.", rule.Location);
                return null;
            }

            var operand = BindClockSide(rule.Value!, properties, rule.Location);
            if (operand is null) return null;
            if (operand.Primitive != subject.Primitive)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A {Describe(operand.Primitive)} cannot be compared with {subject.Description}; use 'date of' to reduce an instant to its day.", rule.Location);
                return null;
            }

            return new(property, kind, null, rule.Message) { ClockOperand = operand.Operand.Clock, Severity = Severity(rule.Severity) };
        }

        sealed record ClockSide(SemanticConditionOperand Operand, SemanticPrimitiveType Primitive);
    }
}
