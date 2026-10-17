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
