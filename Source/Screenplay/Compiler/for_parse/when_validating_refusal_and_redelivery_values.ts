// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { RefusalDeclarations } from '../Parsing/RefusalDeclarations';
import { sameRedeliveryValue } from '../Parsing/RedeliveryValues';
import { parseMappingSource } from '../Parsing/ExpressionParser';
import { sourceLocation } from '../Diagnostics/SourceLocation';
import { TypeRefSyntax } from '../Syntax/Declarations';

const prefix = 'module M\n  feature F\n    slice Automation S\n      event Approved\n      command Claim\n';
const refusalCodes = (source: string) => parse(source).diagnostics.filter(diagnostic => /^PLAY054[0-4]$/.test(diagnostic.code));
const reaction = (branches: string) => `      reaction Claimer\n        when Approved\n          invokes Claim\n${branches}`;
const branch = (selector: string) => `            on refused${selector ? ` ${selector}` : ''}\n              acknowledge\n`;

const values = new RefusalDeclarations(parse('concept State : Enum\n  draft\n  sent\ntype Details\n  state State\n  count Int').value);
const expression = (text: string) => parseMappingSource(text, sourceLocation(1, 1));
const type = (name: string, isCollection = false): TypeRefSyntax => ({ kind: 'TypeRefSyntax', name, isCollection, isOptional: false, location: sourceLocation(1, 1) });

describe('when validating reaction refusal values and redelivery locators', () => {
    it.each([
        ['by validation', 'by validation', true], ['by validation', '', false],
        ['by authorization', 'by authorization', true], ['by authorization', 'by validation', false],
        ['by constraint', 'by constraint', true], ['', 'by authorization', false],
    ])('should compare ordered selectors %s and %s', (first, second, shadowed) => {
        expect(refusalCodes(prefix + reaction(branch(first) + branch(second))).some(diagnostic => diagnostic.code === 'PLAY0540')).toBe(shadowed);
    });

    it.each(['unknown', 'constraint', ''])('should reject the refusal member %s outside its permitted scope', member => {
        const source = prefix + '      event Refused\n        reason String\n' + reaction(`            on refused\n              produces Refused\n                reason = $refusal${member ? `.${member}` : ''}`);
        expect(refusalCodes(source)).toMatchObject([{ code: 'PLAY0541', severity: 'error' }]);
    });

    it.each(['String', 'Text', 'Unknown', 'Int', 'String[]'])('should validate nominal mapping type %s', target => {
        const source = 'concept Text : String\n' + prefix + `      event Refused\n        reason ${target}\n` + reaction('            on refused\n              produces Refused\n                reason = $refusal.message');
        expect(refusalCodes(source).some(diagnostic => diagnostic.code === 'PLAY0541')).toBe(['Int', 'String[]'].includes(target));
    });

    it('should follow a composite property path', () => {
        const source = 'type Details\n  count Int\n' + prefix + '      event Refused\n        details Details\n' + reaction('            on refused\n              produces Refused\n                details.count = $refusal.reason');
        expect(refusalCodes(source)).toMatchObject([{ code: 'PLAY0541', message: "'$refusal.reason' is a String value, incompatible with 'Int'." }]);
    });

    it('should compare qualified and imported constraint aliases', () => {
        const source = 'import M.F.S.One\n' + prefix + '      constraint One\n        unique event Approved\n' + reaction(branch('by constraint One') + branch('by constraint S.One'));
        expect(refusalCodes(source).filter(diagnostic => diagnostic.code === 'PLAY0540')).toHaveLength(3);
    });

    it('should leave handler and file-constraint event targets undecided', () => {
        const source = 'module M\n  feature F\n    slice Automation S\n      event Approved\n      command Claim\n        handler\n          file Claim.cs\n      constraint One\n        unique event Approved\n      constraint External\n        file External.cs\n' + reaction(branch('by constraint One') + branch('by constraint External'));
        expect(refusalCodes(source)).toEqual([]);
    });

    it.each([
        ['"State.sent"', 'State.sent', 'State', false, true],
        ['1', '1', 'Int', false, true], ['1', '2', 'Int', false, false],
        ['null', 'null', 'String', false, true],
        ['[1, 2]', '[1, 2]', 'Int', true, true], ['[1, 2]', '[2, 1]', 'Int', true, false],
        ['[unknown]', '[1]', 'Int', true, null], ['[1]', '[1]', 'Unknown', true, null],
        ['{"state":"sent","count":1}', '{"count":1,"state":"State.sent"}', 'Details', false, true],
        ['{"unknown":1}', '{"unknown":1}', 'Details', false, null],
        ['{"count":unknown}', '{"count":1}', 'Details', false, null],
        ['some.path', '"a"', 'String', false, null],
    ])('should compare %s with %s using declared %s', (left, right, name, collection, expected) => {
        expect(sameRedeliveryValue(expression(left), expression(right), type(name, collection), values)).toBe(expected);
    });

    it.each([
        ['', 0],
        ['        given Approved\n          for "other"', 0],
        ['        given Approved\n          for "one"', 1],
        ['        given Approved\n          for "one"\n        given Approved\n          for "one"', 2],
    ])('should count concrete given locators', (given, count) => {
        const source = prefix + '      reaction Claimer\n        when Approved\n      specification Recovery\n' + given + '\n        when redelivered Approved to Claimer\n          for "one"\n        then no events';
        const diagnostics = refusalCodes(source);
        expect(diagnostics.length).toBe(count === 1 ? 0 : 1);
        if (count !== 1) expect(diagnostics[0].message).toContain(`matches ${count} given occurrences`);
    });

    it('should expand a typed given example before locating redelivery', () => {
        const source = prefix + '      example Prior : Approved\n        for "one"\n      reaction Claimer\n        when Approved\n      specification Recovery\n        given Prior\n        when redelivered Approved to Claimer\n          for "one"\n        then no events';
        expect(refusalCodes(source)).toEqual([]);
    });
});
