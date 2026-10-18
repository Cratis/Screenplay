// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthorizeSyntax, PolicyRequirementSyntax } from '../Syntax/Authorization';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { synthesizePersonaCaller } from '../Syntax/PersonaCallers';
import { AuthenticatedConditionSyntax, ClaimConditionSyntax, PolicyConditionSyntax, RoleConditionSyntax } from '../Syntax/Policies';
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

const quote = (value: string) => '"' + value.replace(/[\\"\n\r\t]/g, character => ({ '\\': '\\\\', '"': '\\"', '\n': '\\n', '\r': '\\r', '\t': '\\t' })[character]!) + '"';

function expressionText(expression: ExpressionSyntax): string {
    switch (expression.kind) {
        case 'LiteralExpressionSyntax': {
            const value = expression.value;
            if (typeof value === 'string') return quote(value);
            if (value !== null && typeof value === 'object') return value.value;
            return typeof value === 'number' ? numberText(value) : String(value);
        }
        case 'PathExpressionSyntax': return expression.path;
        case 'ContextExpressionSyntax': return `$context.${expression.path}`;
        case 'IdentityExpressionSyntax': return `$identity.${expression.path}`;
        case 'EnvironmentExpressionSyntax': return `$env.${expression.name}`;
        case 'StringsExpressionSyntax': return `$strings.${expression.key}`;
        case 'SourceItemExpressionSyntax': return `$.${expression.path}`;
        case 'EventSourceIdExpressionSyntax': return '$eventSourceId';
        case 'EventContextExpressionSyntax': return `$eventContext.${expression.path}`;
        case 'CausedByExpressionSyntax': return expression.property === null ? '$causedBy' : `$causedBy.${expression.property}`;
        case 'RefusalExpressionSyntax': return `$refusal.${expression.member}`;
        case 'CaseValueExpressionSyntax': return `case.${expression.parameter}`;
        case 'RawExpressionSyntax': return expression.text;
        case 'TemplateExpressionSyntax': return '`' + expression.parts.map(part => part.kind === 'TemplateTextSyntax' ? part.text : '${' + expressionText(part.expression) + '}').join('') + '`';
        case 'ListExpressionSyntax': return `[${expression.items.map(structuredText).join(',')}]`;
        case 'ObjectExpressionSyntax': return `{${expression.members.map(member => `${JSON.stringify(member.name)}:${structuredText(member.value)}`).join(',')}}`;
    }
}

function structuredText(expression: ExpressionSyntax): string {
    return expression.kind === 'LiteralExpressionSyntax' && typeof expression.value === 'string' ? JSON.stringify(expression.value) : expressionText(expression);
}

function numberText(value: number): string {
    if (Number.isInteger(value) && value >= -9223372036854775808 && value < 9223372036854775808) return BigInt(value).toString();
    const text = String(value);
    if (!text.includes('e')) return text;
    const [mantissa, exponent] = text.split('e');
    const negative = mantissa.startsWith('-');
    const unsigned = negative ? mantissa.slice(1) : mantissa;
    const digits = unsigned.replace('.', '');
    const decimalAt = (unsigned.includes('.') ? unsigned.indexOf('.') : unsigned.length) + Number(exponent);
    const expanded = decimalAt <= 0 ? '0.' + '0'.repeat(-decimalAt) + digits
        : decimalAt >= digits.length ? digits + '0'.repeat(decimalAt - digits.length) : digits.slice(0, decimalAt) + '.' + digits.slice(decimalAt);
    return (negative ? '-' : '') + expanded;
}

// A buildable alternative can contain non-buildable claim branches inside a nested or.
// Negations are refused before any alternative is chosen, so the not form is kept only for completeness.
function conditionText(condition: PolicyConditionSyntax): string {
    if (condition.kind !== 'LogicalPolicyConditionSyntax') return condition.kind === 'NotPolicyConditionSyntax' ? `not ${conditionText(condition.operand)}` : atomText(condition);
    const left = conditionText(condition.left);
    const right = conditionText(condition.right);
    return `${condition.left.kind === 'LogicalPolicyConditionSyntax' && condition.left.operator !== condition.operator ? `(${left})` : left} ${condition.operator.toLowerCase()} ${condition.right.kind === 'LogicalPolicyConditionSyntax' ? `(${right})` : right}`;
}

function atomText(condition: AuthenticatedConditionSyntax | RoleConditionSyntax | ClaimConditionSyntax): string {
    if (condition.kind === 'AuthenticatedConditionSyntax') return 'authenticated';
    if (condition.kind === 'RoleConditionSyntax') return `role ${quote(condition.role)}`;
    return `claim ${quote(condition.claim)} matches ${condition.matchesSubject ? 'subject' : condition.matches === null ? quote('') : expressionText(condition.matches)}`;
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
