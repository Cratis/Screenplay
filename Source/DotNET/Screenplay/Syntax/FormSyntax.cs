// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

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
    SourceLocation Location) : SyntaxNode(Location);

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
