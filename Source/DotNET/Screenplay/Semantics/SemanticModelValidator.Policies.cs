// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        static IEnumerable<string> ArtifactPaths(SemanticPolicyCondition condition) => condition switch
        {
            SemanticClaimCondition { TargetKind: SemanticClaimTargetKind.Artifact, Value: { } path } => [path],
            SemanticLogicalPolicyCondition logical => ArtifactPaths(logical.Left).Concat(ArtifactPaths(logical.Right)),
            SemanticNotPolicyCondition not => ArtifactPaths(not.Operand),
            _ => []
        };

        static bool UsesSubject(SemanticPolicyCondition condition) => condition switch
        {
            SemanticClaimCondition { TargetKind: SemanticClaimTargetKind.Subject } => true,
            SemanticLogicalPolicyCondition logical => UsesSubject(logical.Left) || UsesSubject(logical.Right),
            SemanticNotPolicyCondition not => UsesSubject(not.Operand),
            _ => false
        };

        static bool IsAnonymous(SemanticAuthorization? authorization, ImmutableArray<SemanticPolicy> policies) =>
            authorization is SemanticPolicyReference reference &&
            policies.Any(policy => policy.Name == reference.Name && policy.Condition is SemanticAnonymousCondition);

        static bool ReferencesAnonymous(SemanticAuthorization? authorization, ImmutableArray<SemanticPolicy> policies) => authorization switch
        {
            SemanticPolicyReference => IsAnonymous(authorization, policies),
            SemanticLogicalAuthorization logical => ReferencesAnonymous(logical.Left, policies) || ReferencesAnonymous(logical.Right, policies),
            _ => false
        };

        static bool RequiresCaller(SemanticAuthorization? authorization, ImmutableArray<SemanticPolicy> policies) =>
            authorization is not null && !IsAnonymous(authorization, policies);

        void ValidatePolicyCondition(SemanticPolicyCondition? condition, bool allowOpaque = true)
        {
            switch (condition)
            {
                case SemanticOpaquePolicyCondition when allowOpaque: break;
                case SemanticAnonymousCondition when !_semanticVersion.IsAtLeast(SemanticVersion.V31):
                    throw new InvalidSemanticContract("The anonymous policy condition requires ESM v31.");
                case SemanticAnonymousCondition when allowOpaque: break;
                case SemanticAnonymousCondition: throw new InvalidSemanticContract("An anonymous policy condition must be the whole policy condition.");
                case SemanticAuthenticatedCondition: break;
                case SemanticStatePolicyCondition state when _semanticVersion.IsAtLeast(SemanticVersion.V44):
                    if (state.Condition is null) throw new InvalidSemanticContract("A state policy condition requires a comparison.");
                    if (UsesTextOperators(state.Condition) && !_semanticVersion.IsAtLeast(SemanticVersion.V45)) throw new InvalidSemanticContract("Text and membership operators in a policy state condition require ESM v45.");
                    break;
                case SemanticStatePolicyCondition: throw new InvalidSemanticContract("Policies that decide from recorded state require ESM v44.");
                case SemanticNotPolicyCondition not when _semanticVersion.IsAtLeast(SemanticVersion.V7):
                    ValidatePolicyCondition(not.Operand, false);
                    break;
                case SemanticNotPolicyCondition: throw new InvalidSemanticContract("Policy negation requires ESM v7.");
                case SemanticRoleCondition { Role: { } }: break;
                case SemanticClaimCondition { Claim: { }, TargetKind: SemanticClaimTargetKind.Subject, Value: null }: break;
                case SemanticClaimCondition { Claim: { }, TargetKind: SemanticClaimTargetKind.Literal or SemanticClaimTargetKind.Artifact, Value: { } }: break;
                case SemanticLogicalPolicyCondition logical when logical.Operator is SemanticLogicalOperator.And or SemanticLogicalOperator.Or:
                    ValidatePolicyCondition(logical.Left, false);
                    ValidatePolicyCondition(logical.Right, false);
                    break;
                default: throw new InvalidSemanticContract("Invalid policy condition.");
            }
        }

        void ValidateEffectiveAuthorization(SemanticAuthorization? authorization, ImmutableArray<SemanticPolicy> policies, IEnumerable<SemanticProperty> properties)
        {
            if (ReferencesAnonymous(authorization, policies) && !IsAnonymous(authorization, policies))
            {
                throw new InvalidSemanticContract("An anonymous policy must be the whole effective authorization of a command or query.");
            }

            ValidateAuthorization(authorization, policies, properties);
        }

        void ValidateAuthorization(SemanticAuthorization? authorization, ImmutableArray<SemanticPolicy> policies, IEnumerable<SemanticProperty> properties)
        {
            switch (authorization)
            {
                case null: return;
                case SemanticPolicyReference reference:
                    var policy = policies.SingleOrDefault(value => value.Name == reference.Name) ??
                        throw new InvalidSemanticContract($"Authorization policy '{reference.Name}' is unresolved.");
                    if (properties.Any(property => property.IsGenerated && property.IsIdentifier) && UsesSubject(policy.Condition))
                    {
                        throw new InvalidSemanticContract($"Policy '{policy.Name}' cannot reference the subject of a generated identifier before generation.");
                    }
                    foreach (var read in policy.Reads.IsDefault ? [] : policy.Reads)
                    {
                        if (properties.All(property => property.Name != read.By || property.IsGenerated)) throw new InvalidSemanticContract($"Policy '{policy.Name}' read key '{read.By}' is not an input of the authorized command or query.");
                    }
                    foreach (var path in ArtifactPaths(policy.Condition))
                    {
                        if (!ResolvesPolicyPath(path, properties)) throw new InvalidSemanticContract($"Policy '{policy.Name}' artifact path '{path}' is unresolved.");
                    }

                    break;
                case SemanticLogicalAuthorization logical when logical.Operator is SemanticLogicalOperator.And or SemanticLogicalOperator.Or:
                    ValidateAuthorization(logical.Left, policies, properties);
                    ValidateAuthorization(logical.Right, policies, properties);
                    break;
                default: throw new InvalidSemanticContract("Invalid authorization expression.");
            }
        }

        bool ResolvesPolicyPath(string path, IEnumerable<SemanticProperty> properties)
        {
            var parts = path.Split('.');
            if (parts.Any(part => part.Length == 0)) return false;
            var property = properties.SingleOrDefault(value => value.Name == parts[0]);
            if (property?.IsGenerated != false) return false;
            foreach (var name in parts.Skip(1))
            {
                if (property.Type.Kind != SemanticTypeReferenceKind.CompositeType || property.Type.IsCollection ||
                    !_types.TryGetValue(property.Type.Target, out var composite))
                {
                    return false;
                }
                property = composite.Properties.SingleOrDefault(value => value.Name == name);
                if (property is null) return false;
            }

            return true;
        }

        void ValidatePolicies(SemanticApplication application)
        {
            RequireObjects(application.Policies, nameof(application.Policies), "policy");
            RejectDuplicateNames(application.Policies.Select(policy => policy.Name), "policy");
            foreach (var policy in application.Policies)
            {
                ValidatePolicyCondition(policy.Condition);
                if (!policy.Reads.IsDefaultOrEmpty)
                {
                    RejectDuplicateNames(policy.Reads.Select(read => read.Name), $"policy '{policy.Name}' read");
                    var readModels = AllSlices(application).SelectMany(slice => slice.ReadModels).Select(model => model.Id).ToHashSet();
                    foreach (var read in policy.Reads)
                    {
                        if (string.IsNullOrWhiteSpace(read.Name) || string.IsNullOrWhiteSpace(read.By)) throw new InvalidSemanticContract($"A read of policy '{policy.Name}' requires an alias and a key property.");
                        if (!readModels.Contains(read.ReadModel)) throw new InvalidSemanticContract($"Policy '{policy.Name}' reads an unknown read model.");
                    }
                }
                if (policy.Condition is SemanticOpaquePolicyCondition opaque &&
                    (!_semanticVersion.IsAtLeast(SemanticVersion.V3) || string.IsNullOrWhiteSpace(opaque.RequirementId)))
                {
                    throw new InvalidSemanticContract($"Policy '{policy.Name}' requires ESM v3 and a requirement identity.");
                }
            }
            var policyRequirements = new HashSet<string>(StringComparer.Ordinal);
            foreach (var policy in application.Policies)
            {
                if (policy.Condition is SemanticOpaquePolicyCondition opaque && !policyRequirements.Add(opaque.RequirementId))
                {
                    throw new InvalidSemanticContract("Policies have duplicate requirement identities.");
                }
            }
            foreach (var slice in AllSlices(application))
            {
                foreach (var command in slice.Commands) ValidateEffectiveAuthorization(command.Authorization, application.Policies, command.Properties);
                foreach (var query in slice.Queries)
                {
                    ValidateEffectiveAuthorization(
                        query.Authorization,
                        application.Policies,
                        SemanticQueryArguments.KeyProperties(query));
                }
                foreach (var specification in slice.Specifications)
                {
                    var authorizedCommand = specification.When is not null && slice.Commands.Any(command => command.Id == specification.When.Command && RequiresCaller(command.Authorization, application.Policies));
                    var authorizedQuery = specification.ThenQueries.Any(result => slice.Queries.Any(query => query.Id == result.Query && RequiresCaller(query.Authorization, application.Policies)));
                    if ((authorizedCommand || authorizedQuery) && specification.GivenCaller is null)
                    {
                        throw new InvalidSemanticContract("An authorized specification requires an explicit caller.");
                    }
                }
            }
        }
    }
}
