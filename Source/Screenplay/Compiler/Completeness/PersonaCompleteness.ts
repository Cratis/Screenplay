// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthorizeSyntax, PolicyRequirementSyntax } from '../Syntax/Authorization';
import { synthesizePersonaCaller } from '../Syntax/PersonaCallers';
import { PolicyConditionSyntax } from '../Syntax/Policies';
import { SpecificationCallerSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { combine, conditionTruth, requirementTruth, Truth } from './AuthorizationFacts';
import { ordinalClaimKey, personaAmbiguities } from './PersonaWitness';

interface Gate {
    readonly kind: string;
    readonly name: string;
    readonly location: SourceLocation;
    readonly gates: readonly AuthorizeSyntax[];
}

function gatesIn(features: readonly FeatureSyntax[], inherited: readonly AuthorizeSyntax[]): readonly Gate[] {
    return features.flatMap(feature => {
        const gates = feature.authorize === null ? inherited : [...inherited, feature.authorize];
        return [...gatesIn(feature.features, gates), ...feature.slices.flatMap(slice => [
            ...slice.commands.map(command => ({ kind: 'Command', name: command.name, location: command.location, gates: command.authorize === null ? gates : [...gates, command.authorize] })),
            ...slice.queries.map(query => ({ kind: 'Query', name: query.name, location: query.location, gates: query.authorize === null ? gates : [...gates, query.authorize] })),
            ...slice.screens.map(screen => ({ kind: 'Screen', name: screen.name, location: screen.location, gates })),
        ])];
    });
}

function references(requirement: PolicyRequirementSyntax): readonly string[] {
    return requirement.kind === 'PolicyReferenceSyntax' ? [requirement.name] : [...references(requirement.left), ...references(requirement.right)];
}

function evaluate(gates: readonly AuthorizeSyntax[], caller: SpecificationCallerSyntax, application: ApplicationSyntax): Truth {
    return gates.reduce<Truth>((truth, gate) => combine(truth, 'And', requirementTruth(gate.requirement, application.policies ?? [], condition => conditionTruth(condition, atom => {
        if (atom.kind === 'AuthenticatedConditionSyntax') return caller.authenticated;
        if (atom.kind === 'RoleConditionSyntax') return caller.roles.includes(atom.role);
        if (atom.kind === 'ClaimConditionSyntax' && !atom.matchesSubject && atom.matches?.kind === 'LiteralExpressionSyntax' && typeof atom.matches.value === 'string') {
            const value = atom.matches.value;
            return caller.claims.some(claim => ordinalClaimKey(claim.type) === ordinalClaimKey(atom.claim) && claim.value === value);
        }
        return null;
    }))), true);
}

// Only buildable alternatives reach this formatter: authenticated, roles, literal claims and combinations.
function conditionText(condition: PolicyConditionSyntax): string {
    const quote = (value: string) => '"' + value.replace(/[\\"\n\r\t]/g, character => ({ '\\': '\\\\', '"': '\\"', '\n': '\\n', '\r': '\\r', '\t': '\\t' })[character]!) + '"';
    if (condition.kind === 'AuthenticatedConditionSyntax') return 'authenticated';
    if (condition.kind === 'RoleConditionSyntax') return `role ${quote(condition.role)}`;
    if (condition.kind === 'ClaimConditionSyntax') return `claim ${quote(condition.claim)} matches ${quote(String(condition.matches?.kind === 'LiteralExpressionSyntax' ? condition.matches.value : ''))}`;
    if (condition.kind === 'NotPolicyConditionSyntax') return `not ${conditionText(condition.operand)}`;
    const left = conditionText(condition.left);
    const right = conditionText(condition.right);
    return `${condition.left.kind === 'LogicalPolicyConditionSyntax' && condition.left.operator !== condition.operator ? `(${left})` : left} ${condition.operator.toLowerCase()} ${condition.right.kind === 'LogicalPolicyConditionSyntax' ? `(${right})` : right}`;
}

export function personaDiagnostics(application: ApplicationSyntax): readonly Diagnostic[] {
    const personas = application.personas.map(persona => ({ persona, result: synthesizePersonaCaller(persona, application) }));
    if (personas.length === 0) return [];
    const gates = application.modules.flatMap(module => gatesIn(module.features, module.authorize === null ? [] : [module.authorize]));
    const used = new Set(gates.flatMap(gate => gate.gates.flatMap(authorize => references(authorize.requirement))));
    const diagnostics: Diagnostic[] = [];
    for (const { persona } of personas) {
        if (!persona.policies.some(policy => used.has(policy))) diagnostics.push({ severity: 'warning', code: DiagnosticCodes.PersonaWithoutGate,
            message: `Persona '${persona.name}' has no policy used by a command, query, or inherited screen gate.`, location: persona.location });
        for (const ambiguity of personaAmbiguities(persona, application)) diagnostics.push({ severity: 'information', code: DiagnosticCodes.AmbiguousPersonaCaller,
            message: `Persona '${persona.name}' policy '${ambiguity.policy}' has an unchosen buildable alternative '${conditionText(ambiguity.alternative)}'; add a policy that pins it.`, location: ambiguity.location });
    }
    for (const gate of gates.filter(gate => gate.kind !== 'Screen' && gate.gates.length > 0 && personas.every(({ result }) => result.caller !== null && evaluate(gate.gates, result.caller, application) === false))) {
        diagnostics.push({ severity: 'warning', code: DiagnosticCodes.GateWithoutPersona,
            message: `${gate.kind} '${gate.name}' is definitely denied to every declared persona's synthesized caller.`, location: gate.location });
    }
    return diagnostics;
}
