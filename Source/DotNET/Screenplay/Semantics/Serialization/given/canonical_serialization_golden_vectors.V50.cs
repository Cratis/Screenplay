// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v50 golden: every v49 construct plus a system with abilities.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v50 golden: the v49 golden plus a system with idempotent, default and code-realized abilities.
    /// </summary>
    public const string AbilitySource = CallerIdentitySource + """


        concept ParcelId : Uuid
        concept TrackingNumber : String

        system Carrier
          description "The parcel carrier's booking service"
          ability BookPickup idempotent
            description "Books a pickup for one parcel"
            parcelId ParcelId
            notes String optional
            returns
              trackingNumber TrackingNumber
              pickupAt DateTime
            outcome NoCapacity
              description "No pickup slot on the requested day"
          ability CancelPickup
            trackingNumber TrackingNumber
            answers later
            ```csharp
            return Cancelled(trackingNumber);
            ```
        """;

    /// <summary>
    /// Gets the canonical bytes of the v50 golden.
    /// </summary>
    public static byte[] EsmV50Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v50.json");

    /// <summary>
    /// Creates the v50 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV50()
    {
        var result = property_order_binding.AdmitScreens(AbilitySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
