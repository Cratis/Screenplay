// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>design system &lt;Name&gt;</c> block: the contract an application's themes implement - its modes,
/// the token roles by category, the components built from them, the words it speaks with and the rules it holds.
/// </summary>
/// <param name="Name">The design system's name.</param>
/// <param name="Modes">The modes (<c>light</c>, <c>dark</c>, ...) every theme gives its color tokens a value in.</param>
/// <param name="Tokens">The token roles, grouped by <see cref="DesignTokenCategorySyntax">category</see>.</param>
/// <param name="Components">The <see cref="DesignComponentSyntax">components</see> the design system defines.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// One design system per application. A theme assigns values to its tokens and never redefines a component part.
/// </remarks>
public record DesignSystemSyntax(
    string Name,
    IEnumerable<string> Modes,
    IEnumerable<DesignTokenCategorySyntax> Tokens,
    IEnumerable<DesignComponentSyntax> Components,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the name of the <c>default theme</c>, or <c>null</c> when none is declared.
    /// </summary>
    public string? DefaultTheme { get; init; }

    /// <summary>
    /// Gets the <c>words</c> block - voice and naming - or <c>null</c> for none.
    /// </summary>
    public DesignWordsSyntax? Words { get; init; }

    /// <summary>
    /// Gets the <c>rules</c> the design system holds its themes to.
    /// </summary>
    public IEnumerable<DesignContrastRuleSyntax> Rules { get; init; } = [];
}

/// <summary>
/// Represents the tokens of one category (<c>color</c>, <c>space</c>, ...) of a <see cref="DesignSystemSyntax"/>.
/// </summary>
/// <param name="Category">The category word.</param>
/// <param name="Tokens">The <see cref="DesignTokenSyntax">tokens</see> declared in the category.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignTokenCategorySyntax(string Category, IEnumerable<DesignTokenSyntax> Tokens, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one token role of a <see cref="DesignTokenCategorySyntax"/>, addressed as <c>&lt;category&gt;.&lt;name&gt;</c>.
/// </summary>
/// <param name="Name">The token's name within its category.</param>
/// <param name="Description">What the token is for, or <c>null</c> when not described.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignTokenSyntax(string Name, string? Description, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a component of a <see cref="DesignSystemSyntax"/>: the parts it is made of and its variants.
/// </summary>
/// <param name="Name">The component's name.</param>
/// <param name="Parts">The part words (<c>text</c>, <c>background</c>, ...) the component is made of.</param>
/// <param name="Variants">The <see cref="DesignVariantSyntax">variants</see> of the component.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignComponentSyntax(string Name, IEnumerable<string> Parts, IEnumerable<DesignVariantSyntax> Variants, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a variant of a <see cref="DesignComponentSyntax"/>: a value per part and the states that override them.
/// </summary>
/// <param name="Name">The variant's name.</param>
/// <param name="Parts">The part assignments of the variant, each referencing a token.</param>
/// <param name="States">The <see cref="DesignStateSyntax">states</see> that override part assignments.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignVariantSyntax(string Name, IEnumerable<DesignAssignmentSyntax> Parts, IEnumerable<DesignStateSyntax> States, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a state (<c>hover</c>, <c>disabled</c>, ...) of a <see cref="DesignVariantSyntax"/>.
/// </summary>
/// <param name="Name">The state's name.</param>
/// <param name="Parts">The part assignments the state overrides.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignStateSyntax(string Name, IEnumerable<DesignAssignmentSyntax> Parts, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>name = value</c> line of a design system or a theme: a part, a palette entry, a token value or a word.
/// </summary>
/// <param name="Name">The assigned name (a part, a palette entry, a token address or a word key).</param>
/// <param name="Value">The value text: a quoted literal (unescaped) or an unquoted reference such as <c>color.primary</c> or <c>palette.white</c>.</param>
/// <param name="IsLiteral"><see langword="true"/> when the value was written as a quoted literal.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignAssignmentSyntax(string Name, string Value, bool IsLiteral, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>words</c> block of a <see cref="DesignSystemSyntax"/>: how the product speaks and what it calls things.
/// </summary>
/// <param name="Voice">The <c>voice</c> entries.</param>
/// <param name="Naming">The <c>naming</c> entries.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignWordsSyntax(IEnumerable<DesignAssignmentSyntax> Voice, IEnumerable<DesignAssignmentSyntax> Naming, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>contrast &lt;foreground&gt; on &lt;background&gt; at least &lt;ratio&gt;</c> rule of a <see cref="DesignSystemSyntax"/>.
/// </summary>
/// <param name="Foreground">The foreground part (for example <c>text</c>).</param>
/// <param name="Background">The background part (for example <c>background</c>).</param>
/// <param name="Minimum">The minimum contrast ratio, as written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DesignContrastRuleSyntax(string Foreground, string Background, string Minimum, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a mode block (<c>light</c>, <c>dark</c>, ...) of a <see cref="ThemeSyntax"/>: the value each color token has in that mode.
/// </summary>
/// <param name="Mode">The mode's name.</param>
/// <param name="Values">The token assignments in the mode.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ThemeModeValuesSyntax(string Mode, IEnumerable<DesignAssignmentSyntax> Values, SourceLocation Location) : SyntaxNode(Location);
