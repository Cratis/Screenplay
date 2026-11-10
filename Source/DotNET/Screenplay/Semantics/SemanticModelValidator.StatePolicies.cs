// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    // ESM v44 construct: a policy that reads recorded state.
    internal static bool UsesStatePolicies(SemanticApplication application) =>
        application.Policies.Any(policy => !policy.Reads.IsDefaultOrEmpty || ContainsState(policy.Condition));

    static bool ContainsState(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition => true,
        SemanticNotPolicyCondition not => ContainsState(not.Operand),
        SemanticLogicalPolicyCondition logical => ContainsState(logical.Left) || ContainsState(logical.Right),
        _ => false
    };

    static void ValidateStatePolicyVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesStatePolicies(application),
            version,
            SemanticVersion.V44,
            "An ESM v44 model must declare a policy that reads recorded state.",
            "Policies that decide from recorded state require ESM v44.");
}
