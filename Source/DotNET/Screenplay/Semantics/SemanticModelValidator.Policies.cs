// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        static void ValidateAuthorization(SemanticAuthorization? authorization, ImmutableArray<SemanticPolicy> policies, IEnumerable<string> properties)
        {
            switch (authorization)
            {
                case null: return;
                case SemanticPolicyReference reference:
                    var policy = policies.SingleOrDefault(value => value.Name == reference.Name) ??
                        throw new InvalidSemanticContract($"Authorization policy '{reference.Name}' is unresolved.");
                    foreach (var path in ArtifactPaths(policy.Condition))
                    {
                        if (!properties.Contains(path, StringComparer.Ordinal)) throw new InvalidSemanticContract($"Policy '{policy.Name}' artifact path '{path}' is unresolved.");
                    }
                    break;
                case SemanticLogicalAuthorization logical when logical.Operator is SemanticLogicalOperator.And or SemanticLogicalOperator.Or:
                    ValidateAuthorization(logical.Left, policies, properties);
                    ValidateAuthorization(logical.Right, policies, properties);
                    break;
                default: throw new InvalidSemanticContract("Invalid authorization expression.");
            }
        }

        static IEnumerable<string> ArtifactPaths(SemanticPolicyCondition condition) => condition switch
        {
            SemanticClaimCondition { TargetKind: SemanticClaimTargetKind.Artifact, Value: { } path } => [path],
            SemanticLogicalPolicyCondition logical => ArtifactPaths(logical.Left).Concat(ArtifactPaths(logical.Right)),
            _ => []
        };

        static void ValidatePolicyCondition(SemanticPolicyCondition? condition)
        {
            switch (condition)
            {
                case SemanticAuthenticatedCondition: break;
                case SemanticRoleCondition { Role: { } }: break;
                case SemanticClaimCondition { Claim: { }, TargetKind: SemanticClaimTargetKind.Subject, Value: null }: break;
                case SemanticClaimCondition { Claim: { }, TargetKind: SemanticClaimTargetKind.Literal or SemanticClaimTargetKind.Artifact, Value: { } }: break;
                case SemanticLogicalPolicyCondition logical when logical.Operator is SemanticLogicalOperator.And or SemanticLogicalOperator.Or:
                    ValidatePolicyCondition(logical.Left);
                    ValidatePolicyCondition(logical.Right);
                    break;
                default: throw new InvalidSemanticContract("Invalid policy condition.");
            }
        }

        void ValidatePolicies(SemanticApplication application)
        {
            RequireObjects(application.Policies, nameof(application.Policies), "policy");
            RejectDuplicateNames(application.Policies.Select(policy => policy.Name), "policy");
            foreach (var policy in application.Policies) ValidatePolicyCondition(policy.Condition);
            foreach (var slice in AllSlices(application))
            {
                foreach (var command in slice.Commands) ValidateAuthorization(command.Authorization, application.Policies, command.Properties.Select(property => property.Name));
                foreach (var query in slice.Queries) ValidateAuthorization(query.Authorization, application.Policies, [query.Argument.Name]);
                foreach (var specification in slice.Specifications)
                {
                    var authorizedCommand = specification.When is not null && slice.Commands.Any(command => command.Id == specification.When.Command && command.Authorization is not null);
                    var authorizedQuery = specification.ThenQueries.Any(result => slice.Queries.Any(query => query.Id == result.Query && query.Authorization is not null));
                    if ((authorizedCommand || authorizedQuery) && specification.GivenCaller is null)
                    {
                        throw new InvalidSemanticContract("An authorized specification requires an explicit caller.");
                    }
                }
            }
        }
    }
}
