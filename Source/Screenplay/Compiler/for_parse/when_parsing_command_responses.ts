// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parseFolder } from '../Files/PlayApplicationAssembly';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const prefix = 'concept Id : Uuid\nconcept lowerCaseConcept : String\nconcept slug : String\nmodule M\n  feature F\n    slice StateChange S\n';
const commandOf = (source: string) => parse(prefix + '      command C\n        ' + source).value.modules[0].features[0].slices[0].commands[0];
const codes = (body: string) => parse(prefix + body).diagnostics.map(diagnostic => diagnostic.code);

describe('when parsing command responses', () => {
    it.each([
        ['returns String', false],
        ['returns lowerCaseConcept', false],
        ['@returns String', false],
        ['returns slug\n        slug slug', true],
        ['slug slug\n        returns slug', true],
        ['@returns String\n        returns returns', true],
        ['returns @slug\n        slug slug', true],
    ])('should disambiguate %s using only same-command properties', (body, response) => {
        expect(commandOf(body).response !== null).toBe(response);
    });

    it.each([
        ['returns String\n          value Int', ['returns', 'value']],
        ['returns lowerCaseConcept\n          value Int', ['returns', 'value']],
        ['@returns String\n          value Int', ['returns', 'value']],
        ['returns String\n          returns Int\n            value Int', ['returns', 'returns', 'value']],
        ['value Int\n        returns String\n          other String', ['value', 'returns', 'other']],
    ])('should preserve ordinary deeper command members after property %s', (body, names) => {
        const result = parse(prefix + '      command C\n        ' + body);
        expect(result.success).toBe(true);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.response).toBeNull();
        expect(command.properties.map(property => property.name)).toEqual(names);
    });

    it.each([
        'returns slug\n          extra Int\n        slug slug',
        'slug slug\n        returns slug\n          extra Int',
        'returns @slug\n          extra Int\n        slug slug',
    ])('should restrict nested members only after scalar resolution: %s', body => {
        const result = parse(prefix + '      command C\n        ' + body);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0486')).toHaveLength(1);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.response?.kind).toBe('ScalarCommandResponseSyntax');
        expect(command.properties.map(property => property.name)).toEqual(['slug']);
    });

    it('should keep generated contextual', () => expect(commandOf('generated String').properties[0].isGenerated).toBe(false));
    it('should preserve a one-field record', () => expect(commandOf('name String\n        returns\n          value = name').response?.kind).toBe('RecordCommandResponseSyntax'));
    it.each(['generated generated', 'identifier generated', 'generated optional', 'generated identifier generated', 'optional optional generated', 'optional generated optional', 'identifier identifier generated'])('should reject %s', modifiers => {
        expect(codes(`      command C\n        id Id ${modifiers}`)).toContain('PLAY0484');
    });
    it.each(['"one" "two"', 'id', '{"id": 1} trailing', '[1] [2]'])('should reject partially consumed or nonconcrete %s', value => {
        expect(parse(prefix + '      specification Invalid\n        when C\n        then returns ' + value).success).toBe(false);
    });
    it.each(['Uuid generated', 'String generated', 'Id optional generated', 'Id[] generated'])('should reject invalid generated type %s', type => {
        expect(codes(`      command C\n        id ${type}`)).toContain('PLAY0483');
    });
    it('should reject generated properties outside commands', () => {
        expect(parse('concept Id : Uuid\ntype T\n  id Id generated').diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0482');
    });
    it.each([
        '      event Recorded\n        id Id generated',
        '      readmodel View\n        id Id generated',
        '      command C\n        id Id\n        produces event Recorded\n          receipt Id generated = id',
    ])('should inspect typed declarations outside commands: %s', body => {
        expect(codes(body)).toContain('PLAY0482');
    });
    it.each([
        ['returns', 'PLAY0486'],
        ['id Id\n        returns id\n        returns id', 'PLAY0486'],
        ['id Id\n        returns @unknown', 'PLAY0487'],
        ['id Id\n        returns\n          value = id\n          value = id', 'PLAY0488'],
        ['id Id\n        returns\n          value Uuid = id', 'PLAY0489'],
        ['ids Id[]\n        returns ids', 'PLAY0489'],
    ])('should diagnose invalid contract %s', (body, code) => expect(codes('      command C\n        ' + body)).toContain(code));

    it('should keep generated fixtures separate from request inputs and walk all new nodes', () => {
        const parsed = parse(prefix + '      command C\n        receipt Id generated\n        returns\n          value = receipt\n      specification Accepts\n        when C\n          generated receipt = "22222222-2222-2222-2222-222222222222"\n          generated = "input"\n        then returns\n          value = "22222222-2222-2222-2222-222222222222"');
        expect(parsed.success).toBe(true);
        const specification = parsed.value.modules[0].features[0].slices[0].specifications[0];
        expect(specification.when?.generatedValues).toHaveLength(1);
        expect(specification.when?.values[0].property).toBe('generated');
        const kinds: string[] = [];
        class Walker extends ScreenplaySyntaxWalker { override visitNode(node: SyntaxNode): void { kinds.push(node.kind); } }
        new Walker().visitApplication(parsed.value);
        expect(kinds).toContain('RecordCommandResponseSyntax');
        expect(kinds).toContain('ResponseFieldSyntax');
        expect(kinds).toContain('PropertyResponseSourceSyntax');
        expect(kinds).toContain('RecordSpecificationReturnSyntax');
        expect(toSyntaxJson(specification)).toHaveProperty('thenReturns');
    });

    it.each([
        ['name String\n        returns name', 'then returns "accepted"', ['ScalarCommandResponseSyntax', 'PropertyResponseSourceSyntax', 'ScalarSpecificationReturnSyntax', 'LiteralExpressionSyntax']],
        ['name String\n        returns\n          value String = name', 'then returns\n          value = "accepted"', ['RecordCommandResponseSyntax', 'ResponseFieldSyntax', 'TypeRefSyntax', 'RecordSpecificationReturnSyntax']],
        ['name String', 'then denied', ['SpecificationDeniedSyntax']],
    ])('should walk independently authored response and denial nodes', (command, outcome, expected) => {
        const parsed = parse(prefix + `      command C\n        ${command}\n      specification Accepts\n        when C\n        ${outcome}`);
        expect(parsed.success).toBe(true);
        const kinds: string[] = [];
        class Walker extends ScreenplaySyntaxWalker { override visitNode(node: SyntaxNode): void { kinds.push(node.kind); } }
        new Walker().visitApplication(parsed.value);
        expect(kinds).toEqual(expect.arrayContaining(expected));
    });

    it.each(['then error', 'then denied'])('should reject returns with %s', outcome => {
        expect(codes('      command C\n        id Id\n        returns id\n      specification Rejects\n        when C\n        then returns "11111111-1111-1111-1111-111111111111"\n        ' + outcome)).toContain('PLAY0491');
    });
    it.each(['unknown', 'id', 'receipt'])('should reject invalid fixture target or value %s', target => {
        expect(codes(`      command C\n        id Id generated identifier\n        receipt Id generated\n      specification Invalid\n        when C\n          generated ${target} = "not a uuid"`)).toContain('PLAY0490');
    });
    it('should reject request inputs for generated values', () => {
        expect(codes('      command C\n        receipt Id generated\n      specification Invalid\n        when C\n          receipt = "22222222-2222-2222-2222-222222222222"')).toContain('PLAY0485');
    });
    it('should collect typed form and execution input uses without reading code fences', () => {
        const source = prefix + '      command C\n        receipt Id generated\n      screen Entry\n        on submit\n          execute C\n            with receipt from $form.receipt\n  form Entry for C\n    field receipt\n';
        const parsed = parseForAuthoring(source);
        expect(parsed.inputUses).toHaveLength(2);
        expect(parsed.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0485')).toHaveLength(2);
    });
    it.each([
        ['returns when accepted', 'PLAY0486'],
        ['returns @id\n          extra String\n        id Id', 'PLAY0486'],
        ['returns id\n          extra String\n        id Id', 'PLAY0486'],
        ['returns\n          malformed\n            nested String', 'PLAY0486'],
        ['id Id\n        returns\n          value = id\n            nested String', 'PLAY0486'],
        ['id Id\n        returns\n          value Id? = id', 'PLAY0489'],
    ])('should reject malformed response %s', (body, code) => {
        expect(codes('      command C\n        ' + body)).toContain(code);
    });

    it('should keep escaped declarations independent of scalar source recognition', () => {
        expect(commandOf('name String\n        returns name\n        @returns Id generated').response?.kind).toBe('ScalarCommandResponseSyntax');
    });

    it.each([
        ['String', '"text"', true], ['String', '42', false],
        ['Bool', 'true', true], ['Bool', '"true"', false],
        ['Int', '42', true], ['Int', '1.5', false],
        ['Decimal', '1.5', true], ['Decimal', '"1.5"', false],
        ['Date', '"2026-10-02"', true], ['Date', '"invalid"', false],
        ['DateTime', '"2026-10-02T12:00:00Z"', true], ['DateTime', '"invalid"', false],
        ['Date', '"2024-02-29"', true], ['Date', '"2023-02-29"', false],
        ['Date', '"0000-01-01"', false], ['Date', '"2026-13-01"', false],
        ['Date', '"2026-00-01"', false], ['Date', '"2026-01-00"', false],
        ['DateTime', '"2026-10-02T12:00:00+14:00"', true],
        ['DateTime', '"2026-10-02T12:00:00+14:01"', false],
        ['DateTime', '"2026-10-02T12:00:00+15:00"', false],
        ['DateTime', '"2026-10-02T12:00:00+01:60"', false],
        ['Date', '"2026-10-02T12:00:00Z"', false],
        ['Id', '"11111111-1111-1111-1111-111111111111"', true], ['Id', '"invalid"', false],
        ['Id', '"11111111111111111111111111111111"', true],
        ['Id', '"{11111111-1111-1111-1111-111111111111}"', true],
        ['Id', '"(11111111-1111-1111-1111-111111111111)"', true],
        ['Id', '"{0x11111111,0x1111,0x1111,{0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11}}"', false],
        ['String optional', 'null', true], ['String', 'null', false],
        ['Unknown', '"opaque"', true], ['Unknown', '{"opaque": true}', true],
        ['String', '{"opaque": true}', false], ['slug', '{"opaque": true}', false],
        ['Status', '"accepted"', true], ['Status', '"other"', false],
        ['Payload', '{"names": ["one", "two"], "note": null}', true],
        ['Payload', '{"names": [false]}', false], ['Payload', '{"names": "one"}', false],
        ['Payload', '{"unknown": true}', false], ['Payload', '"not an object"', false],
        ['Payload', '[1]', false], ['Payload', '{"note": 42}', false],
    ])('should check concrete %s return value %s', (type, value, valid) => {
        const declarations = 'concept Status : Enum\n  accepted\ntype Payload\n  names String[]\n  note String optional\n';
        const parsed = parse(declarations + prefix + `      command C\n        result ${type}\n        returns result\n      specification Accepts\n        when C\n        then returns ${value}`);
        expect(parsed.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0491')).toBe(!valid);
    });

    it.each([
        ['generated receipt', 'PLAY0490'], ['generated receipt = id', 'PLAY0490'],
        ['generated receipt = "one" "two"', 'PLAY0490'],
        ['generated receipt = "11111111-1111-1111-1111-111111111111"\n            nested = 1', 'PLAY0490'],
        ['generated receipt = "11111111-1111-1111-1111-111111111111"\n          generated receipt = "11111111-1111-1111-1111-111111111111"', 'PLAY0490'],
    ])('should reject malformed generated fixture %s', (fixture, code) => {
        expect(codes('      command C\n        receipt Id generated\n      specification Invalid\n        when C\n          ' + fixture)).toContain(code);
    });

    it.each([
        ['then returns', true], ['then returns\n          unknown = "text"', true],
        ['then returns\n          result = false', true],
        ['then returns\n          result = "text"\n          result = "text"', true],
        ['then returns\n          result = "text"\n            nested = 1', true],
        ['then returns\n          malformed', true],
        ['then returns\n          result = "text"', false],
        ['then returns "text"', true],
    ])('should check record return assertions %s', (expectation, invalid) => {
        expect(codes('      command C\n        name String\n        returns\n          result = name\n      specification Accepts\n        when C\n        ' + expectation).includes('PLAY0491')).toBe(invalid);
    });

    it('should reject returns without command actions or response contracts', () => {
        expect(codes('      specification Invalid\n        then returns "text"')).toContain('PLAY0491');
        expect(codes('      command C\n        name String\n      specification Invalid\n        when C\n        then returns "text"')).toContain('PLAY0491');
        expect(codes('      command C\n        name String\n        returns name\n      specification Invalid\n        when C\n        then returns "text"\n        then returns "text"')).toContain('PLAY0491');
    });

    it('should not confuse generated composite declarations with Uuid concepts', () => {
        expect(parse('type Payload\n  id Uuid\n' + prefix + '      command C\n        value Payload generated').diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0483');
    });

    it('should resolve qualified, imported and reused input references without scanning fences', () => {
        const source = prefix + '      command C\n        receipt Id generated\n      screen Entry\n        on submit\n          execute M.F.S.C\n            with receipt from $form.receipt\n          execute C\n            with receipt from $form.receipt\n          execute C\n            with receipt from $form.receipt\n          ```csharp\n          execute C\n            with receipt from ignored\n          ```\n      reaction R\n        when Recorded\n          invokes C\n            receipt = "ignored"\n  form Entry for C\n    field receipt\n';
        const parsed = parseForAuthoring(source);
        expect(parsed.inputUses).toHaveLength(5);
        expect(parsed.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0485')).toHaveLength(5);
        const imported = parseFolder([
            { path: 'command.play', source: prefix + '      command C\n        receipt Id generated\n        returns receipt' },
            { path: 'form.play', source: 'import M.F.S.C\nmodule Other\n  form Entry for C\n    field receipt' },
        ]);
        expect(imported.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0485');
    });

    it('should reject ambiguous property sources and duplicate declared response fields', () => {
        expect(codes('      command C\n        name String\n        name String\n        returns @name')).toContain('PLAY0487');
        expect(codes('      command C\n        name String\n        returns\n          value = name\n          value = name\n      specification Invalid\n        when C\n        then returns\n          value = "text"')).toContain('PLAY0491');
    });

    it('should preserve per-command interpretation in merged files', () => {
        const result = parseFolder([
            { path: 'types.play', source: 'concept lowerCaseConcept : String\nconcept slug : String' },
            { path: 'command.play', source: 'module M\n  feature F\n    slice StateChange S\n      command C\n        returns lowerCaseConcept\n      command D\n        returns slug\n        slug slug' },
        ]);
        expect(result.success).toBe(true);
        const commands = result.value.modules[0].features[0].slices[0].commands;
        expect(commands[0].response).toBeNull();
        expect(commands[0].properties[0].name).toBe('returns');
        expect(commands[1].response?.kind).toBe('ScalarCommandResponseSyntax');
    });
});
