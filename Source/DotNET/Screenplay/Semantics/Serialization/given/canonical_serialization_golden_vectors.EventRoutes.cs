// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV8Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v8.json");

    public static ExecutableSemanticModel CreateSemanticModelV8() =>
        ExecutableSemanticModel.Create(LanguageVersion.V8, SemanticVersion.V8, CreateEventRoutesApplication());

    public static SemanticApplication CreateEventRoutesApplication()
    {
        var application = CreateSemanticModelV7().Application;
        var identity = SemanticTypeReference.ForConcept(Id(8000));
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var integer = SemanticTypeReference.ForConcept(Id(12));
        var source = new SemanticEventSource(Id(9000), "RenamedProject", "Project", [
            new(Id(9001), "RenamedLedger", "Ledger") { StreamIdType = identity },
            new(Id(9002), "Notes", "Notes") { StreamIdType = text },
            new(Id(9003), "Numbers", "Numbers") { StreamIdType = integer },
            new(Id(9004), "All", "All")
        ]) { IdentifierType = identity };
        var fallback = new SemanticEventSource(Id(9010), "Fallback", "Fallback", [new(Id(9011), "All", "All")]);
        var occurred = Event(ApplicationIdentity.Create("Canonical Golden Application"), Id(9020), "RoutedEvent", []);
        var external = Event(ApplicationIdentity.Create("Canonical Golden Application"), Id(9021), "ExternalHistory", []);
        SemanticCommand Command(int number, SemanticEventStream stream, SemanticValue value, bool literal = false) => new(
            Id(number),
            $"Route{number}",
            [new(Id(number + 1), "Id", identity, true), new(Id(number + 2), "Key", stream.StreamIdType ?? text, false)],
            [],
            [new(occurred.Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(number + 1)), [])])
        {
            Destination = new(identity, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(number + 1))),
            Response = new SemanticScalarCommandResponse(Id(number + 2), stream.StreamIdType ?? text),
            Route = new(source.Id, stream.Id)
            {
                StreamId = (stream.StreamIdType, literal) switch
                {
                    (null, _) => null,
                    (_, true) => SemanticExpression.FromValue(value),
                    _ => SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(number + 2))
                }
            }
        };
        var commands = ImmutableArray.Create(
            Command(9100, source.Streams[0], SemanticValue.Text("0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11")),
            Command(9110, source.Streams[1], SemanticValue.Text("a|b%"), true),
            Command(9120, source.Streams[2], SemanticValue.Number(9007199254740991m), true),
            Command(9130, source.Streams[2], SemanticValue.Number(9007199254740990m), true),
            Command(9140, source.Streams[2], SemanticValue.Number(-9007199254740991m), true),
            Command(9150, source.Streams[2], SemanticValue.Number(-9007199254740990m), true),
            Command(9160, source.Streams[3], SemanticValue.Text("unkeyed")) with { Route = new(fallback.Id, fallback.Streams[0].Id) });
        var slice = new SemanticSlice(Id(9200), "EventRoutes", SemanticSliceKind.StateChange, [occurred, external], commands, [], [], [], []);
        slice = AddFixtureRoutes(slice, source, fallback, identity);
        (source, slice) = AddCompositeRoutes(source, slice, identity, text);
        var feature = new SemanticFeature(Id(9300), "EventRoutes", [], [slice]);
        var modules = application.Modules.Select((module, index) => index == 0 ? module with { Features = module.Features.Add(feature) } : module).ToImmutableArray();

        return application with { EventSources = [fallback, source], Modules = modules };
    }

    static SemanticSlice AddFixtureRoutes(SemanticSlice slice, SemanticEventSource source, SemanticEventSource fallback, SemanticTypeReference identity)
    {
        var id = SemanticValue.Text("0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11");
        var specifications = slice.Commands.Select((command, index) => Specification(Id(9400 + index), $"Route fixture {index}") with
        {
            GivenEvents = [new(slice.Events[1].Id, []) { EventSource = new(identity, id), Route = new(source.Id, source.Streams[0].Id) { StreamId = id } }],
            When = new(command.Id, [new(command.Properties[0].Id, id), new(command.Properties[1].Id,
                index switch { 0 => id, >= 2 and <= 5 => ((SemanticValueExpression)command.Route!.StreamId!).Value, _ => SemanticValue.Text("a|b%") })]),
            ThenEvents = [new(slice.Events[0].Id, []) { EventSource = new(identity, id), Route = index == 6 ? new(fallback.Id, fallback.Streams[0].Id) : new(source.Id, command.Route!.Stream)
                { StreamId = index switch { 0 => id, 1 => SemanticValue.Text("a|b%"), _ => ((SemanticValueExpression)command.Route.StreamId!).Value } } }]
        }).ToImmutableArray();
        specifications = specifications.Add(Specification(Id(9410), "Append routed history without a producer") with
        {
            WhenAppended = new(slice.Events[1].Id, []) { EventSource = new(identity, id), Route = new(source.Id, source.Streams[3].Id) },
            ThenEvents = [new(slice.Events[1].Id, []) { Route = new(source.Id, source.Streams[3].Id) }]
        }).Add(Specification(Id(9411), "Assert an unrouted fact") with
        {
            WhenAppended = new(slice.Events[0].Id, []) { EventSource = new(identity, id) },
            ThenEvents = [new(slice.Events[0].Id, []) { Unrouted = true }]
        });

        return slice with { Specifications = specifications };
    }

    static (SemanticEventSource Source, SemanticSlice Slice) AddCompositeRoutes(SemanticEventSource source, SemanticSlice slice, SemanticTypeReference identity, SemanticTypeReference text)
    {
        var stream = new SemanticEventStream(Id(9005), "Periods", "Periods") { StreamIdParts = [new("projectId", identity), new("period", text)] };
        var id = SemanticValue.Text("0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11");
        var period = SemanticValue.Text("2026|10%");
        var command = slice.Commands[0] with
        {
            Id = Id(9170), Name = "CompositeRoute", Properties = [new(Id(9171), "Id", identity, true)],
            Destination = new(identity, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(9171))),
            Produces = [new(slice.Events[0].Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(9171)), [])],
            Response = new SemanticScalarCommandResponse(Id(9171), identity),
            Route = new(source.Id, stream.Id) { StreamIdParts = [new("projectId", SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(9171))), new("period", SemanticExpression.FromValue(period))] }
        };
        var route = new SemanticFixtureRoute(source.Id, stream.Id) { StreamIdParts = [new("projectId", id), new("period", period)] };
        var specification = Specification(Id(9412), "Composite fixtures") with
        {
            GivenEvents = [new(slice.Events[1].Id, []) { EventSource = new(identity, id), Route = route }],
            WhenAppended = new(slice.Events[1].Id, []) { EventSource = new(identity, id), Route = route },
            ThenEvents = [new(slice.Events[1].Id, []) { Route = route }]
        };

        return (source with { Streams = source.Streams.Add(stream) }, slice with { Commands = slice.Commands.Add(command), Specifications = slice.Specifications.Add(specification) });
    }
}
#endif
