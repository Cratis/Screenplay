// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>sample data &lt;Name&gt;</c> block declaring a pack of demo rows a design surface or preview shows
/// instead of a backend.
/// </summary>
/// <param name="Name">The pack's name. Scene's pack id is its kebab-case form (<c>NordicDemo</c> becomes <c>nordic-demo</c>).</param>
/// <param name="Title">The display title, or <c>null</c> when the pack takes its name.</param>
/// <param name="Locale">The BCP 47 locale tag of the pack's data (<c>nb-NO</c>), or <c>null</c> for the default (<c>en-GB</c>).</param>
/// <param name="Sets">The <see cref="SampleDataSetSyntax">sets</see> of rows, one per shape.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Sample data is design and preview time only. It is not part of the executable semantic model and is never emitted
/// into backend or production output: the compiler reports it as deferred (<c>PLAY0269</c>) when binding. The application's
/// active pack is chosen once, in the <see cref="UiProfileSyntax.SampleData"/> of the <c>ui profile</c>.
/// </remarks>
public record SampleDataPackSyntax(
    string Name,
    string? Title,
    string? Locale,
    IEnumerable<SampleDataSetSyntax> Sets,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>shape &lt;Name&gt;</c> block of a <see cref="SampleDataPackSyntax"/>: the rows for one shape.
/// </summary>
/// <param name="Shape">The shape's name. It matches the read model of the same name, or the read model a <c>shape ... maps to ...</c> line maps it to.</param>
/// <param name="Rows">The <see cref="SampleDataRowSyntax">rows</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SampleDataSetSyntax(string Shape, IEnumerable<SampleDataRowSyntax> Rows, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>row</c> block of a <see cref="SampleDataSetSyntax"/>: one row's field values.
/// </summary>
/// <param name="Fields">The <see cref="SampleDataFieldSyntax">field values</see>, in authored order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SampleDataRowSyntax(IEnumerable<SampleDataFieldSyntax> Fields, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>&lt;field&gt; = &lt;literal&gt;</c> line of a <see cref="SampleDataRowSyntax"/>.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="Kind">The kind of literal the value is.</param>
/// <param name="Value">The value: the unescaped text of a string, the authored token of a number, <c>true</c> or <c>false</c>, or <c>null</c> for the null literal.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SampleDataFieldSyntax(string Name, SampleDataValueKind Kind, string? Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// The kind of literal a sample data field value is.
/// </summary>
public enum SampleDataValueKind
{
    /// <summary>A quoted string.</summary>
    String = 0,

    /// <summary>A number, kept as its authored token.</summary>
    Number = 1,

    /// <summary>The literal <c>true</c> or <c>false</c>.</summary>
    Boolean = 2,

    /// <summary>The <c>null</c> literal.</summary>
    Null = 3
}
