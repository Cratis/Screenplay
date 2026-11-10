// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v44 golden: every v43 construct plus a policy that decides from recorded state.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v44 golden: the v43 golden plus a policy that reads a plan and a command it authorizes.
    /// </summary>
    public const string StatePolicySource = """
        policy DataQualityEnabled
          reads AssetPlan as plan by assetId
          require authenticated and plan.dataQuality == true

        """ + ProjectionParitySource + """

        module Fleet
          feature Tokens
            slice StateChange RequestToken
              event TokenRequested
                assetId String
              command RequestScopedToken
                assetId String identifier
                authorize DataQualityEnabled
                produces TokenRequested
                  for assetId
                  assetId = assetId
            slice StateView Plans
              readmodel AssetPlan
                assetId String key
                dataQuality Bool
              event PlanSet
                dataQuality Bool
              projection Build => AssetPlan
                from PlanSet
                  assetId = $eventSourceId
                  dataQuality = dataQuality
        """;

    /// <summary>
    /// Gets the canonical bytes of the v44 golden.
    /// </summary>
    public static byte[] EsmV44Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v44.json");

    /// <summary>
    /// Creates the v44 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV44()
    {
        var result = property_order_binding.AdmitScreens(StatePolicySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
