// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>file</c> directive - the single keyword the language points at a file with.
/// </summary>
/// <remarks>
/// Every block that accepts the directive reads it through here, so the path convention is stated in one
/// place rather than repeated at each of the sites that carry one.
/// </remarks>
internal static partial class FileReferenceParser
{
    /// <summary>
    /// The keyword the directive opens with.
    /// </summary>
    public const string Keyword = "file";

    /// <summary>
    /// Whether the line is a <c>file</c> directive.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> to look at.</param>
    /// <returns>True when it is, false otherwise.</returns>
    /// <remarks>
    /// A bare <c>file</c> with no path is not one - it leaves the word available as a name to whatever the
    /// enclosing block reads a bare word as.
    /// </remarks>
    public static bool IsDirective(SourceLine line) => PathOf(line).Length > 0;

    /// <summary>
    /// Whether the line is a <c>file</c> directive in a block that also reads property lines.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> to look at.</param>
    /// <returns>True when it is, false otherwise.</returns>
    /// <remarks>
    /// <c>file String</c> is a property named <c>file</c>, and <c>file Invoices/Register.cs</c> is the
    /// directive. The two are told apart by shape, the way <c>description</c> already is in the same blocks:
    /// a type reference is a bare identifier, so anything carrying a separator or an extension is a path and
    /// nothing else. The property wins the tie, because a document that used the name before the directive
    /// existed keeps meaning what it meant.
    /// </remarks>
    public static bool IsDirectiveAmongProperties(SourceLine line) =>
        IsDirective(line) && !TypeReferenceRegex().IsMatch(PathOf(line));

    /// <summary>
    /// Parses a <c>file</c> directive from its line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the directive.</param>
    /// <returns>The parsed <see cref="FileReferenceSyntax"/>.</returns>
    /// <remarks>
    /// The path is carried verbatim. Whether it resolves is not the compiler's question - a document is read
    /// where the tree is not present - so a path is never held against a file system here. An absolute one is
    /// a different matter: it is wrong without looking anything up, because it names a place on one machine.
    /// It is reported as a warning rather than an error, so a document carrying one still compiles.
    /// </remarks>
    public static FileReferenceSyntax Parse(ParserContext context, SourceLine line)
    {
        var path = PathOf(line);
        if (AbsolutePathRegex().IsMatch(path))
        {
            context.Warning(
                DiagnosticCodes.AbsoluteFileReference,
                $"'{path}' is an absolute path - a file reference is relative to the repository root, so it means the same thing on every machine",
                line.Location);
        }

        return new(path, line.Location);
    }

    static string PathOf(SourceLine line) =>
        LineText.FirstWord(line.Content) == Keyword ? line.Content[Keyword.Length..].Trim() : string.Empty;

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\[\])?\??$", RegexOptions.None, 1000)]
    private static partial Regex TypeReferenceRegex();

    [GeneratedRegex(@"^(?:[/\\]|[A-Za-z]:[/\\]|~[/\\])", RegexOptions.None, 1000)]
    private static partial Regex AbsolutePathRegex();
}
