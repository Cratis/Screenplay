// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The single rule deciding whether a mapped source type can be assigned to a target type, shared by the semantic
/// validator (which refuses the model) and the binder (which locates the refusal at the mapping).
/// </summary>
internal static class SemanticTypeCompatibility
{
    /// <summary>
    /// Gets whether a value of <paramref name="source"/> can be assigned to <paramref name="target"/>: the same type, or
    /// the same value type widened from required to optional.
    /// </summary>
    /// <param name="source">The type of the mapped source.</param>
    /// <param name="target">The type of the mapped target property.</param>
    /// <returns><see langword="true"/> when the assignment is admitted.</returns>
    internal static bool IsCompatibleSource(SemanticTypeReference source, SemanticTypeReference target) =>
        SameType(source, target) || (SameValueType(source, target) && !source.IsOptional && target.IsOptional);

    /// <summary>
    /// Gets whether a value of one concept (or primitive) can be assigned to a different concept (or primitive) over the same
    /// underlying primitive, admitted from ESM v29 (decision 0098): ids that cross a context boundary keep their own concept on each
    /// side. The target keeps every rule it declares, so the source must prove them: the target concept declares no rule the
    /// source concept does not also declare, and personal data or secrets never flow into a concept that hides less.
    /// </summary>
    /// <param name="source">The type of the mapped source.</param>
    /// <param name="target">The type of the mapped target property.</param>
    /// <param name="concept">Finds a concept by identity.</param>
    /// <returns><see langword="true"/> when the conversion is admitted.</returns>
    internal static bool ConvertsConcept(SemanticTypeReference source, SemanticTypeReference target, Func<SemanticId, SemanticConcept?> concept)
    {
        if (SameValueType(source, target) || source.Kind == SemanticTypeReferenceKind.CompositeType || target.Kind == SemanticTypeReferenceKind.CompositeType ||
            source.IsCollection != target.IsCollection || (source.IsOptional && !target.IsOptional))
        {
            return false;
        }

        var sourceConcept = source.Kind == SemanticTypeReferenceKind.Concept ? concept(source.Target) : null;
        var targetConcept = target.Kind == SemanticTypeReferenceKind.Concept ? concept(target.Target) : null;
        if ((source.Kind == SemanticTypeReferenceKind.Concept && sourceConcept is null) || (target.Kind == SemanticTypeReferenceKind.Concept && targetConcept is null))
        {
            return false;
        }

        var sourcePrimitive = sourceConcept?.Primitive ?? source.Primitive;
        var targetPrimitive = targetConcept?.Primitive ?? target.Primitive;
        if (sourcePrimitive == SemanticPrimitiveType.Unknown || sourcePrimitive != targetPrimitive ||
            (sourceConcept is { Values.IsEmpty: false }) || (targetConcept is { Values.IsEmpty: false }))
        {
            return false;
        }

        if (sourceConcept?.Compliance is not null && targetConcept?.Compliance?.Equals(sourceConcept.Compliance) != true)
        {
            return false;
        }

        var proven = sourceConcept?.Validations ?? [];
        return targetConcept?.Validations.All(rule => proven.Any(held =>
            held.Kind == rule.Kind && Equals(held.Operand, rule.Operand) && held.Severity == rule.Severity && held.Name == rule.Name)) ?? true;
    }

    /// <summary>
    /// Gets whether two references name the same type, including collection and optional shape.
    /// </summary>
    /// <param name="left">The first reference.</param>
    /// <param name="right">The second reference.</param>
    /// <returns><see langword="true"/> when they are identical.</returns>
    internal static bool SameType(SemanticTypeReference left, SemanticTypeReference right) =>
        SameValueType(left, right) && left.IsCollection == right.IsCollection && left.IsOptional == right.IsOptional;

    /// <summary>
    /// Gets whether two references name the same value type, ignoring collection and optional shape.
    /// </summary>
    /// <param name="left">The first reference.</param>
    /// <param name="right">The second reference.</param>
    /// <returns><see langword="true"/> when they name the same primitive, concept or composite type.</returns>
    internal static bool SameValueType(SemanticTypeReference left, SemanticTypeReference right) =>
        left.Kind == right.Kind && left.Primitive == right.Primitive && left.Target == right.Target;
}
