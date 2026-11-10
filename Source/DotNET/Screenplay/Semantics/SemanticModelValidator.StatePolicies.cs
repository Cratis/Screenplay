// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    // ESM v44 construct: a policy that reads recorded state.
    internal static bool UsesStatePolicies(SemanticApplication application) =>
        application.Policies.Any(policy => !policy.Reads.IsDefaultOrEmpty || ContainsState(policy.Condition));

    // ESM v45 construct: a text or membership operator in a policy state condition.
    internal static bool UsesTextOperators(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => comparison.Operator is SemanticComparisonOperator.Contains or SemanticComparisonOperator.StartsWith,
        SemanticLogicalCondition logical => UsesTextOperators(logical.Left) || UsesTextOperators(logical.Right),
        _ => false
    };

    internal static bool UsesTextOperators(SemanticApplication application) =>
        application.Policies.Any(policy => HasTextOperator(policy.Condition));

    static bool HasTextOperator(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition state => UsesTextOperators(state.Condition),
        SemanticNotPolicyCondition not => HasTextOperator(not.Operand),
        SemanticLogicalPolicyCondition logical => HasTextOperator(logical.Left) || HasTextOperator(logical.Right),
        _ => false
    };

    static bool ContainsState(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition => true,
        SemanticNotPolicyCondition not => ContainsState(not.Operand),
        SemanticLogicalPolicyCondition logical => ContainsState(logical.Left) || ContainsState(logical.Right),
        _ => false
    };

    static void ValidateTextOperatorVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesTextOperators(application),
            version,
            SemanticVersion.V45,
            "An ESM v45 model must declare a text or membership operator in a policy state condition.",
            "Text and membership operators in a policy state condition require ESM v45.");

    static void ValidateStatePolicyVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesStatePolicies(application),
            version,
            SemanticVersion.V44,
            "An ESM v44 model must declare a policy that reads recorded state.",
            "Policies that decide from recorded state require ESM v44.");
}
