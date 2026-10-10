// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

// Completeness and MCP inspect authoring models that need not bind. Project only authorization
// declarations to the portable gate vocabulary; this does not admit or execute the application.
internal sealed class AuthorizationRoleAnalysis(ApplicationSyntax application)
{
    internal SemanticPolicy[] Policies { get; } = [.. application.Policies.Select(policy => new SemanticPolicy(
        policy.Name,
        policy.Code is not null || policy.File is not null || policy.Condition is null
            ? new SemanticOpaquePolicyCondition(policy.Name) : Condition(policy.Condition)))];

    internal (string Module, CommandSyntax Command, SemanticAuthorization? Gate)[] Commands { get; } =
        [.. application.Modules.SelectMany(module => CommandsIn(module.Name, module.Features, Gate(module.Authorize)))];

    static IEnumerable<(string Module, CommandSyntax Command, SemanticAuthorization? Gate)> CommandsIn(string module, IEnumerable<FeatureSyntax> features, SemanticAuthorization? inherited)
    {
        foreach (var feature in features)
        {
            var gate = And(inherited, Gate(feature.Authorize));
            foreach (var command in feature.Slices.SelectMany(slice => slice.Commands)) yield return (module, command, And(gate, Gate(command.Authorize)));
            foreach (var command in CommandsIn(module, feature.Features, gate)) yield return command;
        }
    }

    static SemanticAuthorization? And(SemanticAuthorization? left, SemanticAuthorization? right)
    {
        if (left is null) return right;
        return right is null ? left : new SemanticLogicalAuthorization(left, SemanticLogicalOperator.And, right);
    }

    static SemanticAuthorization? Gate(AuthorizeSyntax? authorize) => authorize is null ? null : Requirement(authorize.Requirement);

    static SemanticAuthorization Requirement(PolicyRequirementSyntax requirement) => requirement switch
    {
        PolicyReferenceSyntax reference => new SemanticPolicyReference(reference.Name),
        LogicalPolicyRequirementSyntax logical => new SemanticLogicalAuthorization(Requirement(logical.Left), Operator(logical.Operator), Requirement(logical.Right)),
        _ => new SemanticPolicyReference(string.Empty)
    };

    static SemanticPolicyCondition Condition(PolicyConditionSyntax condition) => condition switch
    {
        AuthenticatedConditionSyntax => new SemanticAuthenticatedCondition(),
        RoleConditionSyntax role => new SemanticRoleCondition(role.Role),
        NotPolicyConditionSyntax not => new SemanticNotPolicyCondition(Condition(not.Operand)),
        LogicalPolicyConditionSyntax logical => new SemanticLogicalPolicyCondition(Condition(logical.Left), Operator(logical.Operator), Condition(logical.Right)),
        ClaimConditionSyntax claim => new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Subject, null),
        _ => new SemanticOpaquePolicyCondition(string.Empty)
    };

    static SemanticLogicalOperator Operator(LogicalOperator op) => op == LogicalOperator.And ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or;
}
