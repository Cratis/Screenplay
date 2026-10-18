// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV30Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v30.json");

    const string AcmeSecondAdmissionsGoldenSource = """
        concept ShipmentId : Uuid
        module Shipping
          feature Dispatch
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId String identifier
                productId String
                quantity Int
                produces OrderPlaced
                  for orderId
                  orderId = orderId
                  productId = productId
                produces StockReserved
                  for productId
                  productId = productId
                  quantity = quantity
              event OrderPlaced
                orderId String
                productId String
              event StockReserved
                productId String
                quantity Int
            slice Automation Dispatching
              event OrderConfirmed
                orderId String
              event ShipmentCreated
                shipmentId ShipmentId
                orderId String
              command CreateShipment
                shipmentId ShipmentId generated identifier
                orderId String
                produces ShipmentCreated
                  for shipmentId
                  shipmentId = shipmentId
                  orderId = orderId
              reaction DispatchConfirmedOrders
                when OrderConfirmed
                  orderId
                  invokes CreateShipment
                    orderId = orderId
              specification ConfirmingAnOrderCreatesAShipment
                given generated CreateShipment.shipmentId = "11111111-1111-4111-8111-111111111111"
                when append OrderConfirmed
                  orderId = "o1"
                then ShipmentCreated
                  for "11111111-1111-4111-8111-111111111111"
                  shipmentId = "11111111-1111-4111-8111-111111111111"
                  orderId = "o1"
        """;

    public static ExecutableSemanticModel CreateSemanticModelV30()
    {
        var source = AcmeSecondAdmissionsGoldenSource;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shipping"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelBinder(2026).Bind("Shipping", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result.Value!.Model;
    }
}
#endif
