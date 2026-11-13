// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v53 golden: every v52 construct plus audiences, availabilities, available gates and provided values.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v53 golden: the v52 golden plus a fleet module that gates and provides.
    /// </summary>
    public const string AvailabilitySource = ExtensionComponentSource + """


        audience Plus
          description "Customers on the Plus package"

        audience Preview

        availability DataQuality
          description "Sold with Plus, once the ship has reported"
          for Plus
          when $context.ship.hasReported == true

        availability Everything
          with DataQuality

        provide region String from "Nordics"

        module Harbor
          provide ship Ship optional from query ShipByKey by $context.arguments.shipId
          available for Plus or Preview

          feature Quality
            available with Everything

            slice StateChange ReportShip
              available when $context.region == "Nordics"
              event ShipReported
                shipId Uuid
              command ReportShip
                available for Plus
                shipId Uuid identifier
                produces ShipReported
                  for shipId
                  shipId = shipId

            slice StateView Fleet
              provide limit Int from 10
              event ShipRegistered
                shipId Uuid
              readmodel Ship
                shipId Uuid
                hasReported Bool
              query ShipByKey => Ship optional
                available for Plus and Preview
                by shipId Uuid
              projection Ships => Ship
                from ShipRegistered key shipId
                  shipId = shipId
              screen ShipScreen
                available for Preview
        """;

    /// <summary>
    /// Gets the canonical bytes of the v53 golden.
    /// </summary>
    public static byte[] EsmV53Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v53.json");

    /// <summary>
    /// Creates the v53 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV53()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(AvailabilitySource);
            if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
            return result.Compilation!.Model;
        }
        finally
        {
            ExtensionContractResolver.Current = previous;
        }
    }
}
#endif
