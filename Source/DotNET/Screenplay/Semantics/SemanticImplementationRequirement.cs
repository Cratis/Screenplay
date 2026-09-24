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

/// <summary>Whether an attachment's content revision is known.</summary>
public enum SemanticAttachmentResolution
{
    /// <summary>The inline code or host-supplied file content was hashed.</summary>
    Resolved,

    /// <summary>The host has not supplied the file content; its path is not a revision.</summary>
    UnresolvedFile
}

/// <summary>
/// Describes an authored implementation attachment without interpreting its code.
/// </summary>
/// <param name="Role">The attachment's role.</param>
/// <param name="Owner">The address of its owning declaration, or the nearest addressable parent.</param>
/// <param name="Member">A stable discriminator for the nested rule or trigger, if any. Code validations use their ordinal among code validations of the owner; reordering those blocks changes their ids. Repeated identical members use an occurrence suffix.</param>
/// <param name="Language">The inline language, or null for a file attachment.</param>
/// <param name="File">The authored file path, or null for inline code.</param>
/// <param name="ContentHash">SHA-256 of inline or host-supplied file content, or empty when unresolved.</param>
/// <param name="Source">The attachment location mapped to the owner's semantic identity.</param>
public sealed record SemanticImplementationRequirement(
    SemanticImplementationRole Role,
    SemanticAddress Owner,
    string? Member,
    string? Language,
    string? File,
    string ContentHash,
    SemanticSourceMapEntry Source)
{
    /// <summary>Stable identity derived from the owner's semantic identity, role and member, never the file path.</summary>
    public string RequirementId { get; init; } = string.Empty;

    /// <summary>The version of the role's context contract.</summary>
    public uint ContextVersion { get; init; } = 1;

    /// <summary>The version of the role's result contract.</summary>
    public uint ResultVersion { get; init; } = 1;

    /// <summary>The capability the target provider must admit; Screenplay does not enforce provider allowlists.</summary>
    public string RequiredCapability { get; init; } = string.Empty;

    /// <summary>Whether the attachment's content was available for hashing.</summary>
    public SemanticAttachmentResolution AttachmentResolution { get; init; } = SemanticAttachmentResolution.Resolved;
}
