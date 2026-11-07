// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a screen's <c>fits &lt;role&gt;, …</c> line - the places the slice's screen fits, in order of preference.
/// </summary>
internal static partial class ScreenFitsParser
{
    /// <summary>
    /// What a <c>fits</c> line may hold, as it reads in a diagnostic.
    /// </summary>
    public const string Expected = "fits <dialog|drawer|pane|panel|page>[, ...]";

    /// <summary>
    /// Parses a <c>fits</c> line of a screen.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <c>fits</c> line.</param>
    /// <param name="screen">The name of the screen, for diagnostics.</param>
    /// <param name="existing">The fits already read, or <c>null</c> when this is the screen's first <c>fits</c> line.</param>
    /// <param name="directiveLocations">Where the screen's directives start, keyed by keyword.</param>
    /// <returns>The fits the screen keeps.</returns>
    public static List<ScreenFit>? Parse(ParserContext context, SourceLine line, string screen, List<ScreenFit>? existing, Dictionary<string, SourceLocation> directiveLocations)
    {
        context.SkipBlock(line.Indent);
        if (existing is not null)
        {
            context.Error(DiagnosticCodes.InvalidScreenFits, $"Screen '{screen}' states 'fits' more than once - list every place on one line, in order of preference", line.Location);
            return existing;
        }

        var match = FitsRegex().Match(line.Content);
        if (!match.Success)
        {
            var hint = line.Content.StartsWith("fits slot", StringComparison.Ordinal)
                ? " - 'fits slot <name>' belongs to a screen template; a screen lists roles"
                : string.Empty;
            context.Error(DiagnosticCodes.InvalidScreenFits, $"Invalid fits '{line.Content}' on screen '{screen}' - expected {Expected}{hint}", line.Location);
            return [];
        }

        directiveLocations["fits"] = line.Location;
        var fits = new List<ScreenFit>();
        foreach (var role in match.Groups[1].Value.Split(',').Select(part => part.Trim()))
        {
            if (Role(role) is not { } fit)
            {
                context.Error(DiagnosticCodes.InvalidScreenFits, $"Screen '{screen}' fits '{role}', which is not a place - expected dialog, drawer, pane, panel or page", line.Location);
                continue;
            }

            if (fits.Contains(fit))
            {
                context.Error(DiagnosticCodes.InvalidScreenFits, $"Screen '{screen}' fits '{role}' more than once", line.Location);
                continue;
            }

            fits.Add(fit);
        }

        return fits;
    }

    /// <summary>
    /// Gets the keyword a <see cref="ScreenFit"/> is written with.
    /// </summary>
    /// <param name="fit">The <see cref="ScreenFit"/>.</param>
    /// <returns>The keyword.</returns>
    public static string Keyword(ScreenFit fit) => fit.ToString().ToLowerInvariant();

    /// <summary>
    /// Gets whether a screen that fits the given places fits a slot with the given role.
    /// </summary>
    /// <param name="fits">What the screen fits; empty when it does not say, which fits every role.</param>
    /// <param name="role">The <see cref="SlotRole"/> of the slot.</param>
    /// <returns><c>true</c> when the screen fits the slot.</returns>
    public static bool FitsRole(IEnumerable<ScreenFit> fits, SlotRole role)
    {
        var listed = fits.ToList();
        return listed.Count == 0 || listed.Contains(role switch
        {
            SlotRole.Pane => ScreenFit.Pane,
            SlotRole.Panel => ScreenFit.Panel,
            _ => ScreenFit.Page
        });
    }

    static ScreenFit? Role(string role) => role switch
    {
        "dialog" => ScreenFit.Dialog,
        "drawer" => ScreenFit.Drawer,
        "pane" => ScreenFit.Pane,
        "panel" => ScreenFit.Panel,
        "page" => ScreenFit.Page,
        _ => null
    };

    [GeneratedRegex(@"^fits\s+([A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex FitsRegex();
}
