// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>screen</c> declaration - the user interface of a slice.
/// </summary>
/// <param name="Name">The name of the screen.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the screen lives in an external file.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">directives</see> making up the screen body.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSyntax(
    string Name,
    FileReferenceSyntax? File,
    IEnumerable<ScreenDirectiveSyntax> Directives,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the base of every directive in a screen body.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ScreenDirectiveSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>data</c> directive binding a read model through a query.
/// </summary>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the bound read model.</param>
/// <param name="Query">The name of the query providing the data.</param>
/// <param name="By">The optional parameter the query is keyed by.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenDataSyntax(TypeRefSyntax Type, string Query, string? By, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents an <c>action</c> directive exposing a command on the screen.
/// </summary>
/// <param name="Command">The name of the command the action invokes.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="Navigate">The optional <see cref="ScreenNavigateSyntax"/> performed after the action.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenActionSyntax(string Command, string? Label, ScreenNavigateSyntax? Navigate, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>navigate to</c> directive.
/// </summary>
/// <param name="Screen">The name of the target screen.</param>
/// <param name="By">The optional parameter carried to the target screen.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenNavigateSyntax(string Screen, string? By, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>layout</c> reference filling the named slots of a declared layout.
/// </summary>
/// <param name="Name">The name of the referenced layout.</param>
/// <param name="Slots">The <see cref="ScreenSlotSyntax">slots</see> being filled.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenLayoutSyntax(string Name, IEnumerable<ScreenSlotSyntax> Slots, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a named slot within a layout reference.
/// </summary>
/// <param name="Name">The name of the slot.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">directives</see> filling the slot.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSlotSyntax(string Name, IEnumerable<ScreenDirectiveSyntax> Directives, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>section</c> directive grouping related directives.
/// </summary>
/// <param name="Name">The name of the section.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">directives</see> in the section.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSectionSyntax(string Name, IEnumerable<ScreenDirectiveSyntax> Directives, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>title</c> directive.
/// </summary>
/// <param name="Text">The title text.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenTitleSyntax(string Text, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>table</c> widget.
/// </summary>
/// <param name="Target">The collection or read model the table shows.</param>
/// <param name="Columns">The <see cref="ScreenColumnSyntax">columns</see> of the table.</param>
/// <param name="RowClick">The optional <see cref="ScreenNavigateSyntax"/> performed on row click.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenTableSyntax(
    string Target,
    IEnumerable<ScreenColumnSyntax> Columns,
    ScreenNavigateSyntax? RowClick,
    SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a column of a <c>table</c> widget.
/// </summary>
/// <param name="Property">The property shown in the column.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenColumnSyntax(string Property, string? Label, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>summary</c> widget.
/// </summary>
/// <param name="Target">The read model the summary shows.</param>
/// <param name="Fields">The <see cref="ScreenFieldSyntax">fields</see> of the summary.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSummarySyntax(string Target, IEnumerable<ScreenFieldSyntax> Fields, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a field of a <c>summary</c> widget.
/// </summary>
/// <param name="Property">The property shown in the field.</param>
/// <param name="Label">The display label.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenFieldSyntax(string Property, string Label, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an inline code block within a screen body.
/// </summary>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> holding the code.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenCodeSyntax(CodeBlockSyntax Code, SourceLocation Location) : ScreenDirectiveSyntax(Location);
