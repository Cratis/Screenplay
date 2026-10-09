// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Captures;

/// <summary>
/// Reads the <c>source events</c> form of a <see cref="CaptureSourceSyntax"/>: a capture whose captured items are the
/// incoming events of one or more <c>from &lt;Event&gt;</c> lines instead of rows of an external system.
/// </summary>
public static class CaptureEventsSource
{
    /// <summary>
    /// The source kind keyword.
    /// </summary>
    public const string Kind = "events";

    /// <summary>
    /// The setting that names one consumed event.
    /// </summary>
    public const string FromSetting = "from";

    /// <summary>
    /// Gets whether a source reads events.
    /// </summary>
    /// <param name="source">The <see cref="CaptureSourceSyntax"/> to inspect.</param>
    /// <returns>True when the kind is <c>events</c>.</returns>
    public static bool IsEvents(CaptureSourceSyntax source) => source.Kind == Kind;

    /// <summary>
    /// Gets the <c>from &lt;Event&gt;</c> settings of an events source, in source order.
    /// </summary>
    /// <param name="source">The <see cref="CaptureSourceSyntax"/> to inspect.</param>
    /// <returns>The <see cref="CaptureSourceSettingSyntax">settings</see> naming consumed events; empty for any other source.</returns>
    public static IEnumerable<CaptureSourceSettingSyntax> Events(CaptureSourceSyntax source) =>
        IsEvents(source) ? source.Settings.Where(setting => setting.Name == FromSetting) : [];
}
