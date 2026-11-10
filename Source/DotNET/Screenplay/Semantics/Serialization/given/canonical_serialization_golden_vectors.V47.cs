// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v47 golden: every v46 construct plus a collection query that narrows its rows by a policy.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v47 golden: the v46 golden plus a state policy that narrows a list.
    /// </summary>
    public const string NarrowingSource = InputPolicySource + """


        policy VesselOnThePlan
          reads VesselPlan as plan by vesselId
          require plan.enabled == true

        module Harbour
          feature Vessels
            slice StateView VesselList
              readmodel Vessel
                vesselId String key
                name String
              readmodel VesselPlan
                vesselId String key
                enabled Bool
              event VesselNamed
                name String
              event VesselPlanSet
                enabled Bool
              projection BuildVessel => Vessel
                from VesselNamed
                  vesselId = $eventSourceId
                  name = name
              projection BuildVesselPlan => VesselPlan
                from VesselPlanSet
                  vesselId = $eventSourceId
                  enabled = enabled
              query VesselsOnThePlan => Vessel[]
                narrow VesselOnThePlan
        """;

    /// <summary>
    /// Gets the canonical bytes of the v47 golden.
    /// </summary>
    public static byte[] EsmV47Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v47.json");

    /// <summary>
    /// Creates the v47 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV47()
    {
        var result = property_order_binding.AdmitScreens(NarrowingSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
