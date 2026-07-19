// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Captures;

/// <summary>
/// Represents the base of every operation in a capture <c>map</c> body.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record CaptureMapOperationSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>map</c> entry translating a source value, such as <c>status = status translate</c> or
/// <c>summary = `${status} invoice`</c>.
/// </summary>
/// <param name="Property">The target property.</param>
/// <param name="Source">The <see cref="ExpressionSyntax"/> providing the source value - a property path or a template.</param>
/// <param name="Translations">The <see cref="CaptureTranslationSyntax">value translations</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureMapEntrySyntax(
    string Property,
    ExpressionSyntax Source,
    IEnumerable<CaptureTranslationSyntax> Translations,
    SourceLocation Location) : CaptureMapOperationSyntax(Location);

/// <summary>
/// Represents a single value translation, such as <c>"utkast" =&gt; draft</c>.
/// </summary>
/// <param name="From">The source value being translated.</param>
/// <param name="To">The target value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureTranslationSyntax(string From, string To, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>split</c> operation, such as <c>split fullName by " "</c> with indented target properties.
/// </summary>
/// <param name="Source">The <see cref="ExpressionSyntax"/> providing the value to split.</param>
/// <param name="Separator">The separator to split the value by.</param>
/// <param name="Targets">The target properties receiving the split parts, in order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureSplitSyntax(
    ExpressionSyntax Source,
    string Separator,
    IEnumerable<string> Targets,
    SourceLocation Location) : CaptureMapOperationSyntax(Location);
