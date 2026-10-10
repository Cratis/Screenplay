// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly Dictionary<string, SemanticEventSource> _eventSources = new(StringComparer.Ordinal);
        ImmutableArray<SemanticConcept> _routeConcepts = [];

        ImmutableArray<SemanticEventSource> BindEventSources(ImmutableArray<SemanticConcept> concepts)
        {
            _routeConcepts = concepts;
            var sources = ImmutableArray.CreateBuilder<SemanticEventSource>();
            var storedSources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in syntax.EventSources)
            {
                var stored = SemanticEventRouting.StoredName(source.Id, source.Name);
                if (string.Equals(stored, "Default", StringComparison.Ordinal) || !storedSources.Add(stored))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Event source '{source.Name}' has a reserved or duplicate stored name; choose a unique stored name other than 'Default'.", source.Location);
                }
                if (source.Description is not null)
                {
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Event source '{source.Name}' description is authoring metadata.", source.Location);
                }
                var address = SemanticAddress.ForEventSource(_applicationIdentity, source.Name);
                var id = Resolve(address, source.Location);
                var streams = ImmutableArray.CreateBuilder<SemanticEventStream>();
                var storedStreams = new HashSet<string>(StringComparer.Ordinal);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var stream in source.Streams)
                {
                    var storedStream = SemanticEventRouting.StoredName(stream.Id, stream.Name);
                    if (!storedStreams.Add(storedStream) || !names.Add(stream.Name))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Stream '{source.Name}.{stream.Name}' has a duplicate stored name; choose a unique stored name within its source.", stream.Location);
                        continue;
                    }
                    if (stream.Description is not null)
                    {
                        Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Stream '{source.Name}.{stream.Name}' description is authoring metadata.", stream.Location);
                    }
                    var scalar = stream.StreamId is null ? null : BindTypeReference(stream.StreamId);
                    var parts = stream.StreamIdParts.Select(part => new SemanticStreamIdPart(part.Name, BindTypeReference(part.Type))).ToImmutableArray();
                    if (scalar is not null) SemanticEventRouting.ScalarKind(scalar, concepts);
                    foreach (var part in parts) SemanticEventRouting.ScalarKind(part.Type, concepts);
                    if ((scalar is not null && !parts.IsEmpty) || (!parts.IsEmpty && (parts.Length < 2 || parts.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count() != parts.Length)))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Stream '{source.Name}.{stream.Name}' requires one scalar key or at least two distinct composite parts, never both.", stream.Location);
                    }
                    streams.Add(new(Resolve(SemanticAddress.ForEventStream(address, stream.Name), stream.Location), stream.Name, storedStream)
                    {
                        StreamIdType = scalar,
                        StreamIdParts = parts
                    });
                }
                var bound = new SemanticEventSource(id, source.Name, stored, streams.ToImmutable())
                {
                    IdentifierType = source.Identifier is null ? null : BindTypeReference(source.Identifier)
                };
                if (!_eventSources.TryAdd(source.Name, bound))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Event source '{source.Name}' is declared more than once.", source.Location);
                    continue;
                }
                sources.Add(bound);
            }

            return sources.ToImmutable();
        }

        SemanticObserverFilter? BindObserverFilter(ObserverFilterSyntax? filter)
        {
            if (filter is null) return null;
            UsesV10 = true;
            if (!_eventSources.TryGetValue(filter.EventSource, out var source))
            {
                Error(DiagnosticCodes.InvalidObserverFilter, "An observer filter requires one declared event source.", filter.Location);
                return null;
            }
            if (filter.Stream is null) return new(source.Id);
            if (ResolveRoute(filter.EventSource, filter.Stream, filter.Location) is not { } resolved) return null;

            return new(source.Id, resolved.Stream.Id);
        }

        (SemanticEventSource Source, SemanticEventStream Stream)? ResolveRoute(string sourceName, string streamName, SourceLocation location)
        {
            if (_eventSources.TryGetValue(sourceName, out var source) && source.Streams.SingleOrDefault(stream => string.Equals(stream.Name, streamName, StringComparison.Ordinal)) is { } stream)
            {
                return (source, stream);
            }
            Error(DiagnosticCodes.InvalidSemanticBinding, $"Event route '{sourceName}.{streamName}' must name one declared stream belonging to its source.", location);

            return null;
        }
    }
}
