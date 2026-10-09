// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV9Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v9.json");

    public static ExecutableSemanticModel CreateSemanticModelV9() =>
        ExecutableSemanticModel.Create(LanguageVersion.V9, SemanticVersion.V9, CreatePublicEventsApplication());

    public static SemanticApplication CreatePublicEventsApplication()
    {
        var application = CreateSemanticModelV8().Application;
        var identity = ApplicationIdentity.Create("Canonical Golden Application");
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var orderPacked = Event(identity, Id(9500), "ParcelPacked", [new(Id(9501), "orderId", text, false)]);
        var orderShipped = Event(identity, Id(9510), "ParcelShipped", [new(Id(9511), "orderId", text, false), new(Id(9512), "status", text, false)]) with { Visibility = SemanticEventVisibility.Public };
        var statusChanged = Event(identity, Id(9520), "ParcelStatusChanged", [new(Id(9521), "status", text, false)]) with { Visibility = SemanticEventVisibility.Public };
        var fold = new SemanticProjection(Id(9530), "OrderShippedPublisher", orderShipped.Id, [])
        {
            Target = SemanticProjectionTargetKind.Event,
            Scope = new(
                [
                    new(
                        orderPacked.Id,
                        SemanticProjectionKey.EventSourceIdentity,
                        null,
                        [
                            new([Id(9512)], SemanticProjectionOperation.Set, SemanticProjectionValue.Literal(SemanticValue.Text("packed"))),
                            new([Id(9511)], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(9501)]))
                        ])
                ],
                [],
                [],
                [],
                null,
                [],
                [])
        };
        var run = new SemanticSpecification(Id(9540), "Publishing a packed order", [], [], null, [], [], [], [])
        {
            WhenAppended = new(orderPacked.Id, [new(Id(9501), SemanticValue.Text("order-1"))]),
            ThenEvents =
            [
                new(orderShipped.Id, [new(Id(9511), SemanticValue.Text("order-1")), new(Id(9512), SemanticValue.Text("packed"))])
            ]
        };
        var outbound = new SemanticSlice(Id(9550), "PublishOrderShipped", SemanticSliceKind.Translate, [orderPacked, orderShipped], [], [], [fold], [], [run])
        {
            Direction = SemanticTranslationDirection.Outbound
        };
        var reduced = new SemanticSlice(Id(9560), "PublishOrderStatus", SemanticSliceKind.Translate, [statusChanged], [], [], [], [], [])
        {
            Direction = SemanticTranslationDirection.Outbound,
            Reducers = [new("OrderStatusReducer", statusChanged.Id, [new(orderPacked.Id, "reducer-requirement-9570")]) { Target = SemanticProjectionTargetKind.Event }]
        };
        var foreign = Event(identity, Id(9580), "CarrierDispatched", [new(Id(9581), "orderId", text, false)]) with
        {
            Visibility = SemanticEventVisibility.Public,
            Origin = "shipping"
        };
        var dispatched = Event(identity, Id(9590), "ParcelDispatched", [new(Id(9591), "orderId", text, false)]);
        var capture = new SemanticCapture(
            Id(9600),
            "ShipmentTracking",
            "orderId",
            [],
            [new(dispatched.Id, text, null, [new(Id(9591)) { Field = "orderId" }])])
        {
            EventsSource = new([foreign.Id])
        };
        var inbound = new SemanticSlice(Id(9610), "TrackShipments", SemanticSliceKind.Translate, [foreign, dispatched], [], [], [], [], [])
        {
            Direction = SemanticTranslationDirection.Inbound,
            Captures = [capture]
        };
        var feature = new SemanticFeature(Id(9700), "PublicEvents", [], [outbound, reduced, inbound]);
        var modules = application.Modules.Select((module, index) => index == 0 ? module with { Features = module.Features.Add(feature) } : module).ToImmutableArray();

        return application with { Modules = modules };
    }
}
#endif
