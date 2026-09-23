// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

// #211 projection blocks: every shape of the scoped projection contract, in one self-contained slice. Identities 300-399.
public static partial class canonical_serialization_golden_vectors
{
    static (ImmutableArray<SemanticCompositeType> Types, SemanticSlice Slice) CreateProjectionBlocks(
        ApplicationIdentity applicationIdentity,
        SemanticId uuidConcept,
        SemanticId textConcept,
        SemanticId decimalNumberConcept)
    {
        var lineType = Id(300);
        var lineNumber = Id(301);
        var lineQuantity = Id(302);
        var lineSubtotal = Id(303);
        var shippingType = Id(305);
        var shippingCarrier = Id(306);
        var lineKeyType = Id(307);
        var lineKeyNumber = Id(308);
        var lineKeyOrder = Id(309);

        var types = ImmutableArray.Create(
            new SemanticCompositeType(
                lineType,
                "OrderLine",
                [
                    new(lineNumber, "LineNumber", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false),
                    new(lineQuantity, "Quantity", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false),
                    new(lineSubtotal, "Subtotal", SemanticTypeReference.ForConcept(decimalNumberConcept, isOptional: true), false)
                ]),
            new SemanticCompositeType(
                shippingType,
                "Shipping",
                [new(shippingCarrier, "Carrier", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), false)]),
            new SemanticCompositeType(
                lineKeyType,
                "OrderLineKey",
                [
                    new(lineKeyNumber, "Number", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false),
                    new(lineKeyOrder, "Order", SemanticTypeReference.ForConcept(uuidConcept), false)
                ]));

