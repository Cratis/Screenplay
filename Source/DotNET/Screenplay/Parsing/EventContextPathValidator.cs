// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks <c>$eventContext.&lt;path&gt;</c> references - in projection expressions and in dynamic dictionary keys -
/// against the <see cref="EventContextCatalog"/>.
/// </summary>
/// <remarks>
/// Chronicle resolves the path by reflection when it builds the projection and throws on one it cannot resolve, and no
/// layer before it looks at the path, so this is the only place an early diagnostic is possible. An unlisted member is
/// a warning - the runtime reflects over the CLR types and may resolve more than the catalog lists. A path below a
/// collection, or no path at all, never resolves and is an error.
/// </remarks>
internal static class EventContextPathValidator
{
    const string EventContextPrefix = "$eventContext";
    const string DynamicKeyMarker = ".$";

    /// <summary>
    /// Validates the path of an <c>$eventContext.&lt;path&gt;</c> expression.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="path">The path following <c>$eventContext.</c>.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the expression.</param>
    public static void Validate(ParserContext context, string path, SourceLocation location)
    {
        var resolution = EventContextCatalog.Resolve(path);
        switch (resolution.Status)
        {
            case EventContextPathStatus.Missing:
                context.Error(
                    DiagnosticCodes.MissingEventContextPath,
                    $"'{EventContextPrefix}.{path}' names no member - expected one of {Names(resolution.Expected)}",
                    location);
                break;

            case EventContextPathStatus.UnknownMember:
                context.Warning(
                    DiagnosticCodes.UnknownEventContextMember,
                    $"Unknown event context member '{resolution.Segment}' in '{EventContextPrefix}.{path}' - expected one of {Names(resolution.Expected)}",
                    location);
                break;

            case EventContextPathStatus.UnknownSubPath:
                context.Warning(
                    DiagnosticCodes.UnknownEventContextPath,
                    $"Unknown event context path '{EventContextPrefix}.{path}' - '{resolution.Member!.Name}' ({resolution.Member.Type}) has {Members(resolution.Expected)}",
                    location);
                break;

            case EventContextPathStatus.BelowCollection:
                context.Error(
                    DiagnosticCodes.EventContextPathBelowCollection,
                    $"'{EventContextPrefix}.{path}' cannot resolve - '{resolution.Member!.Name}' is a collection ({resolution.Member.Type}) and cannot be addressed below",
                    location);
                break;
        }
    }

    /// <summary>
    /// Validates the dynamic dictionary key of a mapping target, such as <c>countByType.$eventContext.eventType.id</c>.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="property">The mapping target property, unescaped.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the mapping.</param>
    /// <remarks>
    /// Chronicle treats any target containing <c>.$</c> as a dynamic key (<c>ProjectionFactory.cs:791</c>) but only
    /// resolves <c>.$eventContext.&lt;path&gt;</c> (<c>PropertyMappers.cs:229-250</c>); any other source is kept as the
    /// literal key text, so the value lands under the wrong key without an error.
    /// </remarks>
    public static void ValidateDynamicKey(ParserContext context, string property, SourceLocation location)
    {
        var marker = property.IndexOf(DynamicKeyMarker, StringComparison.Ordinal);
        if (marker < 0)
        {
            return;
        }

        var key = property[(marker + 1)..];
        if (key == EventContextPrefix)
        {
            Validate(context, string.Empty, location);
            return;
        }

        if (key.StartsWith($"{EventContextPrefix}.", StringComparison.Ordinal))
        {
            Validate(context, key[(EventContextPrefix.Length + 1)..], location);
            return;
        }

        var source = key.Split('.')[0];
        context.Warning(
            DiagnosticCodes.UnresolvedDynamicKeySource,
            $"Dynamic dictionary key '{source}' in '{property}' is never resolved - only '{EventContextPrefix}.<path>' is, so the literal text '{source}' becomes the key",
            location);
    }

    static string Names(IEnumerable<EventContextMember> members) => string.Join(", ", members.Select(member => member.Name));

    static string Members(IReadOnlyList<EventContextMember> members) =>
        members.Count == 0 ? "no members to address" : $"members {Names(members)}";
}
