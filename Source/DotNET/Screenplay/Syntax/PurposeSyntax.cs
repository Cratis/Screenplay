// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Records the controller's declarations about a processing purpose, without executable meaning.
/// </summary>
/// <param name="Name">The purpose name.</param>
/// <param name="Location">The declaration location.</param>
public record PurposeSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the description of the processing.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the Art. 6(1) lawful basis.
    /// </summary>
    public string? Basis { get; init; }

    /// <summary>
    /// Gets the legal reference accompanying the basis.
    /// </summary>
    public string? BasisReference { get; init; }

    /// <summary>
    /// Gets the legitimate-interest statement.
    /// </summary>
    public string? Interest { get; init; }

    /// <summary>
    /// Gets the Art. 9(2) processing condition.
    /// </summary>
    public string? Condition { get; init; }

    /// <summary>
    /// Gets the reference accompanying the condition.
    /// </summary>
    public string? ConditionReference { get; init; }

    /// <summary>
    /// Gets the Art. 10 authorization in law.
    /// </summary>
    public string? Authorization { get; init; }

    /// <summary>
    /// Gets the categories of data subjects.
    /// </summary>
    public IEnumerable<string> Subjects { get; init; } = [];

    /// <summary>
    /// Gets the declared retention period or criteria.
    /// </summary>
    public string? Retention { get; init; }

    /// <summary>
    /// Gets the declared recipients.
    /// </summary>
    public IEnumerable<string> Recipients { get; init; } = [];

    /// <summary>
    /// Gets the declared transfers and safeguards.
    /// </summary>
    public IEnumerable<PurposeTransferSyntax> Transfers { get; init; } = [];

    /// <summary>
    /// Gets the Art. 17(3) erasure exception.
    /// </summary>
    public string? ErasureException { get; init; }
}

/// <summary>
/// References a processing purpose on a module, feature or slice.
/// </summary>
/// <param name="Name">The referenced purpose.</param>
/// <param name="Location">The reference location.</param>
public record PurposeReferenceSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Records a third-country transfer and its declared safeguard.
/// </summary>
/// <param name="Destination">The destination.</param>
/// <param name="Safeguard">The safeguard.</param>
/// <param name="Location">The declaration location.</param>
public record PurposeTransferSyntax(string Destination, string Safeguard, SourceLocation Location) : SyntaxNode(Location);
