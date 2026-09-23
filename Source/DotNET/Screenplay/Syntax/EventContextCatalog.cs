// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Holds the one catalog of what an <c>$eventContext.&lt;path&gt;</c> can address.
/// </summary>
/// <remarks>
/// <para>
/// Chronicle is the authority. It resolves an event-context path by reflection over its <c>EventContext</c> record
/// (<c>Chronicle/Source/Kernel/Concepts/Events/EventContext.cs:28-44,64</c>) with no allow-list of its own, and a
/// path it cannot resolve throws when it builds the projection. This catalog restates that record and the types it
/// holds so the parser, the editor and the documentation can all say the same thing - and so a mistake is caught
/// while the projection is written rather than when it runs.
/// </para>
/// <para>
/// Concept-typed members (<c>ConceptAs&lt;T&gt;</c> in Chronicle) expose their underlying <c>value</c>; the one
/// derived function, <c>Week</c> on <c>occurred</c>, mirrors <c>DerivedPropertyFunctions.cs:43-48</c>. A runtime may
/// resolve more than the catalog lists - it reflects over the CLR types, <c>DateTimeOffset</c> members included - so
/// the parser warns on an unlisted path rather than rejecting it. Addressing below a collection always fails at runtime
/// and is rejected.
/// </para>
/// </remarks>
public static class EventContextCatalog
{
    /// <summary>
    /// The name of the type that roots every <c>$eventContext.&lt;path&gt;</c>.
    /// </summary>
    public const string EventContextType = "EventContext";

    static readonly Dictionary<string, IReadOnlyList<EventContextMember>> _types = new(StringComparer.Ordinal)
    {
        [EventContextType] =
        [
            Composite("eventType", "EventType", "The type of the event - its identifier, generation and whether it is a tombstone."),
            Value("eventSourceType", "EventSourceType", "The type of the event source the event was appended to."),
            Value("eventSourceId", "EventSourceId", "The identifier of the event source the event was appended to - the value $eventSourceId resolves."),
            Value("eventStreamType", "EventStreamType", "The type of the event stream the event belongs to."),
            Value("eventStreamId", "EventStreamId", "The identifier of the event stream the event belongs to."),
            Value("sequenceNumber", "EventSequenceNumber", "The position of the event in its event sequence."),
            Value("occurred", "DateTimeOffset", "When the event occurred - the time it was appended unless one was given."),
            Value("eventStore", "EventStoreName", "The name of the event store the event belongs to."),
            Value("namespace", "EventStoreNamespaceName", "The namespace within the event store the event belongs to."),
            Value("correlationId", "CorrelationId", "The correlation identifier the event was appended with."),
            Collection("causation", "IEnumerable<Causation>", "The chain of causes that led to the event - a collection, not addressable below."),
            Composite("causedBy", "Identity", "The identity that caused the event."),
            Collection("tags", "IEnumerable<Tag>", "The tags the event was appended with - a collection, not addressable below."),
            Value("hash", "EventHash", "The hash of the event content."),
            Value("observationState", "EventObservationState", "Whether the observer sees the event for the first time or again on replay - differs between live processing and replay."),
            Value("subject", "Subject", "The compliance subject the event is about - the event source identifier unless one was given."),
            Value("subjectIsEventSourceId", "bool", "Whether the subject is the event source identifier.")
        ],
        ["EventType"] =
        [
            Value("id", "EventTypeId", "The identifier of the event type."),
            Value("generation", "EventTypeGeneration", "The generation of the event type."),
            Value("tombstone", "bool", "Whether the event is a tombstone event.")
        ],
        ["Identity"] =
        [
            Value("subject", "string", "The subject identifier of the identity."),
            Value("name", "string", "The display name of the identity."),
            Value("userName", "string", "The user name of the identity."),
            Composite("onBehalfOf", "Identity", "The identity this identity acted on behalf of, if any - an identity with the same members.")
        ],
        ["DateTimeOffset"] = [new("Week", "int", EventContextMemberKind.Function, "The ISO 8601 week of the year the value falls in.")],
        ["EventTypeId"] = ConceptOf("string"),
        ["EventTypeGeneration"] = ConceptOf("uint"),
        ["EventSourceType"] = ConceptOf("string"),
        ["EventSourceId"] = ConceptOf("string"),
        ["EventStreamType"] = ConceptOf("string"),
        ["EventStreamId"] = ConceptOf("string"),
        ["EventSequenceNumber"] = ConceptOf("ulong"),
        ["EventStoreName"] = ConceptOf("string"),
        ["EventStoreNamespaceName"] = ConceptOf("string"),
        ["CorrelationId"] = ConceptOf("Guid"),
        ["EventHash"] = ConceptOf("string"),
        ["Subject"] = ConceptOf("string")
    };

