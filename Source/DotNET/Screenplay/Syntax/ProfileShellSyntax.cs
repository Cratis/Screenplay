// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The kind of literal a shell <c>option</c> value is, kept so the authored form prints back unchanged.
/// </summary>
public enum ShellOptionValueKind
{
    /// <summary>A bare word such as <c>slim</c>, which is the text value <c>"slim"</c>.</summary>
    Word = 0,

    /// <summary>A quoted string.</summary>
    Text = 1,

    /// <summary>A number, kept as its authored token.</summary>
    Number = 2,

    /// <summary>The literal <c>true</c> or <c>false</c>.</summary>
    Flag = 3
}

/// <summary>
/// Represents the <c>root template</c> line of a <see cref="UiProfileSyntax"/>: the template the whole application is drawn in,
/// every screen's template chain placed in its primary outlet, with the options the template exposes.
/// </summary>
/// <param name="Name">The referenced template's name: an own layout or application-scope screen template, or one a blueprint ships.</param>
/// <param name="Options">The <c>option</c> lines configuring the shell, in authored order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// <c>root template [blueprint &lt;Blueprint&gt;] &lt;Name&gt; [version "&lt;range&gt;"]</c>. It is the same template reference a <c>template</c>
/// assignment uses. The legacy <c>layout &lt;Name&gt;</c> line stays readable beside it.
/// </remarks>
public record RootTemplateSyntax(string Name, IEnumerable<ShellOptionSyntax> Options, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the name of the blueprint package the template comes from, or <c>null</c> for an own template.
    /// </summary>
    public string? Blueprint { get; init; }

    /// <summary>
    /// Gets the semver range of the blueprint the reference accepts (<c>version "^4.5"</c>), or <c>null</c> when it does not pin a version.
    /// </summary>
    public string? Version { get; init; }
}

/// <summary>
/// Represents an <c>option &lt;name&gt; &lt;value&gt;</c> line under a <see cref="RootTemplateSyntax"/>: one of the shell's own configurable items.
/// </summary>
/// <param name="Name">The option's name, as the shell exposes it (for example <c>navigationMode</c>).</param>
/// <param name="Value">The value as authored, without quotes.</param>
/// <param name="Kind">The <see cref="ShellOptionValueKind"/> of the value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Whether the shell exposes the option, and whether the value suits it, is decided against the shell's descriptor, not the grammar.
/// </remarks>
public record ShellOptionSyntax(string Name, string Value, ShellOptionValueKind Kind, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>appearance</c> block of a <see cref="UiProfileSyntax"/>: how the application looks regardless of the shell.
/// </summary>
/// <param name="ColorScheme">The <c>color scheme</c> word (<c>light</c>, <c>dark</c> or <c>system</c>), or <c>null</c> when not declared.</param>
/// <param name="Accent">The <c>accent</c> colour text (<c>#4f46e5</c>), or <c>null</c> when not declared.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ProfileAppearanceSyntax(string? ColorScheme, string? Accent, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the location of the <c>color scheme</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? ColorSchemeLocation { get; init; }

    /// <summary>
    /// Gets the location of the <c>accent</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? AccentLocation { get; init; }

    /// <summary>
    /// Gets the <c>corners</c> word (<c>sharp</c>, <c>default</c> or <c>round</c>), or <c>null</c> when not declared.
    /// </summary>
    public string? Corners { get; init; }

    /// <summary>
    /// Gets the location of the <c>corners</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? CornersLocation { get; init; }

    /// <summary>
    /// Gets the <c>font</c> id from Scene's font catalog (<c>inter</c>, <c>source-serif-4</c>), or <c>null</c> when not declared.
    /// </summary>
    public string? Font { get; init; }

    /// <summary>
    /// Gets the location of the <c>font</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? FontLocation { get; init; }
}

/// <summary>
/// Represents the <c>identity</c> block of a <see cref="UiProfileSyntax"/>: what the application calls itself and the mark it wears.
/// </summary>
/// <param name="Name">The display name text, or <c>null</c> to take the application's own name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// <c>name "&lt;text&gt;"</c>, <c>logo initials "&lt;1-2 characters&gt;"</c>, <c>logo icon &lt;icon&gt;</c> or <c>logo image "&lt;data:image/...;base64,...&gt;"</c>,
/// and <c>show name true|false</c>. Every line is optional; each at most once.
/// </remarks>
public record ProfileIdentitySyntax(string? Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the location of the <c>name</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? NameLocation { get; init; }

    /// <summary>
    /// Gets the kind of the logo (<c>initials</c>, <c>icon</c> or <c>image</c>), or <c>null</c> when the profile declares no logo.
    /// </summary>
    public string? LogoKind { get; init; }

    /// <summary>
    /// Gets the logo's value as authored: the initials, the icon id, or the image as a base64 data URI. <c>null</c> when the profile declares no logo.
    /// </summary>
    public string? LogoValue { get; init; }

    /// <summary>
    /// Gets the location of the <c>logo</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? LogoLocation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the name is drawn beside the logo, or <c>null</c> when the profile does not say (it is shown).
    /// </summary>
    public bool? ShowName { get; init; }

    /// <summary>
    /// Gets the location of the <c>show name</c> line, or <c>null</c> when not declared.
    /// </summary>
    public SourceLocation? ShowNameLocation { get; init; }
}

/// <summary>
/// Represents the <c>new screens use template</c> line of a <see cref="UiProfileSyntax"/>: the template a new screen starts from.
/// </summary>
/// <param name="Name">The referenced template's name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// <c>new screens use template [blueprint &lt;Blueprint&gt;] &lt;Name&gt; [version "&lt;range&gt;"]</c>. It is an authoring default only.
/// </remarks>
public record NewScreenTemplateSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the name of the blueprint package the template comes from, or <c>null</c> for an own template.
    /// </summary>
    public string? Blueprint { get; init; }

    /// <summary>
    /// Gets the semver range of the blueprint the reference accepts, or <c>null</c> when it does not pin a version.
    /// </summary>
    public string? Version { get; init; }
}
