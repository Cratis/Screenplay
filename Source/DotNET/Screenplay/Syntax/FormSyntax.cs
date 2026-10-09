// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines how a command form's fields are generated.
/// </summary>
public enum FormGenerationMode
{
    /// <summary>
    /// The form does not declare a generation mode.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Generate fields from command metadata.
    /// </summary>
    Auto = 1,

    /// <summary>
    /// Use authored fields.
    /// </summary>
    Manual = 2
}

/// <summary>
/// Defines how a command-bound form arranges command properties into columns.
/// </summary>
public enum FormColumnMode
{
    /// <summary>
    /// The form does not declare a column mode.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Columns are inferred from the command and package defaults.
    /// </summary>
    Auto = 1,

    /// <summary>
    /// Columns are authored explicitly.
    /// </summary>
    Manual = 2
}

/// <summary>
/// Defines the Scene command-form width unit.
/// </summary>
public enum FormWidthUnitSyntax
{
    /// <summary>
    /// A CSS-style fractional unit.
    /// </summary>
    Fraction = 0,

    /// <summary>
    /// Pixels.
    /// </summary>
    Pixels = 1,

    /// <summary>
    /// Percent.
    /// </summary>
    Percent = 2,

    /// <summary>
    /// Automatic width.
    /// </summary>
    Auto = 3
}

/// <summary>
/// Represents a top level <c>form &lt;Name&gt; for &lt;Command&gt;</c> block - a named, command-bound input
/// surface that a build renders wherever that command is invoked.
/// </summary>
/// <param name="Name">The form's name.</param>
/// <param name="For">The name of the command the form submits.</param>
/// <param name="Populate">The <see cref="FormPopulateSource"/> that seeds the form's initial values, or <c>null</c> if not declared.</param>
/// <param name="Fields">The <see cref="FormFieldSyntax">fields</see> the form binds to the command's properties.</param>
/// <param name="OnSubmit">The optional <see cref="ScreenNavigateSyntax"/> performed after a successful submit.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A form never appears in a screen's directive tree - it is discovered by its <see cref="For"/> binding
/// wherever the named command is invoked, the same way a <c>ui profile</c> is discovered by a build rather
/// than referenced by a screen.
/// </remarks>
public record FormSyntax(
    string Name,
    string For,
    FormPopulateSource? Populate,
    IEnumerable<FormFieldSyntax> Fields,
    ScreenNavigateSyntax? OnSubmit,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the report-only description of this input surface.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the behaviors attached inline to the form. Every screen beneath it inherits them, additively
    /// with whatever is attached closer in.
    /// </summary>
    public IEnumerable<BehaviorSyntax> Behaviors { get; init; } = [];

    /// <summary>
    /// Gets the named behaviors attached to the form with <c>uses</c>.
    /// </summary>
    public IEnumerable<UsesBehaviorSyntax> UsedBehaviors { get; init; } = [];

    /// <summary>
    /// Gets the command-property column mode for renderers that arrange form fields in columns.
    /// </summary>
    public FormColumnMode ColumnMode { get; init; } = FormColumnMode.Unspecified;

    /// <summary>
    /// Gets how the form's fields are generated.
    /// </summary>
    public FormGenerationMode GenerationMode { get; init; } = FormGenerationMode.Unspecified;

    /// <summary>
    /// Gets the manually authored columns when <see cref="ColumnMode"/> is <see cref="FormColumnMode.Manual"/>.
    /// </summary>
    public IEnumerable<FormColumnSyntax> Columns { get; init; } = [];

    /// <summary>
    /// Gets the platform-neutral Scene command-form geometry.
    /// </summary>
    public CommandFormLayoutSyntax? Layout { get; init; }
}

/// <summary>
/// Represents the base of a form's <c>populate</c> declaration - where its initial values come from.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record FormPopulateSource(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>populate via query &lt;Query&gt; [by &lt;param&gt;]</c> declaration.
/// </summary>
/// <param name="Query">The name of the query providing the initial values.</param>
/// <param name="By">The optional parameter the query is keyed by.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FormPopulateViaQuerySyntax(string Query, string? By, SourceLocation Location) : FormPopulateSource(Location);

/// <summary>
/// Represents a <c>populate from item</c> declaration - reusing an item already bound in scope, such as the
/// row a table's <c>on row-click</c> navigated from.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FormPopulateFromItemSyntax(SourceLocation Location) : FormPopulateSource(Location);

/// <summary>
/// Represents a <c>field</c> declaration binding a form to one of its command's properties.
/// </summary>
/// <param name="Property">The command property the field binds to.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="From">The optional source property, when it differs from <see cref="Property"/>.</param>
/// <param name="ComposeUsing">The optional callback that computes the property's value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FormFieldSyntax(string Property, string? Label, string? From, string? ComposeUsing, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a manually authored form column.
/// </summary>
/// <param name="Property">The command property shown in the column.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FormColumnSyntax(string Property, string? Label, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a Scene 4.12 command-form layout block.
/// </summary>
/// <param name="Columns">The authored columns.</param>
/// <param name="Placements">The authored field placements.</param>
/// <param name="ColumnGap">The optional column gap.</param>
/// <param name="RowGap">The optional row gap.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in source text.</param>
public record CommandFormLayoutSyntax(
    IEnumerable<FormLayoutColumnSyntax> Columns,
    IEnumerable<FormFieldPlacementSyntax> Placements,
    FormWidthSyntax? ColumnGap,
    FormWidthSyntax? RowGap,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one Scene command-form layout column.
/// </summary>
/// <param name="Index">The one-based column index.</param>
/// <param name="Width">The optional authored width.</param>
/// <param name="MinWidth">The optional minimum width.</param>
/// <param name="MaxWidth">The optional maximum width.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in source text.</param>
public record FormLayoutColumnSyntax(int Index, FormWidthSyntax? Width, FormWidthSyntax? MinWidth, FormWidthSyntax? MaxWidth, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one Scene command-form field placement.
/// </summary>
/// <param name="Field">The command field.</param>
/// <param name="Row">The one-based row.</param>
/// <param name="Column">The one-based column.</param>
/// <param name="RowSpan">The optional row span.</param>
/// <param name="ColumnSpan">The optional column span.</param>
/// <param name="Width">The optional field width.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in source text.</param>
public record FormFieldPlacementSyntax(string Field, int Row, int Column, int? RowSpan, int? ColumnSpan, FormWidthSyntax? Width, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a Scene command-form width value.
/// </summary>
/// <param name="Unit">The width unit.</param>
/// <param name="Value">The numeric value for fraction, pixel and percent widths; <c>null</c> for auto.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in source text.</param>
public record FormWidthSyntax(FormWidthUnitSyntax Unit, double? Value, SourceLocation Location) : SyntaxNode(Location);
