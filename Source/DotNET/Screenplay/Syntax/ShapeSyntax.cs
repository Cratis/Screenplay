// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top-level <c>shape</c> declaration - the typed contract of the data a screen needs, tightening the
/// contract the compiler infers from what the screen reads.
/// </summary>
/// <remarks>
/// A shape has typed fields like a read model has properties, but it belongs to no slice and is never built by an
/// event: a slice that adopts a screen provides the shape's data from one of its queries.
/// </remarks>
/// <param name="Name">The name of the shape.</param>
/// <param name="Properties">The typed fields of the shape.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional prose description of the shape.</param>
public record ShapeSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location);

/// <summary>
/// Represents <c>uses screen &lt;Name&gt;</c> in a slice - the slice adopts a screen declared elsewhere and provides
/// the data the screen needs.
/// </summary>
/// <param name="Screen">The name of the adopted screen.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record UsesScreenSyntax(string Screen, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the data the slice provides to the screen's needs, in authored order.
    /// </summary>
    public IEnumerable<ScreenProvisionSyntax> Provisions { get; init; } = [];
}

/// <summary>
/// Represents <c>data &lt;Need&gt; via query &lt;Query&gt; [by &lt;param&gt;]</c> inside <c>uses screen</c> - the query
/// that provides one need of the adopted screen.
/// </summary>
/// <param name="Need">The shape name, or the alias, of the need the query provides.</param>
/// <param name="Query">The name of the providing query.</param>
/// <param name="By">The optional query parameter the screen supplies.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenProvisionSyntax(string Need, string Query, string? By, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the <c>&lt;field&gt; from &lt;path&gt;</c> lines that map a shape field to a read model path where the names differ.
    /// </summary>
    public IEnumerable<ScreenProvisionMappingSyntax> Mappings { get; init; } = [];
}

/// <summary>
/// Represents <c>&lt;field&gt; from &lt;path&gt;</c> inside a provision.
/// </summary>
/// <param name="Field">The field of the shape the screen reads.</param>
/// <param name="Path">The dotted path on the providing query's read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenProvisionMappingSyntax(string Field, string Path, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents <c>data &lt;Shape&gt;[] [as &lt;alias&gt;]</c> inside a screen - a need for data the screen reads but
/// does not source itself.
/// </summary>
/// <param name="Type">The shape the screen needs, a list when it is a collection.</param>
/// <param name="Alias">The optional alias that tells two needs of the same shape apart.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenNeedSyntax(TypeRefSyntax Type, string? Alias, SourceLocation Location) : ScreenDirectiveSyntax(Location)
{
    /// <summary>
    /// Gets the name a provision uses to name this need: the alias when there is one, otherwise the shape.
    /// </summary>
    public string Name => Alias ?? Type.Name;
}
