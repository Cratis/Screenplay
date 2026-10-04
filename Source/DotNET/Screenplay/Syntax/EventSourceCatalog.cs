// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>Describes an exact authoring lookup, without selecting among physical duplicates.</summary>
public enum EventSourceResolutionKind
{
    /// <summary>Exactly one physical declaration owns the reference.</summary>
    Unique,

    /// <summary>Multiple physical declarations claim the source or stream.</summary>
    Ambiguous,

    /// <summary>No declaration of the requested name exists.</summary>
    NotFound,

    /// <summary>The name belongs to a known value declaration, not an event source.</summary>
    WrongKind
}

/// <summary>Represents an exact source-owned stream resolution.</summary>
/// <param name="Kind">The resolution state.</param>
/// <param name="Sources">Every physical parent candidate.</param>
/// <param name="Streams">Every stream candidate under those parents.</param>
public record EventSourceResolution(EventSourceResolutionKind Kind, IReadOnlyList<EventSourceSyntax> Sources, IReadOnlyList<EventStreamSyntax> Streams);

/// <summary>
/// Indexes application-owned sources without suffix lookup, semantic ids or path-derived identity.
/// An ambiguous parent always makes its children's ownership ambiguous.
/// </summary>
/// <param name="application">The authoritative assembled application.</param>
public sealed class EventSourceCatalog(ApplicationSyntax application)
{
    readonly ILookup<string, EventSourceSyntax> _sources = application.EventSources.ToLookup(source => source.Name, StringComparer.Ordinal);
    readonly HashSet<string> _otherNames = application.Concepts.Select(concept => concept.Name)
        .Concat((application.Types ?? []).Select(type => type.Name)).ToHashSet(StringComparer.Ordinal);

    /// <summary>Resolves an exact source and its exact local stream name.</summary>
    /// <param name="source">The application-owned source name.</param>
    /// <param name="stream">The source-local stream name.</param>
    /// <returns>The state and all physical candidates.</returns>
    public EventSourceResolution Resolve(string source, string stream)
    {
        var parents = _sources[source].ToArray();
        var streams = parents.SelectMany(parent => parent.Streams).Where(candidate => candidate.Name == stream).ToArray();
        var kind = parents.Length switch
        {
            0 => _otherNames.Contains(source) ? EventSourceResolutionKind.WrongKind : EventSourceResolutionKind.NotFound,
            1 => streams.Length switch { 0 => EventSourceResolutionKind.NotFound, 1 => EventSourceResolutionKind.Unique, _ => EventSourceResolutionKind.Ambiguous },
            _ => EventSourceResolutionKind.Ambiguous
        };
        return new(kind, parents, streams);
    }
}
