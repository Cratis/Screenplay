// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const prefix = 'module M\n  feature F\n    slice StateChange S\n';

describe('when parsing optional type references', () => {
    it.each([
        'type T\n  note String?\n',
        'trigger T\n  note String?\n',
        `${prefix}      event E\n        note String?\n`,
        `${prefix}      readmodel R\n        note String?\n`,
        `${prefix}      command C\n        description String?\n`,
        `${prefix}      command C\n        id Uuid identifier\n        produces event E\n          note String? = name\n`,
        `${prefix}      query Q => observable R[]?\n        by id Uuid?\n        filter note String? from note\n`,
        `${prefix}      reaction R\n        when E\n          note String?\n`,
    ])('should preserve structure and report each committed legacy type once in %s', source => {
        const legacy = parse(source);
        const canonical = parse(source.replaceAll('?', ' optional'));
        expect(legacy.success).toBe(true);
        expect(canonical.diagnostics).toEqual([]);
        expect(toSyntaxJson(legacy.value)).toEqual(toSyntaxJson(canonical.value));
        expect(legacy.diagnostics).toHaveLength(source.split('?').length - 1);
        for (const diagnostic of legacy.diagnostics) {
            expect(diagnostic.code).toBe('PLAY0479');
            expect(diagnostic.severity).toBe('information');
            expect(source.split('\n')[diagnostic.location.line - 1].slice(diagnostic.location.column - 1).split('?')[0]).not.toContain(' ');
        }
    });

    it.each(['String optional optional', 'String? optional', 'String optional?', 'String optional[]', 'String?[]', 'String Optional'])('should reject %s', type => {
        expect(parse(`type T\n  note ${type}`).success).toBe(false);
    });

    it('should explain reversed identifier modifiers', () => {
        expect(parse(`${prefix}      command C\n        id Uuid identifier optional\n`).diagnostics.map(value => value.code)).toContain('PLAY0480');
    });

    it.each([' optional', '?'])('should preserve event identifier rejection for %s', marker => {
        expect(parse(`${prefix}      event E\n        id Uuid${marker} identifier\n`).diagnostics.map(value => value.code)).toContain('PLAY0019');
    });

    it('should keep optional contextual', () => {
        expect(parse('type optional\n  optional String\n  value optional optional\n').diagnostics).toEqual([]);
    });

    it('should reserve optional reads', () => {
        expect(parse(`${prefix}      command C\n        reads R optional as r\n`).diagnostics.map(value => value.code)).toEqual(['PLAY0481']);
    });

    it.each([
        ['observable?', false, 'observable', true, false],
        ['observable[]?', false, 'observable', true, true],
        ['observable observable?', true, 'observable', true, false],
        ['observable optional', true, 'optional', false, false],
    ] as const)('should preserve the query interpretation of %s', (type, isObservable, name, isOptional, isCollection) => {
        const parsed = parse(`${prefix}      query Q => ${type}`);
        const query = parsed.value.modules[0].features[0].slices[0].queries[0];
        expect(query).toMatchObject({ isObservable, returnType: { name, isOptional, isCollection } });
        if (type === 'observable?') expect(parsed.diagnostics[0].message).toContain("Keep 'observable?'");
    });

    it('should keep observable greedy', () => {
        const query = parse(`${prefix}      query Q => observable optional\n`).value.modules[0].features[0].slices[0].queries[0];
        expect(query.isObservable).toBe(true);
        expect(query.returnType.name).toBe('optional');
        expect(query.returnType.isOptional).toBe(false);
    });

    it('should warn about type shaped tags without committing a type', () => {
        const diagnostics = parse(`${prefix}      event E\n        tag TagType optional\n`).diagnostics.map(value => value.code);
        expect(diagnostics).toContain('PLAY0020');
        expect(diagnostics).not.toContain('PLAY0479');
    });

    it('should distinguish file properties from dotted attachment paths', () => {
        const result = parse('type T\n  file String optional\n  file Models/T.cs\n');
        expect(result.diagnostics).toEqual([]);
        expect(result.value.types[0].properties).toHaveLength(1);
        expect(result.value.types[0].properties[0].type.isOptional).toBe(true);
    });
});
