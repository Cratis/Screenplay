// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

// Source-only physical read confidence. It confers neither edit eligibility nor semantic identity.
internal sealed class EventSourceReadConfidence(IEnumerable<(EventSourceSyntax Source, bool PlacementResolved)> sources, bool isComplete)
{
    readonly ILookup<string, (EventSourceSyntax Source, bool PlacementResolved)> _sources = sources.ToLookup(entry => entry.Source.Name, StringComparer.Ordinal);

    // Invalid command payloads cannot contain application-owned sources. Only unread root
    // blocks or malformed source declarations taint this source-only extent contract.
    internal static bool HasUnknownExtent(IEnumerable<Diagnostic> diagnostics) => diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
        (diagnostic.Code == DiagnosticCodes.UnknownTopLevelConstruct || diagnostic.Code == DiagnosticCodes.InvalidEventSourceDeclaration));

    internal EventSourceReadResolution Resolve(string source, string? stream = null)
    {
        var parents = _sources[source].ToArray();
        var children = stream is null ? [] : parents.SelectMany(parent => parent.Source.Streams).Where(child => child.Name == stream).ToArray();
        var ambiguous = parents.Length > 1 || children.Length > 1;
        var incomplete = !isComplete || parents.Any(parent => !parent.PlacementResolved);
        var state = (ambiguous, incomplete, parents.Length == 1 && (stream is null || children.Length == 1)) switch
        {
            (true, _, _) => "ambiguous",
            (_, true, _) => "incomplete",
            (_, _, true) => "unique",
            _ => "notFound"
        };
        var reasons = new List<string>();
        if (parents.Length > 1) reasons.Add("Multiple physical parent sources claim the exact name.");
        if (children.Length > 1) reasons.Add("Multiple physical streams claim the exact parent and local name.");
        if (parents.Any(parent => !parent.PlacementResolved)) reasons.Add("A physical parent has unresolved import placement.");
        if (!isComplete) reasons.Add("Physical source extent or placement is incomplete; missing declarations cannot be excluded.");

        return new(state, [.. parents.Select(parent => parent.Source)], children, [.. reasons]);
    }
}

internal sealed record EventSourceReadResolution(string State, EventSourceSyntax[] Sources, EventStreamSyntax[] Streams, string[] Reasons);
