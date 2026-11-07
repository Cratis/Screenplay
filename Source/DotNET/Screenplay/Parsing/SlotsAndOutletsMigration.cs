// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Words the diagnostics for forms Screenplay 6 renamed when it split regions into slots and item lists into outlets.
/// </summary>
/// <remarks>
/// There is no deprecation period: a Screenplay 5 form is an error, and its message names the form that replaces
/// it and the migration that rewrites it.
/// </remarks>
internal static class SlotsAndOutletsMigration
{
    /// <summary>
    /// The command that rewrites a Screenplay 5 model for Screenplay 6.
    /// </summary>
    internal const string Command = "screenplay migrate --from 5 --to 6";

    /// <summary>
    /// Words the message for a renamed form.
    /// </summary>
    /// <param name="old">The old form, described where it was written - for example <c>'outlet detail' in a component</c>.</param>
    /// <param name="replacement">The form that replaces it.</param>
    /// <returns>The diagnostic message.</returns>
    internal static string Renamed(string old, string replacement) =>
        $"{old} is written '{replacement}' since Screenplay 6 - a region content is placed in is a slot; run '{Command}' to rewrite the model (slots and outlets)";

    /// <summary>
    /// Words the message for a form Screenplay 6 replaced with an outlet.
    /// </summary>
    /// <param name="old">The old form, described where it was written - for example <c>'toolbar actions' in a feature</c>.</param>
    /// <param name="replacement">The form that replaces it.</param>
    /// <returns>The diagnostic message.</returns>
    internal static string Replaced(string old, string replacement) =>
        $"{old} is written '{replacement}' since Screenplay 6 - an item list a template offers is an outlet; run '{Command}' to rewrite the model (slots and outlets)";
}