    /// <summary>
    /// Gets the members of the event context itself - the first segment of every path.
    /// </summary>
    public static IReadOnlyList<EventContextMember> Members => _types[EventContextType];

    /// <summary>
    /// Gets every path the catalog lists, depth first in declaration order.
    /// </summary>
    /// <remarks>
    /// A member whose type already appears above it on the same path - <c>causedBy.onBehalfOf</c>, itself an
    /// identity - is listed without repeating its members; <see cref="Resolve"/> still follows it to any depth.
    /// </remarks>
    public static IReadOnlyList<EventContextPath> Paths { get; } = [.. Flatten(string.Empty, EventContextType, [EventContextType])];

    /// <summary>
    /// Gets the members a path may continue into below a member.
    /// </summary>
    /// <param name="member">The <see cref="EventContextMember"/> to get the members of.</param>
    /// <returns>The members below it; empty for a collection, a function or a primitive value.</returns>
    public static IReadOnlyList<EventContextMember> MembersOf(EventContextMember member) =>
        member.Kind is EventContextMemberKind.Collection or EventContextMemberKind.Function
            ? []
            : _types.GetValueOrDefault(member.Type, []);

    /// <summary>
    /// Resolves a path, as written after <c>$eventContext.</c>, against the catalog.
    /// </summary>
    /// <param name="path">The dotted path, such as <c>eventType.id</c>.</param>
    /// <returns>The <see cref="EventContextPathResolution"/>.</returns>
    public static EventContextPathResolution Resolve(string path)
    {
        var segments = path.Split('.');
        EventContextMember? current = null;
        var expected = Members;
        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                return new(path, EventContextPathStatus.Missing) { Member = current, Expected = expected };
            }

            if (current?.Kind == EventContextMemberKind.Collection)
            {
                return new(path, EventContextPathStatus.BelowCollection) { Member = current, Segment = segment };
            }

            var next = expected.FirstOrDefault(member => member.IsNamedBy(segment));
            if (next is null)
            {
                var status = current is null ? EventContextPathStatus.UnknownMember : EventContextPathStatus.UnknownSubPath;
                return new(path, status) { Member = current, Segment = segment, Expected = expected };
            }

            current = next;
            expected = MembersOf(next);
        }

        return new(path, EventContextPathStatus.Known) { Member = current };
    }

    static IEnumerable<EventContextPath> Flatten(string prefix, string type, IReadOnlyList<string> above)
    {
        foreach (var member in _types[type])
        {
            var path = prefix.Length == 0 ? member.Name : $"{prefix}.{member.Name}";
            yield return new(path, member);

            if (member.Kind is EventContextMemberKind.Collection or EventContextMemberKind.Function ||
                above.Contains(member.Type) ||
                !_types.ContainsKey(member.Type))
            {
                continue;
            }

            foreach (var nested in Flatten(path, member.Type, [.. above, member.Type]))
            {
                yield return nested;
            }
        }
    }

    static EventContextMember Value(string name, string type, string description) => new(name, type, EventContextMemberKind.Value, description);

    static EventContextMember Composite(string name, string type, string description) => new(name, type, EventContextMemberKind.Composite, description);

    static EventContextMember Collection(string name, string type, string description) => new(name, type, EventContextMemberKind.Collection, description);

    static IReadOnlyList<EventContextMember> ConceptOf(string primitive) => [Value("value", primitive, $"The underlying {primitive} value.")];
}
