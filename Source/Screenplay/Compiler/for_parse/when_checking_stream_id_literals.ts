// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const prefix = 'concept Key : Int\nconcept Id : Uuid\neventsource A\n  identifier Id\n  stream S\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id identifier\n';
const route = (literal: string, type = 'String') => prefix.replace('streamId String', `streamId ${type}`) + `        stream A.S\n          streamId = ${literal}`;
const specification = (literal: string, step = 'given', type = 'String') => prefix.replace('streamId String', `streamId ${type}`) + `      event Recorded\n      specification Example\n        ${step} Recorded\n          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"\n          stream A.S\n            streamId = ${literal}`;

describe('when checking stream id literals with the shared formatter', () => {
    it.each(['""', '"e\u0301"', '"\uD800"', '"\uDC00"', '9007199254740992', '-9007199254740992'])('should refuse command literal %s as an error', literal => {
        const result = parse(route(literal, literal.startsWith('"') ? 'String' : 'Key'));
        expect(result.diagnostics.some(d => d.code === 'PLAY0504' && d.severity === 'error')).toBe(true);
        expect(result.success).toBe(false);
    });
    it.each(['given', 'then'])('should refuse malformed literals on %s specification routes', step => {
        for (const literal of ['"e\u0301"', '"\uD800"', '"\uDC00"', '9007199254740992', '-9007199254740992']) {
            expect(parse(specification(literal, step, literal.startsWith('"') ? 'String' : 'Key')).diagnostics.some(d => d.code === 'PLAY0549' && d.severity === 'error')).toBe(true);
        }
    });
    it.each(['9007199254740990', '9007199254740991', '-9007199254740990', '-9007199254740991'])('should preserve adjacent accepted integer %s', value => expect(parse(route(value, 'Key')).success).toBe(true));
    it('should keep exact-mode integers unbounded and NFC and whitespace text unchanged', () => {
        expect(parse('numbers exact\n' + route('9007199254740993', 'Key')).success).toBe(true);
        expect(parse('numbers exact\n' + specification('9007199254740993', 'given', 'Key')).success).toBe(true);
        for (const value of ['"\u00e9"', '" "']) expect(parse(route(value)).success).toBe(true);
    });
    it.each([
        ['id Uuid identifier', '', 1],
        ['id Id identifier\n        other String', '\n        produces event Recorded\n          for other', 1],
        ['id Id identifier', '\n        produces Recorded\n      event Recorded', 0],
        ['id Id generated identifier', '\n        produces Recorded\n      event Recorded', 0],
        ['id Id identifier', '\n        produces event Recorded', 0],
        ['id Id identifier', '\n        produces event Recorded\n          for id', 0],
    ])('should check routed destination types without duplicate errors %s', (properties, production, count) => {
        const result = parse(route('"key"').replace('id Id identifier', properties) + production);
        expect(result.diagnostics.filter(d => d.code === 'PLAY0504' && d.severity === 'error')).toHaveLength(count);
    });
    it('should refuse allocation without a generated identifier for a non-UUID source', () => {
        const result = parse(route('"key"').replace('concept Id : Uuid', 'concept Id : String') + '\n        produces Recorded\n      event Recorded');
        expect(result.diagnostics.some(d => d.code === 'PLAY0504' && d.severity === 'error')).toBe(true);
    });
    it('should leave unrouted and imported destination types unresolved', () => {
        expect(parse(prefix.replace('id Id identifier', 'id Uuid identifier')).diagnostics.some(d => d.code === 'PLAY0504')).toBe(false);
        expect(parse('import Contracts.Unknown\n' + route('"key"').replace('id Id identifier', 'id Unknown identifier') + '\n        produces event Recorded').diagnostics.some(d => d.code === 'PLAY0504')).toBe(false);
    });
});
