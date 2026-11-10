// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v46 golden: every v45 construct plus typed policy inputs mapped under authorize.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v46 golden: the v45 golden plus a policy with a typed input that a command maps.
    /// </summary>
    public const string InputPolicySource = """
        policy PlanGrants
          asset String
          reads PlanOfAsset as plan by asset
          require plan.enabled == true

        """ + TextPolicySource + """

        module Plans
          feature Grants
            slice StateChange RequestGrant
              event GrantRequested
                vesselId String
              command RequestGrant
                vesselId String identifier
                authorize PlanGrants
                  asset = vesselId
                produces GrantRequested
                  for vesselId
                  vesselId = vesselId
            slice StateView PlanOfAssets
              readmodel PlanOfAsset
                assetId String key
                enabled Bool
              event PlanOfAssetSet
                enabled Bool
              projection BuildPlanOfAsset => PlanOfAsset
                from PlanOfAssetSet
                  assetId = $eventSourceId
                  enabled = enabled
        """;

    /// <summary>
    /// Gets the canonical bytes of the v46 golden.
    /// </summary>
    public static byte[] EsmV46Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v46.json");

    /// <summary>
    /// Creates the v46 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV46()
    {
        var result = property_order_binding.AdmitScreens(InputPolicySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
