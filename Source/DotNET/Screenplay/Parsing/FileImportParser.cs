// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>import "&lt;pattern&gt;"</c> - the import of other <c>.play</c> files, as distinct from the import
/// of a qualified name from another bounded context.
/// </summary>
internal static partial class FileImportParser
{
    /// <summary>
    /// Gets whether an <c>import</c> line names files rather than a qualified name.
    /// </summary>
    /// <param name="content">The content of the line.</param>
    /// <returns><c>true</c> when the operand is quoted.</returns>
    public static bool IsFileImport(string content) => QuotedRegex().IsMatch(content);

    /// <summary>
    /// Parses a consumed <c>import</c> line inside a module or feature, where only files can be imported.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/>.</param>
    /// <param name="imports">The file imports collected so far.</param>
    public static void Parse(ParserContext context, SourceLine line, List<FileImportSyntax> imports)
    {
        if (TryParse(line) is { } import)
        {
            imports.Add(import);
            return;
        }

        context.Error(
            DiagnosticCodes.InvalidFileImport,
            $"Invalid import '{line.Content}' - inside a module or feature, import names files: 'import \"<path or glob>\"'",
            line.Location);
        context.SkipBlock(line.Indent);
    }

    /// <summary>
    /// Parses an <c>import "&lt;pattern&gt;"</c> line.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/>.</param>
    /// <returns>The <see cref="FileImportSyntax"/>, or <c>null</c> when the line does not import files.</returns>
    public static FileImportSyntax? TryParse(SourceLine line)
    {
        var match = FileImportRegex().Match(line.Content);
        return match.Success ? new(match.Groups[1].Value, line.Location) : null;
    }

    [GeneratedRegex(@"^import\s+""([^""\\]+)""$", RegexOptions.None, 1000)]
    private static partial Regex FileImportRegex();

    [GeneratedRegex(@"^import\s+""", RegexOptions.None, 1000)]
    private static partial Regex QuotedRegex();
}
