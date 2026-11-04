// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        static void ValidateClockOperand(SemanticClockOperand clock)
        {
            if (!Enum.IsDefined(clock.Kind) || !Enum.IsDefined(clock.Unit)) throw new InvalidSemanticContract("Unknown clock kind or offset unit.");
            if (clock.Kind == SemanticClockKind.Today && clock.Unit != SemanticClockUnit.Day) throw new InvalidSemanticContract("A today offset must be in days.");
        }

        static SemanticPrimitiveType Primitive(SemanticClockOperand clock) =>
            clock.Kind == SemanticClockKind.Today || clock.DateOf ? SemanticPrimitiveType.Date : SemanticPrimitiveType.DateTime;

        // Date of reduces a DateTime to its date; it never applies to a Date.
        static bool IsClockComparable(SemanticPrimitiveType primitive, bool dateOf) =>
            SemanticDateComparison.IsDate(primitive) && (!dateOf || primitive == SemanticPrimitiveType.DateTime);

        static bool IsScalarClockRule(SemanticValidationRule validation, SemanticTypeReference propertyType) =>
            validation.Operand is null && !propertyType.IsCollection && SemanticDateComparison.IsComparison(validation.Kind);

        static SemanticPrimitiveType ClockPrimitive(SemanticConditionOperand operand, SemanticClockOperand clock)
        {
            if (operand.Property.IsSet || operand.DateOf) throw new InvalidSemanticContract("A clock operand must not also reference a property.");
            ValidateClockOperand(clock);
            return Primitive(clock);
        }

        // A clock comparison (decisions 0106 and 0107) compares required scalar command instants with a clock; both sides share one primitive.
        void ValidateClockCondition(SemanticComparison comparison, Dictionary<SemanticId, SemanticProperty> properties, bool allowed)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V37)) throw new InvalidSemanticContract("Clock operands require ESM v37.");
            if (!allowed) throw new InvalidSemanticContract("Clock operands are admitted only in command require and produces when conditions.");
            if (!Enum.IsDefined(comparison.Operator)) throw new InvalidSemanticContract("Unknown comparison operator.");
            if (ClockOperandPrimitive(comparison.Left, properties) != ClockOperandPrimitive(comparison.Right, properties))
                throw new InvalidSemanticContract("Clock comparison operands must share the Date or DateTime primitive.");
        }

        SemanticPrimitiveType ClockOperandPrimitive(SemanticConditionOperand operand, Dictionary<SemanticId, SemanticProperty> properties)
        {
            if (operand is null || operand.Value is not null || operand.Read is not null)
                throw new InvalidSemanticContract("A clock comparison operand must be a clock or a command property.");
            return operand.Clock is { } clock ? ClockPrimitive(operand, clock) : PropertyPrimitive(operand, properties);
        }

        SemanticPrimitiveType PropertyPrimitive(SemanticConditionOperand operand, Dictionary<SemanticId, SemanticProperty> properties)
        {
            if (!operand.Property.IsSet || !properties.TryGetValue(operand.Property, out var property)) throw new InvalidSemanticContract("Condition property is not a command property.");
            if (property.Type.IsCollection || property.Type.IsOptional) throw new InvalidSemanticContract("Clock comparisons require required scalar command properties.");
            var primitive = UnderlyingPrimitive(property.Type);
            if (!IsClockComparable(primitive, operand.DateOf))
                throw new InvalidSemanticContract("Clock comparisons require Date or DateTime command properties; date of reduces a DateTime.");
            return operand.DateOf ? SemanticPrimitiveType.Date : primitive;
        }

        // A clock validation rule compares a scalar Date or DateTime command property with a clock of the same primitive.
        void ValidateClockValidation(SemanticValidationRule validation, SemanticClockOperand clock, bool isConcept, SemanticTypeReference propertyType)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V37)) throw new InvalidSemanticContract("Clock operands require ESM v37.");
            if (isConcept || !IsScalarClockRule(validation, propertyType))
                throw new InvalidSemanticContract("A clock validation rule compares a scalar command property with a clock and carries no constant.");
            ValidateClockOperand(clock);
            if (UnderlyingPrimitive(propertyType) != Primitive(clock)) throw new InvalidSemanticContract("A clock validation rule must compare operands of the same Date or DateTime primitive.");
        }
    }
}
