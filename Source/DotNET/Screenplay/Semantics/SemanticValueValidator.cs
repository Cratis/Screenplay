// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics;

static class SemanticValueRules
{
    internal static bool ValidProperties(ImmutableArray<SemanticPropertyValue> properties) =>
        !properties.IsDefault &&
        properties.All(_ => _ is { TargetProperty.IsSet: true, Value: not null }) &&
        properties.Select(_ => _.TargetProperty).Distinct().Count() == properties.Length;

    internal static bool ValidateText(string value, SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Text => true,
        SemanticPrimitiveType.Uuid => Guid.TryParseExact(value, "D", out var uuid) && uuid.ToString("D", CultureInfo.InvariantCulture) == value,
        SemanticPrimitiveType.Date => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        SemanticPrimitiveType.DateTime => DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
        _ => false
    };

    internal static bool AreEqual(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticNullValue, SemanticNullValue) => true,
        (SemanticTextValue leftText, SemanticTextValue rightText) =>
            string.Equals(leftText.Value, rightText.Value, StringComparison.Ordinal),
        (SemanticNumberValue leftNumber, SemanticNumberValue rightNumber) => leftNumber.Value == rightNumber.Value,
        (SemanticBooleanValue leftBoolean, SemanticBooleanValue rightBoolean) => leftBoolean.Value == rightBoolean.Value,
        (SemanticArrayValue leftArray, SemanticArrayValue rightArray) =>
            leftArray.Values.Length == rightArray.Values.Length &&
            leftArray.Values.Zip(rightArray.Values).All(_ => AreEqual(_.First, _.Second)),
        (SemanticCompositeValue leftObject, SemanticCompositeValue rightObject) =>
            leftObject.Properties.Length == rightObject.Properties.Length &&
            leftObject.Properties.All(leftProperty => rightObject.Properties.Any(rightProperty =>
                leftProperty.TargetProperty == rightProperty.TargetProperty && AreEqual(leftProperty.Value, rightProperty.Value))),
        _ => false
    };

    internal static InvalidSemanticContract Malformed() => new("A semantic value variant is malformed or unknown.");
}

