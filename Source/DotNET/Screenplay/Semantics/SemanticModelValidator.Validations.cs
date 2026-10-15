// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        static void RejectIncompatibleValidationKind(SemanticValidationRuleKind kind, ValidatedShape shape)
        {
            RejectIncompatibleTextKind(kind, shape);
            RejectIncompatibleBoundKind(kind, shape);
            RejectIncompatibleEqualityKind(kind, shape);
            RejectIncompatibleNumericKind(kind, shape);
        }

        static void RejectIncompatibleTextKind(SemanticValidationRuleKind kind, ValidatedShape shape)
        {
            switch (kind)
            {
                case SemanticValidationRuleKind.Length when !shape.Scalar || !shape.Text:
                    throw new InvalidSemanticContract("Length validation requires a scalar non-enumerated text value.");
                case SemanticValidationRuleKind.Matches when !shape.Scalar || !shape.Text:
                    throw new InvalidSemanticContract("Matches validation requires a scalar non-enumerated text value.");
            }
        }

        static void RejectIncompatibleBoundKind(SemanticValidationRuleKind kind, ValidatedShape shape)
        {
            switch (kind)
            {
                case SemanticValidationRuleKind.NotEmpty when shape.Scalar && shape.Primitive != SemanticPrimitiveType.Text:
                    throw new InvalidSemanticContract("Not-empty validation requires text or a collection.");
                case SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum when !shape.Scalar || !(shape.Number || shape.Text):
                    throw new InvalidSemanticContract("Minimum and maximum validation require a scalar number or a scalar non-enumerated text value.");
            }
        }

        static void RejectIncompatibleEqualityKind(SemanticValidationRuleKind kind, ValidatedShape shape)
        {
            if (kind is not (SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual))
            {
                return;
            }

            var comparable = shape.Number || shape.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean;
            if (!shape.Scalar || !comparable)
            {
                throw new InvalidSemanticContract("Equality validation requires a scalar text, enumeration, number or boolean value.");
            }
        }

        static void RejectIncompatibleNumericKind(SemanticValidationRuleKind kind, ValidatedShape shape)
        {
            switch (kind)
            {
                case SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
                    SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual when !shape.Scalar || !shape.Number:
                    throw new InvalidSemanticContract("Ordering validation requires a scalar number.");
                case SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual when shape.Scalar || !shape.Number:
                    throw new InvalidSemanticContract("Quantified validation requires a collection of numbers.");
            }
        }

        static bool BindsLength(SemanticValidationRuleKind kind, bool text) =>
            kind == SemanticValidationRuleKind.Length ||
            (text && kind is SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum);

        static SemanticTypeReference OperandType(SemanticValidationRuleKind kind, bool bindsLength, SemanticTypeReference propertyType) => kind switch
        {
            _ when bindsLength => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber),
            SemanticValidationRuleKind.Matches => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
            SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual =>
                propertyType with { IsCollection = false, IsOptional = false },
            _ => propertyType
        };

        static void ValidateMatchesPattern(SemanticValue operand)
        {
            if (operand is not SemanticTextValue pattern)
            {
                throw new InvalidSemanticContract("Matches validation requires a text pattern.");
            }

            var parsed = SemanticMatchPattern.Parse(pattern.Value);
            if (parsed.Invalid)
            {
                throw new InvalidSemanticContract($"Matches validation has an invalid regular expression: {parsed.Description}");
            }

            // Older ESM may contain a refused construct. Plan compilation and generation classify it explicitly.
        }

        void ValidateValidation(
            SemanticValidationRule validation,
            bool isConcept,
            SemanticTypeReference propertyType)
        {
            ValidateValidationHeader(validation, isConcept);
            if (validation.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation)
            {
                ValidatePredicateRule(validation, isConcept);
                return;
            }

            if (validation.Name is not null || validation.RequirementId is not null)
            {
                throw new InvalidSemanticContract("A declarative rule cannot carry a predicate attachment.");
            }

            var requiresOperand = validation.Kind != SemanticValidationRuleKind.NotEmpty;
            if (requiresOperand != (validation.Operand is not null))
            {
                throw new InvalidSemanticContract($"Validation rule '{validation.Kind}' has an invalid operand shape.");
            }

            if (validation.Kind == SemanticValidationRuleKind.Checksum)
            {
                if (!_semanticVersion.IsAtLeast(SemanticVersion.V21) || !isConcept || propertyType.IsCollection ||
                    UnderlyingPrimitive(propertyType) != SemanticPrimitiveType.Text || IsEnumeration(propertyType) ||
                    validation.Operand is not SemanticTextValue algorithm || !(algorithm.Value == "luhn" || algorithm.Value == "mod11" || algorithm.Value == "mod97"))
                {
                    throw new InvalidSemanticContract("Checksum validation requires a String concept, luhn/mod11/mod97 and ESM v21.");
                }
                return;
            }

            var primitive = UnderlyingPrimitive(propertyType);
            var shape = new ValidatedShape(
                primitive,
                !propertyType.IsCollection,
                primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber,
                primitive == SemanticPrimitiveType.Text && !IsEnumeration(propertyType));
            RejectIncompatibleValidationKind(validation.Kind, shape);
            if (validation.Operand is not null)
            {
                ValidateValidationOperand(validation, propertyType, shape.Text);
            }
        }

        void ValidateValidationHeader(SemanticValidationRule validation, bool isConcept)
        {
            RejectNull(validation, "validation rule");
            ValidateEnum(validation.Kind, SemanticValidationRuleKind.Unknown, "validation rule kind");
            ValidateEnum(validation.Severity, (SemanticValidationSeverity)(-1), "validation severity");
            if (isConcept && validation.Property.IsSet)
            {
                throw new InvalidSemanticContract("A concept validation must use the implicit concept value.");
            }

            if (validation.Message?.Length == 0)
            {
                throw new InvalidSemanticContract("A validation message cannot be empty.");
            }
        }

        void ValidatePredicateRule(SemanticValidationRule validation, bool isConcept)
        {
            var lacksIdentity = string.IsNullOrWhiteSpace(validation.Name) || string.IsNullOrWhiteSpace(validation.RequirementId);
            var misplacedCode = validation.Kind == SemanticValidationRuleKind.CodeValidation && !isConcept;
            var unsupported = !_semanticVersion.IsAtLeast(SemanticVersion.V3) || validation.Operand is not null;
            if (unsupported || misplacedCode || lacksIdentity)
            {
                throw new InvalidSemanticContract("A rule predicate requires ESM v3, a name and a requirement identity, but no operand.");
            }
        }

        void ValidateValidationOperand(SemanticValidationRule validation, SemanticTypeReference propertyType, bool text)
        {
            if (validation.Operand is SemanticNullValue && validation.Kind is not (SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual))
            {
                throw new InvalidSemanticContract($"Validation rule '{validation.Kind}' cannot use a null operand.");
            }

            // A length rule and a bound on text constrain its length, so the operand is a length, not text.
            var bindsLength = BindsLength(validation.Kind, text);
            ValidateValue(validation.Operand!, OperandType(validation.Kind, bindsLength, propertyType), "validation operand");
            if (validation.Kind == SemanticValidationRuleKind.Matches)
            {
                ValidateMatchesPattern(validation.Operand!);
            }

            if (bindsLength && validation.Operand is SemanticNumberValue { Value: < 0 })
            {
                throw new InvalidSemanticContract($"Validation rule '{validation.Kind}' bounds a length and cannot use a negative operand.");
            }
        }

        // What a validation rule is applied to: the underlying primitive, whether it is a single value, a number or plain text.
        readonly record struct ValidatedShape(SemanticPrimitiveType Primitive, bool Scalar, bool Number, bool Text);
    }
}
