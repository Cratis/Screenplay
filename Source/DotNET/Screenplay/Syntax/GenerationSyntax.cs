// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a concept's declared value-generation strategy.
/// </summary>
/// <param name="Strategy">The closed strategy name.</param>
/// <param name="Location">The source anchor of the declaration.</param>
public sealed record GenerationSyntax(string Strategy, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the literal prefix, including the TypeID strategy argument.
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// Gets the declared encoded letter case.
    /// </summary>
    public string? Case { get; init; }

    /// <summary>
    /// Gets the NanoID size, when explicitly declared.
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// Gets the NanoID alphabet, when explicitly declared.
    /// </summary>
    public string? Alphabet { get; init; }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Gets the byte encoding.
    /// </summary>
    public string? Encoding { get; init; }

    /// <summary>
    /// Gets the preview pattern argument.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets the preview checksum modifier.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets the preview integer range, retaining exact source digits.
    /// </summary>
    public string? Range { get; init; }

    /// <summary>
    /// Gets the preview implementation name.
    /// </summary>
    public string? Rule { get; init; }

    /// <summary>
    /// Gets the generation rule's implementation file, distinct from concept provenance.
    /// </summary>
    public FileReferenceSyntax? File { get; init; }

    /// <summary>
    /// Gets the generation rule's tagged inline implementation.
    /// </summary>
    public CodeBlockSyntax? Code { get; init; }
}
