// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PolicyRequirementSyntax } from '../Syntax/Authorization';
import { PolicyConditionSyntax, PolicySyntax } from '../Syntax/Policies';

export type Truth = boolean | null;

export function combine(left: Truth, operator: 'And' | 'Or', right: Truth): Truth {
    if (operator === 'And') return left === false || right === false ? false : left === true && right === true ? true : null;
    return left === true || right === true ? true : left === false && right === false ? false : null;
}

export function policyOf(name: string, policies: readonly PolicySyntax[]): PolicyConditionSyntax | null {
    const policy = policies.find(policy => policy.name === name);
    return policy?.code === null && policy.file === null ? policy.condition : null;
}

export function requirementTruth(requirement: PolicyRequirementSyntax, policies: readonly PolicySyntax[], condition: (condition: PolicyConditionSyntax) => Truth): Truth {
    if (requirement.kind === 'LogicalPolicyRequirementSyntax') return combine(requirementTruth(requirement.left, policies, condition), requirement.operator, requirementTruth(requirement.right, policies, condition));
    const value = policyOf(requirement.name, policies);
    return value === null ? null : condition(value);
}

export function conditionTruth(condition: PolicyConditionSyntax, atom: (condition: PolicyConditionSyntax) => Truth): Truth {
    if (condition.kind === 'LogicalPolicyConditionSyntax') return combine(conditionTruth(condition.left, atom), condition.operator, conditionTruth(condition.right, atom));
    if (condition.kind === 'NotPolicyConditionSyntax') {
        const truth = conditionTruth(condition.operand, atom);
        return truth === null ? null : !truth;
    }
    return atom(condition);
}
