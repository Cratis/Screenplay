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
public record SystemSyntax(string Name, string? Description, SourceLocation Location) : SyntaxNode(Location);

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
