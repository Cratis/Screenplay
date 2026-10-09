// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the route shared by every fact a command produces.
/// </summary>
/// <param name="Source">The declared source identity.</param>
/// <param name="Stream">The declared stream identity.</param>
public sealed record SemanticCommandRoute(SemanticId Source, SemanticId Stream)
{
    /// <summary>
    /// Gets the scalar key expression, when the stream is scalar-keyed.
    /// </summary>
    public SemanticExpression? StreamId { get; init; }

    /// <summary>
    /// Gets composite key mappings in declaration order, or an empty collection.
    /// </summary>
    public ImmutableArray<SemanticCommandRoutePart> StreamIdParts { get; init; } = [];
}

/// <summary>
/// Represents one command mapping into a declared stream identity part.
/// </summary>
/// <param name="Part">The exact declared part name.</param>
/// <param name="Value">The expression supplying the part.</param>
public sealed record SemanticCommandRoutePart(string Part, SemanticExpression Value);

/// <summary>
/// Represents the concrete route of a specification event occurrence.
/// </summary>
/// <param name="Source">The declared source identity.</param>
/// <param name="Stream">The declared stream identity.</param>
public sealed record SemanticFixtureRoute(SemanticId Source, SemanticId Stream)
{
    /// <summary>
    /// Gets the scalar key value, when the stream is scalar-keyed.
    /// </summary>
    public SemanticValue? StreamId { get; init; }

    /// <summary>
    /// Gets composite key values in declaration order, or an empty collection.
    /// </summary>
    public ImmutableArray<SemanticFixtureRoutePart> StreamIdParts { get; init; } = [];
}

/// <summary>
/// Represents one fixture value for a declared stream identity part.
/// </summary>
/// <param name="Part">The exact declared part name.</param>
/// <param name="Value">The concrete part value.</param>
public sealed record SemanticFixtureRoutePart(string Part, SemanticValue Value);

/// <summary>
/// Represents the stored route carried by one fact; an unrouted fact has no route.
/// </summary>
/// <param name="SourceKind">The stored source name.</param>
/// <param name="StreamKind">The stored stream name.</param>
/// <param name="StreamId">The canonical encoded key, or null for an unkeyed stream.</param>
public sealed record SemanticEventRoute(string SourceKind, string StreamKind, string? StreamId);

[SuppressMessage("Usage", "MA0182", Justification = "Shared seam for the event routes admission batch; production callers join after the inert skeleton.")]
static class SemanticEventRouting
{
    internal static bool Uses(SemanticApplication application) =>
        !application.EventSources.IsDefaultOrEmpty || Slices(application).Any(slice =>
            slice.Commands.Any(command => command.Route is not null) ||
            slice.Specifications.Any(specification => specification.GivenEvents.Concat(specification.ThenEvents)
                .Any(value => value.Route is not null || value.Unrouted) || specification.WhenAppended?.Route is not null));

    internal static string StoredName(string? id, string name) => id ?? name;

    internal static (SemanticEventSource Source, SemanticEventStream Stream) Resolve(SemanticApplication application, SemanticId sourceId, SemanticId streamId)
    {
        var sources = application.EventSources.Where(source => source.Id == sourceId).ToArray();
        if (sources.Length != 1)
        {
            throw new InvalidSemanticContract("An event route requires one declared source.");
        }
        var streams = sources[0].Streams.Where(stream => stream.Id == streamId).ToArray();
        if (streams.Length != 1)
        {
            throw new InvalidSemanticContract("An event route requires one stream belonging to its declared source.");
        }

        return (sources[0], streams[0]);
    }

    internal static StreamIdScalarKind ScalarKind(SemanticTypeReference type, ImmutableArray<SemanticConcept> concepts)
    {
        if (type.IsOptional || type.IsCollection)
        {
            throw new InvalidSemanticContract("A stream identity type must be a required scalar.");
        }
        var matches = type.Kind == SemanticTypeReferenceKind.Concept ? concepts.Where(concept => concept.Id == type.Target).ToArray() : [];
        var primitive = type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept when matches.Length == 1 && matches[0].Values.IsDefaultOrEmpty => matches[0].Primitive,
            _ => SemanticPrimitiveType.Unknown
        };

