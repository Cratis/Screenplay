// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    // ESM v49 construct: a policy state condition that compares with the caller's identifier, or a caller fixture that names one.
    internal static bool UsesCallerIdentity(SemanticApplication application) =>
        application.Policies.Any(policy => Operands(policy.Condition).Any(operand => operand.CallerId)) ||
        application.Modules.SelectMany(module => module.Features).SelectMany(Slices).SelectMany(slice => slice.Specifications).Any(specification => specification.GivenCaller?.Id is not null);

    static IEnumerable<SemanticConditionOperand> Operands(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition { Condition: SemanticComparison comparison } => [comparison.Left, comparison.Right],
        SemanticStatePolicyCondition => [],
        SemanticNotPolicyCondition not => Operands(not.Operand),
        SemanticLogicalPolicyCondition logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };

    static void ValidateCallerIdentityVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesCallerIdentity(application),
            version,
            SemanticVersion.V49,
            "An ESM v49 model must compare a policy state condition with the caller's identifier or name one in a caller fixture.",
            "A policy state condition that compares with the caller's identifier, or a caller fixture that names one, requires ESM v49.");
}
