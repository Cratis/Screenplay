// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PersonaSyntax } from './Authorization';
import { ApplicationSyntax } from './Structure';
import { PolicyConditionSyntax, PolicySyntax } from './Policies';
import { PersonaCallerContribution } from './PersonaCallerContribution';
import { PersonaCallerRefusal } from './PersonaCallerRefusal';
import { PersonaCallerResult } from './PersonaCallerResult';
import { SpecificationCallerClaimSyntax } from './Specifications';

const roleClaimType = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const ordinal = (left: string, right: string): number => left < right ? -1 : left > right ? 1 : 0;
// Ordinal casing never expands a character (for example sharp s into SS).
const claimTypeKey = (type: string): string => Array.from(type, character => {
    const upper = character.toUpperCase();
    return upper.length === character.length && character !== '\u0131' && character !== '\u017f' ? upper : character;
}).join('');

export function synthesizePersonaCaller(persona: PersonaSyntax, application: ApplicationSyntax): PersonaCallerResult {
    const contributions: PersonaCallerContribution[] = [];
    const policies: PolicySyntax[] = [];
    const refused = (refusal: PersonaCallerRefusal): PersonaCallerResult => ({ caller: null, contributions, refusal });
    if (persona.policies.length === 0) return refused({ policy: null, reason: 'noPolicies', location: persona.location });
    for (const name of persona.policies) {
        const policy = (application.policies ?? []).find(policy => policy.name === name);
        if (!policy) return refused({ policy: name, reason: 'unresolvedPolicy', location: persona.location });
        if (policy.condition === null || policy.code !== null || policy.file !== null) return refused({ policy: name, reason: 'opaqueImplementation', location: policy.location });
        const not = negation(policy.condition);
        if (not) return refused({ policy: name, reason: 'negation', location: not.location });
        policies.push(policy);
    }
    for (const policy of policies) {
        const refusal = required(policy.condition!, policy.name, contributions);
        if (refusal) return refused(refusal);
    }
    for (const policy of policies) {
        const refusal = complete(policy.condition!, policy.name, contributions);
        if (refusal) return refused(refusal);
    }
    const roles = [...new Set(contributions.filter(atom => atom.kind === 'role').map(atom => atom.value))].sort(ordinal);
    const claims: SpecificationCallerClaimSyntax[] = [];
    for (const atom of contributions.filter(atom => atom.kind === 'claim')) {
        if (!claims.some(claim => claimTypeKey(claim.type) === claimTypeKey(atom.type!) && claim.value === atom.value)) {
            claims.push({ kind: 'SpecificationCallerClaimSyntax', type: atom.type!, value: atom.value, location: atom.location });
        }
    }
    claims.sort((left, right) => ordinal(claimTypeKey(left.type), claimTypeKey(right.type)) || ordinal(left.value, right.value));
    return { caller: { kind: 'SpecificationCallerSyntax', authenticated: true, roles, claims, location: persona.location }, contributions, refusal: null };
}

function negation(condition: PolicyConditionSyntax): PolicyConditionSyntax | null {
    return condition.kind === 'NotPolicyConditionSyntax' ? condition : condition.kind === 'LogicalPolicyConditionSyntax' ? negation(condition.left) ?? negation(condition.right) : null;
}

function atom(condition: PolicyConditionSyntax, policy: string, values: PersonaCallerContribution[]): PersonaCallerRefusal | null {
    switch (condition.kind) {
        case 'AuthenticatedConditionSyntax': values.push({ kind: 'authenticated', value: 'true', type: null, policy, location: condition.location }); return null;
        case 'RoleConditionSyntax': values.push({ kind: 'role', value: condition.role, type: null, policy, location: condition.location }); return null;
        case 'ClaimConditionSyntax':
            if (claimTypeKey(condition.claim) === claimTypeKey(roleClaimType)) return { policy, reason: 'roleClaim', location: condition.location };
            if (!condition.matchesSubject && condition.matches?.kind === 'LiteralExpressionSyntax' && typeof condition.matches.value === 'string') {
                values.push({ kind: 'claim', value: condition.matches.value, type: condition.claim, policy, location: condition.location }); return null;
            }
    }
    return { policy, reason: 'nonLiteralClaim', location: condition.location };
}

function required(condition: PolicyConditionSyntax, policy: string, values: PersonaCallerContribution[]): PersonaCallerRefusal | null {
    return condition.kind !== 'LogicalPolicyConditionSyntax' ? atom(condition, policy, values) : condition.operator === 'Or' ? null : required(condition.left, policy, values) ?? required(condition.right, policy, values);
}

function satisfied(condition: PolicyConditionSyntax, values: readonly PersonaCallerContribution[]): boolean {
    switch (condition.kind) {
        case 'AuthenticatedConditionSyntax': return true;
        case 'RoleConditionSyntax': return values.some(atom => atom.kind === 'role' && atom.value === condition.role);
        case 'ClaimConditionSyntax': {
            const target = condition.matches;
            return !condition.matchesSubject && target?.kind === 'LiteralExpressionSyntax' && values.some(atom => atom.kind === 'claim' && claimTypeKey(atom.type!) === claimTypeKey(condition.claim) && atom.value === target.value);
        }
        case 'LogicalPolicyConditionSyntax': return condition.operator === 'And' ? satisfied(condition.left, values) && satisfied(condition.right, values) : satisfied(condition.left, values) || satisfied(condition.right, values);
        default: return false;
    }
}

function complete(condition: PolicyConditionSyntax, policy: string, values: PersonaCallerContribution[]): PersonaCallerRefusal | null {
    if (condition.kind !== 'LogicalPolicyConditionSyntax') return satisfied(condition, values) ? null : atom(condition, policy, values);
    if (condition.operator === 'And') return complete(condition.left, policy, values) ?? complete(condition.right, policy, values);
    if (satisfied(condition, values)) return null;
    const left = [...values];
    const first = complete(condition.left, policy, left);
    if (first === null) { values.push(...left.slice(values.length)); return null; }
    const right = [...values];
    if (complete(condition.right, policy, right) !== null) return first;
    values.push(...right.slice(values.length));
    return null;
}
