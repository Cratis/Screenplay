// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents authoring metadata describing the application's additional caller details.
/// </summary>
/// <param name="Details">The typed caller details.</param>
/// <param name="Location">The source location.</param>
public record IdentitySyntax(IEnumerable<IdentityDetailSyntax> Details, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional description of the caller details.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents one typed caller detail with exactly one source.
/// </summary>
/// <param name="Name">The detail name.</param>
/// <param name="Type">The declared type.</param>
/// <param name="Source">The source of the detail.</param>
/// <param name="Location">The source location.</param>
public record IdentityDetailSyntax(string Name, TypeRefSyntax Type, IdentitySourceSyntax Source, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the source of a caller detail.
/// </summary>
/// <param name="Location">The source location.</param>
public abstract record IdentitySourceSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a caller detail sourced from a token claim.
/// </summary>
/// <param name="Claim">The opaque claim name.</param>
/// <param name="Location">The source location.</param>
public record ClaimIdentitySourceSyntax(string Claim, SourceLocation Location) : IdentitySourceSyntax(Location);

/// <summary>
/// Represents a caller detail sourced from a keyed, single-result query.
/// </summary>
/// <param name="Query">The query name.</param>
/// <param name="By">The caller-derived query key.</param>
/// <param name="Location">The source location.</param>
public record QueryIdentitySourceSyntax(string Query, ExpressionSyntax By, SourceLocation Location) : IdentitySourceSyntax(Location);

/// <summary>
/// Represents a caller detail sourced from inline code.
/// </summary>
/// <param name="Code">The opaque implementation.</param>
/// <param name="Location">The source location.</param>
public record CodeIdentitySourceSyntax(CodeBlockSyntax Code, SourceLocation Location) : IdentitySourceSyntax(Location);

/// <summary>
/// Represents a caller detail sourced from a file reference.
/// </summary>
/// <param name="File">The implementation reference.</param>
/// <param name="Location">The source location.</param>
public record FileIdentitySourceSyntax(FileReferenceSyntax File, SourceLocation Location) : IdentitySourceSyntax(Location);
