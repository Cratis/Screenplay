// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies the purpose of a code attachment that cannot be executed by the reference model.
/// </summary>
public enum SemanticImplementationRole
{
    /// <summary>A command's handler.</summary>
    CommandHandler,

    /// <summary>A command's code validation.</summary>
    CommandValidation,

    /// <summary>A concept's code validation.</summary>
    ConceptValidation,

    /// <summary>A named validation rule's predicate.</summary>
    RulePredicate,

    /// <summary>A policy's code predicate.</summary>
    PolicyPredicate,

    /// <summary>A reducer rule's transition.</summary>
    ReducerTransition,

    /// <summary>A query's performer.</summary>
    QueryPerformer,

    /// <summary>A file-backed constraint.</summary>
    ConstraintPredicate,

    /// <summary>A reaction trigger's implementation.</summary>
    ReactionEffect
}

/// <summary>
/// Describes an authored implementation that blocks executable admission without interpreting its code.
/// </summary>
/// <param name="Role">The attachment's role.</param>
/// <param name="Owner">The address of its owning declaration, or the nearest addressable parent.</param>
/// <param name="Member">The name of the nested rule or trigger, if any.</param>
/// <param name="Language">The inline language, or null for a file attachment.</param>
/// <param name="File">The authored file path, or null for inline code.</param>
/// <param name="ContentHash">SHA-256 of the inline code or the authored path, in lowercase hex.</param>
/// <param name="Source">The attachment location mapped to the owner's semantic identity.</param>
public sealed record SemanticImplementationRequirement(
    SemanticImplementationRole Role,
    SemanticAddress Owner,
    string? Member,
    string? Language,
    string? File,
    string ContentHash,
    SemanticSourceMapEntry Source);
