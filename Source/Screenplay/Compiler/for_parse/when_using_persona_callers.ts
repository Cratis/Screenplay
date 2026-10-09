// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseSpecificationSource } from '../ScreenplayCompiler';
import { synthesizePersonaCaller } from '../Syntax/PersonaCallers';
import { expandEffectiveSpecificationExamples } from '../Parsing/SpecificationCommandExamples';

const synthesize = (...conditions: string[]) => {
    const source = conditions.map((condition, index) => `policy P${index}\n  require ${condition}`).join('\n') + '\npersona Person\n' + conditions.map((_, index) => `  policy P${index}`).join('\n');
    const application = parse(source).value;
    return synthesizePersonaCaller(application.personas[0], application);
};

describe('when using persona callers', () => {
    it('keeps a persona reference distinct from an empty caller', () => {
        const result = parseSpecificationSource('specification Allowed\n  given caller as Person');
        expect(result.success).toBe(true);
        expect(result.value[0].givenCallerPersona?.name).toBe('Person');
        expect(result.value[0].givenCaller).toBeNull();
    });
    it('accepts the existing Unicode identifier alphabet', () => {
        const result = parseSpecificationSource('specification X\n  given caller as Pärson');
        expect(result.success).toBe(true);
        expect(result.value[0].givenCallerPersona?.name).toBe('Pärson');
    });
    it('preserves the single caller rule', () => {
        const result = parseSpecificationSource('specification Allowed\n  given caller as Person\n  given caller');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0387']);
    });
    it('collects required atoms before alternatives', () => {
        const result = synthesize('role "A" or role "B"', 'role "B"');
        expect(result.caller?.roles).toEqual(['B']);
        expect(result.caller?.authenticated).toBe(true);
    });
    it('chooses only the leftmost buildable witness', () => expect(synthesize('role "A" or role "B"').caller?.roles).toEqual(['A']));
    it('skips a role URI alternative', () => expect(synthesize('claim "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" matches "A" or role "B"').caller?.roles).toEqual(['B']));
    it('refuses a required role URI ignoring case', () => expect(synthesize('claim "HTTP://SCHEMAS.MICROSOFT.COM/WS/2008/06/IDENTITY/CLAIMS/ROLE" matches "A"').refusal?.reason).toBe('roleClaim'));
    it('does not refuse a bare role claim type', () => expect(synthesize('claim "role" matches "A"').caller?.claims[0].type).toBe('role'));
    it('deduplicates repeated roles and literal claims', () => {
        const result = synthesize('role "Z" and role "A" and role "Z" and claim "dept" matches "A" and claim "DEPT" matches "A"');
        expect(result.caller?.roles).toEqual(['A', 'Z']);
        expect(result.caller?.claims).toHaveLength(1);
    });
    it('does not classify Unicode lookalikes as a role URI', () => {
        expect(synthesize('claim "http://schemas.microsoft.com/ws/2008/06/identıty/claims/role" matches "A"').refusal).toBeNull();
        expect(synthesize('claim "http://ſchemas.microsoft.com/ws/2008/06/identity/claims/role" matches "A"').refusal).toBeNull();
    });
    it('does not expand claim type casing', () => expect(synthesize('claim "ß" matches "A" and claim "SS" matches "A"').caller?.claims).toHaveLength(2));
    it('skips a nonliteral alternative', () => expect(synthesize('claim "owner" matches subject or role "B"').caller?.roles).toEqual(['B']));
    it('refuses a required nonliteral claim', () => expect(synthesize('role "A" and claim "owner" matches subject').refusal?.reason).toBe('nonLiteralClaim'));
    it('refuses negation even in an unused alternative', () => expect(synthesize('role "A" or not role "B"').refusal?.reason).toBe('negation'));
    it('uses the first unbuildable reason', () => expect(synthesize('claim "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" matches "A" or claim "owner" matches subject').refusal?.reason).toBe('roleClaim'));
    it('refuses policyless personas', () => expect(synthesize().refusal?.reason).toBe('noPolicies'));
    it('refuses missing and opaque policies', () => {
        const application = parse('persona Person\n  policy Missing').value;
        expect(synthesizePersonaCaller(application.personas[0], application).refusal?.reason).toBe('unresolvedPolicy');
        expect(synthesizePersonaCaller(application.personas[0], { ...application, policies: undefined }).refusal?.reason).toBe('unresolvedPolicy');
        const opaque = parse('policy Missing\n  file Policy.cs\npersona Person\n  policy Missing').value;
        expect(synthesizePersonaCaller(opaque.personas[0], opaque).refusal?.reason).toBe('opaqueImplementation');
    });
    it('expands callers with persona and policy provenance', () => {
        const application = parse('policy Member\n  require role "A"\npersona Person\n  policy Member\nmodule M\n  feature F\n    slice StateChange S\n      specification X\n        given caller as Person').value;
        const result = expandEffectiveSpecificationExamples(application).specifications[0];
        expect(result.effective.givenCaller?.roles).toEqual(['A']);
        expect(result.effective.givenCallerPersona).toBeUndefined();
        expect(result.steps[0].values[0]).toMatchObject({ origin: 'persona', persona: 'Person', policy: 'Member' });
    });
});
