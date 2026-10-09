// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a property declaration on an event, command or type.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the property.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="IsIdentifier">Whether the property is marked with <c>identifier</c>, making it the event source id
/// of the command it is declared on.</param>
public record PropertySyntax(
    string Name,
    TypeRefSyntax Type,
    SourceLocation Location,
    bool IsIdentifier = false) : SyntaxNode(Location)
{
    /// <summary>
    /// The modifier that marks a command property as the event source id of the command.
    /// </summary>
    public const string IdentifierModifier = "identifier";

    /// <summary>
    /// Gets whether the command allocates this value rather than accepting it as input.
    /// </summary>
    public bool IsGenerated { get; init; }

    /// <summary>
    /// Gets whether this event property identifies the data subject (report-only metadata).
    /// </summary>
    public bool IsSubject { get; init; }

    /// <summary>Gets whether the authored property name had an explicit escape, not a structural modifier.</summary>
    [SourceSpanMetadata]
    public bool NameWasEscaped { get; init; }
}
