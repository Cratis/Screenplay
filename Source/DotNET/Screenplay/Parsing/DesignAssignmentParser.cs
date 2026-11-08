// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads the <c>name = value</c> lines shared by a design system and a theme: parts, palette entries, token values and words.
/// </summary>
internal static partial class DesignAssignmentParser
{
    /// <summary>
    /// Tries to read a <c>name = "literal"</c> or <c>name = reference</c> line.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> to read.</param>
    /// <param name="assignment">The parsed <see cref="DesignAssignmentSyntax"/> when the line matches.</param>
    /// <returns><see langword="true"/> when the line is an assignment.</returns>
    public static bool TryParse(SourceLine line, [NotNullWhen(true)] out DesignAssignmentSyntax? assignment)
    {
        var match = AssignmentRegex().Match(line.Content);
        if (!match.Success)
        {
            assignment = null;
            return false;
        }

        var name = match.Groups["name"].Value;
        assignment = match.Groups["literal"].Success
            ? new(name, StringLiteral.Unescape(match.Groups["literal"].Value), true, line.Location)
            : new(name, match.Groups["reference"].Value, false, line.Location);
        return true;
    }

    [GeneratedRegex("^(?<name>[A-Za-z_][\\w.\\-]*)\\s*=\\s*(?:\"(?<literal>" + StringLiteral.BodyPattern + ")\"|(?<reference>[A-Za-z_][\\w.\\-]*))$", RegexOptions.NonBacktracking)]
    private static partial Regex AssignmentRegex();
}
