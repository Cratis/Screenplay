// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog;

/// <summary>
/// Holds the catalog against Chronicle's <c>EventContext</c> record.
/// </summary>
/// <remarks>
/// This repository does not reference Chronicle, so the record is restated here. The members are the positional
/// parameters of <c>Chronicle/Source/Kernel/Concepts/Events/EventContext.cs:28-44</c> - mirrored in the .NET client
/// at <c>Chronicle/Source/Clients/DotNET/Events/EventContext.cs</c> - followed by the computed
/// <c>SubjectIsEventSourceId</c> property at <c>:64</c>. <c>EventType</c> is <c>Events/EventType.cs:12</c> and
/// <c>Identity</c> is <c>Identities/Identity.cs:16</c>. There is no <c>causationId</c>: the one place it ever
/// appeared was Chronicle's Workbench help text.
/// </remarks>
public class when_comparing_with_chronicles_event_context : Specification
{
    static readonly string[] _chronicleEventContext =
    [
        "EventType:EventType",
        "EventSourceType:EventSourceType",
        "EventSourceId:EventSourceId",
        "EventStreamType:EventStreamType",
        "EventStreamId:EventStreamId",
        "SequenceNumber:EventSequenceNumber",
        "Occurred:DateTimeOffset",
        "EventStore:EventStoreName",
        "Namespace:EventStoreNamespaceName",
        "CorrelationId:CorrelationId",
        "Causation:IEnumerable<Causation>",
        "CausedBy:Identity",
        "Tags:IEnumerable<Tag>",
        "Hash:EventHash",
        "ObservationState:EventObservationState",
        "Subject:Subject",
        "SubjectIsEventSourceId:bool"
    ];

    static readonly string[] _chronicleEventType = ["Id:EventTypeId", "Generation:EventTypeGeneration", "Tombstone:bool"];
    static readonly string[] _chronicleIdentity = ["Subject:string", "Name:string", "UserName:string", "OnBehalfOf:Identity"];

    List<string> _members;
    List<string> _eventType;
    List<string> _identity;

    void Because()
    {
        _members = [.. EventContextCatalog.Members.Select(Describe)];
        _eventType = [.. EventContextCatalog.MembersOf(Member("eventType")).Select(Describe)];
        _identity = [.. EventContextCatalog.MembersOf(Member("causedBy")).Select(Describe)];
    }

    [Fact] void should_list_every_member_of_the_record_in_order() => string.Join(", ", _members).ShouldEqual(string.Join(", ", _chronicleEventContext));
    [Fact] void should_list_every_member_of_the_event_type() => string.Join(", ", _eventType).ShouldEqual(string.Join(", ", _chronicleEventType));
    [Fact] void should_list_every_member_of_the_identity() => string.Join(", ", _identity).ShouldEqual(string.Join(", ", _chronicleIdentity));
    [Fact] void should_write_every_member_camel_cased() => EventContextCatalog.Members.All(member => char.IsLower(member.Name[0])).ShouldBeTrue();
    [Fact] void should_treat_causation_as_a_collection() => Member("causation").Kind.ShouldEqual(EventContextMemberKind.Collection);
    [Fact] void should_treat_tags_as_a_collection() => Member("tags").Kind.ShouldEqual(EventContextMemberKind.Collection);
    [Fact] void should_offer_week_as_the_only_derived_function() => EventContextCatalog.Paths.Where(path => path.Member.Kind == EventContextMemberKind.Function).Select(path => path.Path).ShouldContainOnly("occurred.Week");

    static EventContextMember Member(string name) => EventContextCatalog.Members.Single(member => member.Name == name);

    static string Describe(EventContextMember member) => $"{member.DeclaredName}:{member.Type}";
}
