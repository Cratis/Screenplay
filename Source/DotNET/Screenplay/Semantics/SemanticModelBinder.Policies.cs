// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        ImmutableArray<SemanticPolicy> BindPolicies() => syntax.Policies.Select(policy =>
        {
            if (policy.Code is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' uses csharp; portable implementation attachments are deferred to #139.", policy.Code.Location);
            }

            var condition = policy.Condition is null ? null : BindPolicyCondition(policy.Condition);
            return condition is null ? null : new SemanticPolicy(policy.Name, condition);
        }).Where(policy => policy is not null).Select(policy => policy!).ToImmutableArray();

        SemanticPolicyCondition? BindPolicyCondition(PolicyConditionSyntax syntaxCondition) => syntaxCondition switch
        {
            AuthenticatedConditionSyntax => new SemanticAuthenticatedCondition(),
            RoleConditionSyntax role => new SemanticRoleCondition(role.Role),
            ClaimConditionSyntax { MatchesSubject: true } claim => new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Subject, null),
            ClaimConditionSyntax { Matches: LiteralExpressionSyntax { Value: string value } } claim =>
                new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Literal, value),
            ClaimConditionSyntax { Matches: PathExpressionSyntax path } claim =>
                new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Artifact, path.Path),
            LogicalPolicyConditionSyntax logical => BindLogicalPolicy(logical),
            _ => UnsupportedPolicyCondition(syntaxCondition)
        };

        SemanticPolicyCondition? BindLogicalPolicy(LogicalPolicyConditionSyntax logical)
        {
            var left = BindPolicyCondition(logical.Left);
            var right = BindPolicyCondition(logical.Right);
            return left is null || right is null ? null :
                new SemanticLogicalPolicyCondition(left, PolicyOperator(logical.Operator), right);
        }

        SemanticPolicyCondition? UnsupportedPolicyCondition(PolicyConditionSyntax condition)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Policy claim target is not a portable literal, subject, or artifact path.", condition.Location);
            return null;
        }

        static SemanticLogicalOperator PolicyOperator(LogicalOperator op) => op == LogicalOperator.And
            ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or;

        SemanticAuthorization? EffectiveAuthorization(AuthorizeSyntax? own, IEnumerable<string> artifactProperties, string moduleName, ImmutableArray<string> featurePath)
        {
            var module = syntax.Modules.Single(value => value.Name == moduleName);
            var scopes = new List<AuthorizeSyntax?> { module.Authorize };
            var features = module.Features;
            foreach (var name in featurePath)
            {
                var feature = features.Single(value => value.Name == name);
                scopes.Add(feature.Authorize);
                features = feature.Features;
            }

            scopes.Add(own);
            SemanticAuthorization? result = null;
            foreach (var scope in scopes)
            {
                var bound = BindAuthorization(scope, artifactProperties);
                if (bound is not null)
                {
                    result = result is null ? bound : new SemanticLogicalAuthorization(result, SemanticLogicalOperator.And, bound);
                }
            }

            return result;
        }

        SemanticAuthorization? BindAuthorization(AuthorizeSyntax? authorize, IEnumerable<string> artifactProperties)
        {
            if (authorize is null) return null;
            var names = artifactProperties.ToHashSet(StringComparer.Ordinal);
            return BindAuthorizationNode(authorize.Requirement, names);
        }

        SemanticAuthorization? BindAuthorizationNode(PolicyRequirementSyntax requirement, HashSet<string> properties)
        {
            if (requirement is LogicalPolicyRequirementSyntax logical)
            {
                var left = BindAuthorizationNode(logical.Left, properties);
                var right = BindAuthorizationNode(logical.Right, properties);
                return left is null || right is null ? null :
                    new SemanticLogicalAuthorization(left, PolicyOperator(logical.Operator), right);
            }

            if (requirement is not PolicyReferenceSyntax reference) return null;
            var policy = syntax.Policies.SingleOrDefault(policy => policy.Name == reference.Name);
            if (policy is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Authorization policy '{reference.Name}' is unresolved.", reference.Location);
                return null;
            }

            foreach (var path in PolicyPaths(policy.Condition))
            {
                if (!properties.Contains(path))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' artifact path '{path}' does not resolve against the authorized command properties or query arguments.", reference.Location);
                }
            }

            return new SemanticPolicyReference(reference.Name);
        }

        static IEnumerable<string> PolicyPaths(PolicyConditionSyntax? condition) => condition switch
        {
            ClaimConditionSyntax { Matches: PathExpressionSyntax path } => [path.Path],
            LogicalPolicyConditionSyntax logical => PolicyPaths(logical.Left).Concat(PolicyPaths(logical.Right)),
            _ => []
        };
    }
}