        var placed = Event(applicationIdentity, 320, "OrderPlaced", [(321, "OrderId", SemanticTypeReference.ForConcept(uuidConcept)), (322, "Label", SemanticTypeReference.ForConcept(textConcept)), (323, "Line", SemanticTypeReference.ForCompositeType(lineType))]);
        var lineAdded = Event(applicationIdentity, 325, "LineAdded", [(326, "OrderId", SemanticTypeReference.ForConcept(uuidConcept)), (327, "LineNumber", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber)), (328, "Amount", SemanticTypeReference.ForConcept(decimalNumberConcept))]);
        var lineRemoved = Event(applicationIdentity, 330, "LineRemoved", [(331, "OrderId", SemanticTypeReference.ForConcept(uuidConcept)), (332, "LineNumber", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber))]);
        var shipped = Event(applicationIdentity, 335, "OrderShipped", [(336, "Carrier", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))]);
        var shippingCleared = Event(applicationIdentity, 337, "ShippingCleared", []);
        var customerRegistered = Event(applicationIdentity, 340, "CustomerRegistered", [(341, "Name", SemanticTypeReference.ForConcept(textConcept))]);
        var cancelled = Event(applicationIdentity, 342, "OrderCancelled", []);
        var customerClosed = Event(applicationIdentity, 343, "CustomerClosed", []);
        var discontinued = Event(applicationIdentity, 344, "LineDiscontinued", []);
        var reopened = Event(applicationIdentity, 345, "OrderReopened", []);

        var orderId = Id(351);
        var orderLabel = Id(352);
        var orderLines = Id(353);
        var orderShipping = Id(354);
        var orderTotal = Id(355);
        var orderEvents = Id(356);
        var orderLastSeen = Id(357);
        var orderCustomer = Id(358);
        var orderCustomerName = Id(359);
        var orderFirstQuantity = Id(360);
        var orders = new SemanticReadModel(
            Id(350),
            "OrderView",
            [
                new(orderId, "OrderId", SemanticTypeReference.ForConcept(uuidConcept), true),
                new(orderLabel, "Label", SemanticTypeReference.ForConcept(textConcept, isOptional: true), false),
                new(orderLines, "Lines", SemanticTypeReference.ForCompositeType(lineType, isCollection: true), false),
                new(orderShipping, "Shipping", SemanticTypeReference.ForCompositeType(shippingType, isOptional: true), false),
                new(orderTotal, "Total", SemanticTypeReference.ForConcept(decimalNumberConcept, isOptional: true), false),
                new(orderEvents, "Events", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, isOptional: true), false),
                new(orderLastSeen, "LastSeen", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime, isOptional: true), false),
                new(orderCustomer, "CustomerId", SemanticTypeReference.ForConcept(uuidConcept, isOptional: true), false),
                new(orderCustomerName, "CustomerName", SemanticTypeReference.ForConcept(textConcept, isOptional: true), false),
                new(orderFirstQuantity, "FirstQuantity", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, isOptional: true), false)
            ]);
        var lineKey = Id(371);
        var lineLookup = new SemanticReadModel(
            Id(370),
            "LineLookup",
            [
                new(lineKey, "Key", SemanticTypeReference.ForCompositeType(lineKeyType), true),
                new(Id(372), "Amount", SemanticTypeReference.ForConcept(decimalNumberConcept, isOptional: true), false)
            ]);

        var eventSource = SemanticProjectionKey.EventSourceIdentity;
        var lineScope = SemanticProjectionScope.Empty with
        {
            From =
            [
                new(
                    Id(325),
                    new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(327)])),
                    eventSource,
                    [
                        Map([lineNumber], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(327)])),
                        Map([lineQuantity], SemanticProjectionOperation.Increment),
                        Map([lineSubtotal], SemanticProjectionOperation.Add, SemanticProjectionValue.EventProperty([Id(328)]))
                    ])
            ],
            Removals = [new(Id(330), new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(332)])), new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(331)])))],
            JoinRemovals = [new(Id(344), eventSource)]
        };
        var shippingScope = SemanticProjectionScope.Empty with
        {
            From = [new(Id(335), eventSource, null, [Map([shippingCarrier], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(336)]))])],
            Removals = [new(Id(337), eventSource, null)]
        };
        var ordersProjection = new SemanticProjection(Id(380), "OrderViewProjection", orders.Id, [])
        {
            Scope = new(
                [
                    new(
                        Id(320),
                        new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(321)])),
                        null,
                        [
                            Map([orderLabel], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(322)])),
                            Map([orderFirstQuantity], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(323), lineQuantity])),
                            Map([orderShipping, shippingCarrier], SemanticProjectionOperation.Set, SemanticProjectionValue.Literal(SemanticValue.Text("pending"))),
                            Map([orderTotal], SemanticProjectionOperation.Clear)
                        ]),
                    new(
                        Id(345),
                        new SemanticProjectionValueKey(SemanticProjectionValue.Literal(SemanticValue.Text("00000000-0000-0000-0000-000000000345"))),
                        null,
                        [
                            Map([orderTotal], SemanticProjectionOperation.Subtract, SemanticProjectionValue.Literal(SemanticValue.Number(1.5m))),
                            Map([orderEvents], SemanticProjectionOperation.Decrement),
                            Map([orderCustomer], SemanticProjectionOperation.Set, SemanticProjectionValue.EventSourceIdentity)
                        ])
                ],
                [new(Id(340), orderCustomer, [Map([orderCustomerName], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(341)]))])],
                [new(orderLines, lineNumber, lineScope)],
                [new(orderShipping, shippingScope)],
                new(
                    true,
                    true,
                    [
                        Map([orderLastSeen], SemanticProjectionOperation.Set, SemanticProjectionValue.EventContext("occurred")),
                        Map([orderEvents], SemanticProjectionOperation.Increment)
                    ]),
                [new(Id(342), eventSource, null)],
                [new(Id(343), eventSource)])
        };
        var lineLookupProjection = new SemanticProjection(Id(381), "LineLookupProjection", lineLookup.Id, [])
        {
            Scope = SemanticProjectionScope.Empty with
            {
                From =
                [
                    new(
                        Id(325),
                        new SemanticProjectionCompositeKey(
                            lineKeyType,
                            [
                                new(lineKeyOrder, SemanticProjectionValue.EventSourceIdentity),
                                new(lineKeyNumber, SemanticProjectionValue.EventProperty([Id(327)]))
                            ]),
                        null,
                        [Map([Id(372)], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(328)]))])
                ],
                Every = new(false, false, [])
            }
        };

        var slice = new SemanticSlice(
            Id(390),
            "ProjectionBlocks",
            SemanticSliceKind.StateView,
            [placed, lineAdded, lineRemoved, shipped, shippingCleared, customerRegistered, cancelled, customerClosed, discontinued, reopened],
            [],
            [orders, lineLookup],
            [ordersProjection, lineLookupProjection],
            [],
            []);
        return (types, slice);
    }

    static SemanticEventContract Event(
        ApplicationIdentity applicationIdentity,
        int id,
        string name,
        (int Id, string Name, SemanticTypeReference Type)[] properties) =>
        new(
            Id(id),
            EventContractId.CreateLegacy(applicationIdentity, name),
            EventContractRevision.Initial,
            name,
            [.. properties.Select(_ => new SemanticProperty(Id(_.Id), _.Name, _.Type, false))]);

    static SemanticProjectionMapping Map(
        ImmutableArray<SemanticId> target,
        SemanticProjectionOperation operation,
        SemanticProjectionValue? source = null) => new(target, operation, source);
}
#endif