sealed class SemanticValueValidator(
    IReadOnlyDictionary<SemanticId, SemanticConcept> concepts,
    IReadOnlyDictionary<SemanticId, SemanticCompositeType> types)
{
    public void Validate(SemanticValue value, SemanticTypeReference target, string description) =>
        Validate(value, target, description, 0, true);

    public void ValidatePattern(SemanticValue value, SemanticTypeReference target, string description) =>
        Validate(value, target, description, 0, false);

    public void ValidateVariant(SemanticValue value) => ValidateVariant(value, 0);

    public SemanticTypeReference? TypeOf(SemanticValue value)
    {
        ValidateVariant(value);
        return value switch
        {
            SemanticNullValue => null,
            SemanticTextValue => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
            SemanticNumberValue => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber),
            SemanticBooleanValue => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean),
            SemanticArrayValue or SemanticCompositeValue => throw new InvalidSemanticContract("A collection or composite literal requires a declared target type."),
            _ => throw SemanticValueRules.Malformed()
        };
    }

    void Validate(SemanticValue value, SemanticTypeReference target, string description, int depth, bool enforceEnumeration)
    {
        ValidateNode(value, depth);
        if (value is SemanticNullValue)
        {
            if (!target.IsOptional)
            {
                throw new InvalidSemanticContract($"A null {description} is incompatible with a required target.");
            }

            return;
        }

        if (target.IsCollection)
        {
            if (value is not SemanticArrayValue array)
            {
                throw new InvalidSemanticContract($"A scalar {description} is incompatible with a collection target.");
            }

            var elementType = target with { IsCollection = false, IsOptional = false };
            foreach (var element in array.Values)
            {
                Validate(element, elementType, $"element of {description}", depth + 1, enforceEnumeration);
            }

            return;
        }

        if (value is SemanticArrayValue)
        {
            throw new InvalidSemanticContract($"A collection {description} is incompatible with a scalar target.");
        }

        if (target.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            ValidateObject(value, target, description, depth, enforceEnumeration);
            return;
        }

        var primitive = UnderlyingPrimitive(target);
        var compatible = value switch
        {
            SemanticTextValue text => SemanticValueRules.ValidateText(text.Value, primitive),
            SemanticNumberValue number => primitive == SemanticPrimitiveType.DecimalNumber ||
                (primitive == SemanticPrimitiveType.WholeNumber && decimal.Truncate(number.Value) == number.Value),
            SemanticBooleanValue => primitive == SemanticPrimitiveType.Boolean,
            _ => false
        };
        if (!compatible)
        {
            throw new InvalidSemanticContract($"A {description} is incompatible with its target type.");
        }

        if (enforceEnumeration &&
            target.Kind == SemanticTypeReferenceKind.Concept &&
            concepts[target.Target].Values is { IsEmpty: false } declaredValues &&
            (value is not SemanticTextValue enumeratedText || !declaredValues.Contains(enumeratedText.Value, StringComparer.Ordinal)))
        {
            throw new InvalidSemanticContract($"A {description} targets an enumeration concept but is not a declared value.");
        }
    }

    void ValidateObject(
        SemanticValue value,
        SemanticTypeReference target,
        string description,
        int depth,
        bool enforceEnumeration)
    {
        if (value is not SemanticCompositeValue objectValue || !types.TryGetValue(target.Target, out var compositeType))
        {
            throw new InvalidSemanticContract($"A {description} is incompatible with its composite target type.");
        }

        var declared = compositeType.Properties.ToDictionary(_ => _.Id);
        var assigned = new HashSet<SemanticId>();
        foreach (var propertyValue in objectValue.Properties)
        {
            if (!assigned.Add(propertyValue.TargetProperty))
            {
                throw new InvalidSemanticContract($"Composite value property '{propertyValue.TargetProperty}' is duplicated.");
            }

            if (!declared.TryGetValue(propertyValue.TargetProperty, out var property))
            {
                throw new InvalidSemanticContract($"Composite value property '{propertyValue.TargetProperty}' is unknown for '{compositeType.Name}'.");
            }

            Validate(
                propertyValue.Value,
                property.Type,
                $"property '{property.Name}' of {description}",
                depth + 1,
                enforceEnumeration);
        }

        if (compositeType.Properties.Any(_ => !_.Type.IsOptional && !assigned.Contains(_.Id)))
        {
            throw new InvalidSemanticContract($"A {description} is missing one or more required properties of '{compositeType.Name}'.");
        }
    }

    void ValidateVariant(SemanticValue value, int depth)
    {
        ValidateNode(value, depth);
        switch (value)
        {
            case SemanticArrayValue array:
                foreach (var element in array.Values)
                {
                    ValidateVariant(element, depth + 1);
                }

                break;
            case SemanticCompositeValue objectValue:
                foreach (var property in objectValue.Properties)
                {
                    ValidateVariant(property.Value, depth + 1);
                }

                break;
        }
    }

    void ValidateNode(SemanticValue? value, int depth)
    {
        if (depth >= CanonicalJson.MaximumDepth)
        {
            throw new InvalidSemanticContract($"A semantic value exceeds the canonical maximum depth of {CanonicalJson.MaximumDepth}.");
        }

        if (value is null || !Enum.IsDefined(value.Kind) || value.Kind == SemanticValueKind.Unknown)
        {
            throw SemanticValueRules.Malformed();
        }

        var valid = value switch
        {
            SemanticNullValue => value.Kind == SemanticValueKind.Null,
            SemanticTextValue textValue => value.Kind == SemanticValueKind.Text && textValue.Value is not null,
            SemanticNumberValue => value.Kind == SemanticValueKind.Number,
            SemanticBooleanValue => value.Kind == SemanticValueKind.Boolean,
            SemanticArrayValue array => value.Kind == SemanticValueKind.Array && !array.Values.IsDefault && array.Values.All(_ => _ is not null),
            SemanticCompositeValue objectValue => value.Kind == SemanticValueKind.Composite && SemanticValueRules.ValidProperties(objectValue.Properties),
            _ => false
        };
        if (!valid)
        {
            throw SemanticValueRules.Malformed();
        }

        if (value is SemanticTextValue text)
        {
            CanonicalJson.RequireNfc(text.Value, "semantic text value");
        }
    }

    SemanticPrimitiveType UnderlyingPrimitive(SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive,
        SemanticTypeReferenceKind.Concept when concepts.TryGetValue(type.Target, out var concept) => concept.Primitive,
        _ => SemanticPrimitiveType.Unknown
    };
}
