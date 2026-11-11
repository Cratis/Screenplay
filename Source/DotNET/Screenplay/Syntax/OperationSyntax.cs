// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a named external system in the application's authoring contract.
/// </summary>
/// <param name="Name">The system name.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Location">The declaration location.</param>
public record SystemSyntax(string Name, string? Description, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the abilities the system declares, in declaration order.
    /// </summary>
    public IEnumerable<AbilitySyntax> Abilities { get; init; } = [];
}

/// <summary>
/// Represents one typed ability a system can be asked to perform, with its returned values, named outcomes and effect class.
/// </summary>
/// <param name="Name">The ability name, unique within its system ignoring case.</param>
/// <param name="EffectClass">The written effect class (<c>safe</c>, <c>idempotent</c> or <c>once</c>), or null when omitted, which means <c>once</c>.</param>
/// <param name="Inputs">The declared input shape.</param>
/// <param name="Returns">The returned values; empty when the ability returns nothing.</param>
/// <param name="Outcomes">The named business outcomes other than success.</param>
/// <param name="Location">The declaration location.</param>
public record AbilitySyntax(string Name, string? EffectClass, IEnumerable<PropertySyntax> Inputs, IEnumerable<PropertySyntax> Returns, IEnumerable<AbilityOutcomeSyntax> Outcomes, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether the ability's answer arrives after the invocation returns (<c>answers later</c>).
    /// </summary>
    public bool AnswersLater { get; init; }

    /// <summary>
    /// Gets the optional implementation hints (the <c>AbilityEffect</c> attachment).
    /// </summary>
    public ImplementationSyntax? Implementation { get; init; }

    /// <summary>
    /// Gets the optional selected implementation file.
    /// </summary>
    public FileReferenceSyntax? File { get; init; }

    /// <summary>
    /// Gets the optional inline implementation source.
    /// </summary>
    public CodeBlockSyntax? Code { get; init; }
}

/// <summary>
/// Represents a named business result of an ability other than success, such as a refusal the external system gives. It carries no payload.
/// </summary>
/// <param name="Name">The outcome name.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Location">The declaration location.</param>
public record AbilityOutcomeSyntax(string Name, string? Description, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a slice-owned operation intent, not an admitted executable operation.
/// </summary>
/// <param name="Name">The operation name.</param>
/// <param name="Uses">The named external system.</param>
/// <param name="Inputs">The declared input shape.</param>
/// <param name="Location">The declaration location.</param>
public record OperationSyntax(string Name, string Uses, IEnumerable<PropertySyntax> Inputs, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional intent description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional execution intent and source.
    /// </summary>
    public OperationPhaseSyntax? Execute { get; init; }

    /// <summary>
    /// Gets the optional compensation intent and source.
    /// </summary>
    public OperationPhaseSyntax? Compensate { get; init; }
}

/// <summary>
/// Represents one operation phase. Source belongs here; implementation holds hints only.
/// </summary>
/// <param name="Description">The optional description.</param>
/// <param name="File">The optional selected file.</param>
/// <param name="Code">The optional inline source.</param>
/// <param name="Implementation">The optional implementation intent.</param>
/// <param name="Location">The phase location.</param>
public record OperationPhaseSyntax(string? Description, FileReferenceSyntax? File, CodeBlockSyntax? Code, ImplementationSyntax? Implementation, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Enumerates standalone and inline declarations without treating references as declarations.
/// </summary>
public static class OperationDeclarations
{
    /// <summary>
    /// Gets the operation declarations owned by a slice.
    /// </summary>
    /// <param name="slice">The owning slice.</param>
    /// <returns>Standalone declarations followed by inline declarations.</returns>
    public static IEnumerable<OperationSyntax> In(SliceSyntax slice) => slice.Operations.Concat(
        slice.Commands.SelectMany(command => command.Produces).Select(production => production.InlineOperation).OfType<OperationSyntax>());
}
