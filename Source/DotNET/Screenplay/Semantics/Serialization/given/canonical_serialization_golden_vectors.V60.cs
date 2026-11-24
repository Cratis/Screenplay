// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v60 golden: the v58 source plus a graph projection with its declaring slice.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// Gets the source of the v60 golden, including the complete parcel lifecycle facts from v58.
    /// </summary>
    public static string GraphProjectionScopeSource => NegatedAudienceSource + """


        module Ownership
          feature Chart
            slice StateView OwnerChart
              event OwnerAdded
              graph Owners
                node Owner
                  ownerId Uuid key
              projection OwnerChart => graph Owners
                node Owner
                  from OwnerAdded key $eventSourceId
                    ownerId = $eventSourceId
        """;

    /// <summary>
    /// Gets the canonical bytes of the v60 golden.
    /// </summary>
    public static byte[] EsmV60Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v60.json");

    /// <summary>
    /// Creates the v60 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV60()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(GraphProjectionScopeSource);
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
