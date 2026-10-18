// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PersonaSyntax } from '../Syntax/Authorization';
import { PersonaCallerContribution } from '../Syntax/PersonaCallerContribution';
import { PolicyConditionSyntax } from '../Syntax/Policies';
import { synthesizePersonaCaller } from '../Syntax/PersonaCallers';
import { ApplicationSyntax } from '../Syntax/Structure';

interface Ambiguity {
    readonly policy: string;
    readonly alternative: PolicyConditionSyntax;
    readonly location: SourceLocation;
}

// The public TS witness has no ambiguity payload. Replay the same syntax-only choice algorithm
// locally, retaining partial ambiguities on refusal, without changing persona synthesis or its API.
export function personaAmbiguities(persona: PersonaSyntax, application: ApplicationSyntax): readonly Ambiguity[] {
    const policies = persona.policies.map(name => application.policies?.find(policy => policy.name === name));
    const refusedBeforeChoices = policies.some(policy => !policy || policy.condition === null || policy.code !== null || policy.file !== null || hasNegation(policy.condition));
    if (refusedBeforeChoices) return [];
    const required: PersonaCallerContribution[] = [];
    for (const policy of policies) if (!requiredAtoms(policy!.condition!, policy!.name, required, application)) return [];
    const values = [...required];
    const ambiguities: Ambiguity[] = [];
    for (const policy of policies) if (!complete(policy!.condition!, policy!.name, values, required, ambiguities, application)) break;
    return ambiguities;
}

function hasNegation(condition: PolicyConditionSyntax): boolean {
    return condition.kind === 'NotPolicyConditionSyntax' || condition.kind === 'LogicalPolicyConditionSyntax' && (hasNegation(condition.left) || hasNegation(condition.right));
}

function atom(condition: PolicyConditionSyntax, policy: string, values: PersonaCallerContribution[], application: ApplicationSyntax): boolean {
    // Delegate atom acceptance and ordinal claim handling to the existing portable witness.
    const result = synthesizePersonaCaller({ kind: 'PersonaSyntax', name: '', description: null, policies: [policy], location: condition.location }, {
        ...application, policies: [{ kind: 'PolicySyntax', name: policy, condition, code: null, file: null, location: condition.location }],
    });
    if (result.caller === null) return false;
    values.push(...result.contributions);
    return true;
}

function requiredAtoms(condition: PolicyConditionSyntax, policy: string, values: PersonaCallerContribution[], application: ApplicationSyntax): boolean {
    if (condition.kind !== 'LogicalPolicyConditionSyntax') return atom(condition, policy, values, application);
    return condition.operator === 'Or' || requiredAtoms(condition.left, policy, values, application) && requiredAtoms(condition.right, policy, values, application);
}

function satisfied(condition: PolicyConditionSyntax, values: readonly PersonaCallerContribution[]): boolean {
    switch (condition.kind) {
        case 'AuthenticatedConditionSyntax': return true;
        case 'RoleConditionSyntax': return values.some(value => value.kind === 'role' && value.value === condition.role);
        case 'ClaimConditionSyntax': {
            const target = condition.matches;
            // The synthesis API already canonicalizes claim types using ordinal casing.
            return !condition.matchesSubject && target?.kind === 'LiteralExpressionSyntax' && typeof target.value === 'string' && values.some(value => value.kind === 'claim' && ordinalClaimKey(value.type!) === ordinalClaimKey(condition.claim) && value.value === target.value);
        }
    }
    // Negations are refused before any choice is made, so only logical conditions remain.
    return condition.kind === 'LogicalPolicyConditionSyntax' && (condition.operator === 'And' ? satisfied(condition.left, values) && satisfied(condition.right, values) : satisfied(condition.left, values) || satisfied(condition.right, values));
}

export const ordinalClaimKey = (type: string): string => Array.from(type, character => {
    const upper = character.toUpperCase();
    return upper.length === character.length && character !== '\u0131' && character !== '\u017f' ? upper : character;
}).join('');

function complete(condition: PolicyConditionSyntax, policy: string, values: PersonaCallerContribution[], required: readonly PersonaCallerContribution[], ambiguities: Ambiguity[], application: ApplicationSyntax): boolean {
    if (condition.kind !== 'LogicalPolicyConditionSyntax') return satisfied(condition, values) || atom(condition, policy, values, application);
    if (condition.operator === 'And') return complete(condition.left, policy, values, required, ambiguities, application) && complete(condition.right, policy, values, required, ambiguities, application);
    if (satisfied(condition, values)) return true;
    const left = [...values];
    const right = [...values];
    const leftAmbiguities: Ambiguity[] = [];
    const rightAmbiguities: Ambiguity[] = [];
    const first = complete(condition.left, policy, left, required, leftAmbiguities, application);
    const second = complete(condition.right, policy, right, required, rightAmbiguities, application);
    if (!first && !second) return false;
    if (first) {
        if (second && !satisfied(condition, required)) ambiguities.push({ policy, alternative: condition.right, location: condition.location });
        values.push(...left.slice(values.length));
        ambiguities.push(...leftAmbiguities);
    } else {
        values.push(...right.slice(values.length));
        ambiguities.push(...rightAmbiguities);
    }
    return true;
}
