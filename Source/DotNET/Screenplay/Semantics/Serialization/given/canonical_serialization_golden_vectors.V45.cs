// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v45 golden: every v44 construct plus text and membership operators in a policy state condition.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v45 golden: the v44 golden plus a policy that tests a feature set and a name prefix.
    /// </summary>
    public const string TextPolicySource = """
        policy FeatureEnabled
          reads FeaturePlan as plan by assetId
          require authenticated and plan.features contains "DataQuality" and plan.tier starts with "pro"

        """ + StatePolicySource + """

        module Entitlements
          feature Features
            slice StateChange RequestFeature
              event FeatureRequested
                assetId String
              command RequestFeature
                assetId String identifier
                authorize FeatureEnabled
                produces FeatureRequested
                  for assetId
                  assetId = assetId
            slice StateView FeaturePlans
              readmodel FeaturePlan
                assetId String key
                features String[]
                tier String
              event FeaturePlanSet
                features String[]
                tier String
              projection BuildFeaturePlan => FeaturePlan
                from FeaturePlanSet
                  assetId = $eventSourceId
                  features = features
                  tier = tier
        """;

    /// <summary>
    /// Gets the canonical bytes of the v45 golden.
    /// </summary>
    public static byte[] EsmV45Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v45.json");

    /// <summary>
    /// Creates the v45 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV45()
    {
        var result = property_order_binding.AdmitScreens(TextPolicySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
