// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>storyline</c> declaration - a named, ordered path through the model, such as a user journey that
/// crosses several modules, features and slices.
/// </summary>
/// <remarks>
/// A storyline is authoring metadata for review, tooling and boards; it is never part of the executable model.
/// Each step names one element by kind and reference, so renames and moves rewrite it like any other reference.
/// </remarks>
/// <param name="Name">The name of the storyline.</param>
/// <param name="Title">The optional title; the name stands in when it is absent.</param>
/// <param name="Steps">The ordered steps of the storyline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record StorylineSyntax(
    string Name,
    string? Title,
    IEnumerable<StorylineStepSyntax> Steps,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional prose description of the storyline.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents one step of a <see cref="StorylineSyntax"/>: <c>&lt;kind&gt; &lt;reference&gt; ["&lt;note&gt;"]</c>.
/// </summary>
/// <param name="ElementKind">The kind of element the step names.</param>
/// <param name="Target">The reference to the element: its name, or a qualified name when the name alone is ambiguous.</param>
/// <param name="Note">The optional note saying what happens at this step.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record StorylineStepSyntax(
    StorylineElementKind ElementKind,
    string Target,
    string? Note,
    SourceLocation Location) : SyntaxNode(Location);
