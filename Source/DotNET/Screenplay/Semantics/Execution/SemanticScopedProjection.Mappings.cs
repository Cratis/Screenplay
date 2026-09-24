// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Evaluates keys, values and mappings of a scoped projection.
/// </summary>
internal sealed partial class SemanticScopedProjection
{
    // Chronicle converts with Convert.ChangeType, which rounds half to even for an integral target.
    static decimal Coerce(decimal value, bool whole) => whole ? Math.Round(value, MidpointRounding.ToEven) : value;

    void Apply(
        Dictionary<SemanticId, SemanticValue> target,
        ImmutableArray<SemanticProjectionMapping> mappings,
        Dictionary<SemanticId, SemanticProperty> targets,
        SemanticFact fact)
    {
        foreach (var mapping in mappings)
        {
            var property = targets[mapping.Target[0]];
            foreach (var segment in mapping.Target[1..])
            {
                property = _types[property.Type.Target].Properties.Single(_ => _.Id == segment);
            }

            // Absent intermediates of the target path are created (Chronicle ExpandoObjectExtensions.EnsurePath).
            ModifyNested(target, mapping.Target[..^1], 0, true, container =>
            {
                var last = mapping.Target[^1];
                if (Operate(mapping, property, container.GetValueOrDefault(last), fact) is { } value)
                {
                    container[last] = value;
                }
            });
        }
    }

    SemanticValue? Operate(SemanticProjectionMapping mapping, SemanticProperty property, SemanticValue? current, SemanticFact fact)
    {
        switch (mapping.Operation)
        {
            // A null source sets the target to null (Chronicle PropertyMappers.cs:26-37).
            case SemanticProjectionOperation.Set:
                return Evaluate(mapping.Source!, fact);
            case SemanticProjectionOperation.Clear:
                return SemanticValue.Null;
        }

        // Arithmetic is typed by the read-model target; a missing target is seeded with 0 and both operands are converted to the
        // target type before the operation (Chronicle PropertyMappers.cs:47-219). A cleared value reads back as missing.
        var whole = Primitive(property.Type) == SemanticPrimitiveType.WholeNumber;
        if (current is not (null or SemanticNullValue or SemanticNumberValue))
        {
            Fail($"Projection '{projection.Name}' cannot do arithmetic on the non-numeric value of '{property.Name}'.");
            return null;
        }

        var operand = mapping.Operation is SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement
            ? SemanticValue.Number(1)
            : Evaluate(mapping.Source!, fact);
        if (operand is not SemanticNumberValue number)
        {
            // Chronicle's Convert.ChangeType throws for a null or non-numeric operand, failing the observer.
            Fail($"Projection '{projection.Name}' arithmetic on '{property.Name}' needs a numeric operand.");
            return null;
        }

        var left = Coerce(current is SemanticNumberValue value ? value.Value : 0m, whole);
        var right = Coerce(number.Value, whole);
        return SemanticValue.Number(mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Increment ? left + right : left - right);
    }

    SemanticValue? Key(SemanticProjectionKey key)
    {
        switch (key)
        {
            case SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }:
                return EventSource();
            case SemanticProjectionValueKey value when Evaluate(value.Value, _fact) is { } resolved and not SemanticNullValue:
                return resolved;
            case SemanticProjectionCompositeKey composite:
                var parts = composite.Parts.Select(_ => new SemanticPropertyValue(_.Property, Evaluate(_.Value, _fact) ?? SemanticValue.Null)).ToImmutableArray();
                return _failure is null ? SemanticValue.Composite(parts) : null;
            default:
                Fail($"Projection '{projection.Name}' key resolved to no value.");
                return null;
        }
    }

    SemanticValue? EventSource()
    {
        var source = _fact.Context?.EventSource.Value ?? _fact.Destination;
        if (source is SemanticNullValue)
        {
            Fail($"Projection '{projection.Name}' keys on the event source identity, which this fact does not carry.");
            return null;
        }

        return source;
    }

    SemanticValue? Evaluate(SemanticProjectionValue value, SemanticFact fact)
    {
        switch (value)
        {
            case SemanticProjectionLiteral literal:
                return literal.Value;
            case SemanticProjectionEventSourceIdentity:
            case SemanticProjectionEventContextValue { Path: "eventSourceId" }:
                return fact.Context?.EventSource.Value ?? fact.Destination;
            case SemanticProjectionEventProperty property:
                var values = fact.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
                var current = values.GetValueOrDefault(property.Path[0], SemanticValue.Null);
                foreach (var segment in property.Path[1..])
                {
                    current = current is SemanticCompositeValue composite
                        ? composite.Properties.FirstOrDefault(_ => _.TargetProperty == segment)?.Value ?? SemanticValue.Null
                        : SemanticValue.Null;
                }

                return current;
            default:
                Fail($"Projection '{projection.Name}' reads a value the reference evaluator has no occurrence context for.");
                return null;
        }
    }

    SemanticPrimitiveType Primitive(SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive,
        SemanticTypeReferenceKind.Concept => plan.Model.Application.Concepts.Single(_ => _.Id == type.Target).Primitive,
        _ => SemanticPrimitiveType.Unknown
    };
}