        return primitive switch
        {
            SemanticPrimitiveType.Text => StreamIdScalarKind.Text,
            SemanticPrimitiveType.Uuid => StreamIdScalarKind.Uuid,
            SemanticPrimitiveType.WholeNumber when type.Kind == SemanticTypeReferenceKind.Concept => StreamIdScalarKind.WholeNumber,
            _ => throw new InvalidSemanticContract("A stream identity type must be text, UUID or a whole-number concept.")
        };
    }

    internal static bool TryFormat(
        SemanticEventSource source,
        SemanticEventStream stream,
        SemanticValue? scalar,
        ImmutableArray<SemanticFixtureRoutePart> parts,
        ImmutableArray<SemanticConcept> concepts,
        out SemanticEventRoute? route,
        out StreamIdFormatFailure failure)
    {
        route = null;
        failure = StreamIdFormatFailure.Arity;
        string? encoded = null;
        if (stream.StreamIdType is { } type)
        {
            if (!stream.StreamIdParts.IsDefaultOrEmpty || !parts.IsDefaultOrEmpty || scalar is null) return false;
            if (!TryFormatScalar(ScalarKind(type, concepts), scalar, out encoded, out failure)) return false;
        }
        else if (!stream.StreamIdParts.IsDefaultOrEmpty)
        {
            if (scalar is not null || stream.StreamIdParts.Length < 2 || parts.IsDefault || parts.Length != stream.StreamIdParts.Length ||
                stream.StreamIdParts.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count() != stream.StreamIdParts.Length)
            {
                return false;
            }
            var formatted = new List<string>();
            foreach (var declaration in stream.StreamIdParts)
            {
                var matches = parts.Where(part => string.Equals(part.Part, declaration.Name, StringComparison.Ordinal)).ToArray();
                if (matches.Length != 1)
                {
                    failure = StreamIdFormatFailure.Arity;
                    return false;
                }
                if (!TryFormatScalar(ScalarKind(declaration.Type, concepts), matches[0].Value, out var value, out failure)) return false;
                formatted.Add(value!);
            }
            encoded = SemanticStreamIdFormatter.EncodeComposite(formatted);
        }
        else if (scalar is not null || !parts.IsDefaultOrEmpty)
        {
            return false;
        }
        route = new(source.SourceKind, stream.StreamKind, encoded);
        failure = StreamIdFormatFailure.None;

        return true;
    }

    internal static bool TryDecode(
        SemanticEventStream stream,
        string value,
        ImmutableArray<SemanticConcept> concepts,
        out IReadOnlyList<string>? parts,
        out StreamIdFormatFailure failure)
    {
        parts = null;
        failure = StreamIdFormatFailure.Arity;
        if (stream.StreamIdType is { } type)
        {
            if (!stream.StreamIdParts.IsDefaultOrEmpty) return false;
            var kind = ScalarKind(type, concepts);
            string? formatted = null;
            failure = StreamIdFormatFailure.Noncanonical;
            var valid = kind switch
            {
                StreamIdScalarKind.Text => SemanticStreamIdFormatter.TryFormatText(value, out formatted, out failure),
                StreamIdScalarKind.Uuid => SemanticStreamIdFormatter.TryFormatUuidText(value, out formatted, out failure),
                StreamIdScalarKind.WholeNumber => BigInteger.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer) &&
                    SemanticStreamIdFormatter.TryFormatInteger(integer, true, out formatted, out failure),
                _ => false
            };
            if (!valid) return false;
            if (!string.Equals(value, formatted, StringComparison.Ordinal))
            {
                failure = StreamIdFormatFailure.Noncanonical;
                return false;
            }
            parts = [value];
            failure = StreamIdFormatFailure.None;

            return true;
        }

        return !stream.StreamIdParts.IsDefaultOrEmpty && SemanticStreamIdFormatter.TryDecodeComposite(
            value, [.. stream.StreamIdParts.Select(part => ScalarKind(part.Type, concepts))], out parts, out failure);
    }

    // "none" and "ambiguous" remain distinct so binding can report the right refusal without guessing a type.
    internal static SemanticTypeReference? FixtureSourceType(SemanticApplication application, SemanticFixtureRoute route, SemanticId eventContract, out string failure)
    {
        var (source, _) = Resolve(application, route.Source, route.Stream);
        failure = string.Empty;
        if (source.IdentifierType is not null) return source.IdentifierType;
        var slices = Slices(application).ToArray();
        var commands = slices.SelectMany(slice => slice.Commands).ToArray();
        var triggers = slices.SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers).ToArray();
        var captures = slices.SelectMany(slice => slice.Captures).ToArray();
        var types = ProducerTypes(eventContract, []).Distinct().ToArray();
        if (types.Length == 1) return types[0];
        failure = types.Length == 0 ? "none" : "ambiguous";

        return null;

        IEnumerable<SemanticTypeReference> ProducerTypes(SemanticId contract, HashSet<SemanticId> visited)
        {
            if (!visited.Add(contract)) yield break;
            foreach (var command in commands)
            {
                foreach (var produced in command.Produces.Where(produced => produced.EventContract == contract))
                {
                    if (SemanticModelValidator.ProducedEventSourceType(command, produced) is { } type) yield return type;
                }
            }
            foreach (var trigger in triggers)
            {
                foreach (var produced in trigger.Produces.Where(produced => produced.EventContract == contract))
                {
                    if (produced.DestinationType is { } type)
                    {
                        yield return type;
                    }
                    else if (trigger.Kind == SemanticReactionTriggerKind.Event)
                    {
                        foreach (var inherited in ProducerTypes(trigger.Source, visited)) yield return inherited;
                    }
                }
            }
            foreach (var append in captures.SelectMany(capture => capture.Appends.Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends))).Where(append => append.EventContract == contract))
            {
                yield return append.EventSourceType;
            }
        }
    }

    static bool TryFormatScalar(StreamIdScalarKind kind, SemanticValue value, out string? formatted, out StreamIdFormatFailure failure)
    {
        formatted = null;
        failure = StreamIdFormatFailure.Noncanonical;
        if (value is SemanticTextValue text)
        {
            return kind switch
            {
                StreamIdScalarKind.Text => SemanticStreamIdFormatter.TryFormatText(text.Value, out formatted, out failure),
                StreamIdScalarKind.Uuid => SemanticStreamIdFormatter.TryFormatUuidText(text.Value, out formatted, out failure),
                _ => false
            };
        }
        if (kind == StreamIdScalarKind.WholeNumber && value is SemanticNumberValue number)
        {
            failure = StreamIdFormatFailure.NotIntegral;
            if (decimal.Truncate(number.Value) != number.Value) return false;

            return SemanticStreamIdFormatter.TryFormatInteger(new BigInteger(number.Value), true, out formatted, out failure);
        }

        return false;
    }

    static IEnumerable<SemanticSlice> Slices(SemanticApplication application) => application.Modules.SelectMany(module => module.Features).SelectMany(Slices);
    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
