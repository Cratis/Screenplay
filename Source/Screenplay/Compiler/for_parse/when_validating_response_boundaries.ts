// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';

const prefix = 'concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n';
const codes = (source: string) => parse(source).diagnostics.map(diagnostic => diagnostic.code);

describe('when validating response boundaries', () => {
    it.each([
        ['Date', '2024-02-29', true], ['Date', '1900-02-29', false],
        ['Date', '2000-02-29', true], ['Date', '2023/02/29', false],
        ['Date', '2024-01-02T00:00', false], ['Date', 'Jan 2 2024', false],
        ['DateTime', '2024-02-29T12:00:00Z', true],
        ['DateTime', '2024-02-29T12:00:00.1234567-14:00', true],
        ['DateTime', '2024-02-29T12:00:00.12345678Z', false],
        ['DateTime', '2023-02-29t12:00:00Z', false],
        ['DateTime', '2023-02-29T12:00:00Z', false],
        ['DateTime', '2023-01-01T12:00:00+1500', false],
        ['DateTime', '2024-01-01T12:00:00+14:01', false],
        ['DateTime', '2024-01-01T12:00:00+01:60', false],
        ['DateTime', '2024-01-01T24:00:00Z', false],
        ['DateTime', '2024-01-01T12:00:00', false], ['DateTime', '10:00', false],
    ])('should check %s response calendar and offset spelling for %s', (type, value, valid) => {
        expect(codes(prefix + `      command C\n        value ${type}\n        returns value\n      specification Accepts\n        when C\n        then returns "${value}"`).includes('PLAY0491')).toBe(!valid);
    });

    it.each([
        ['"abcdef0123456789abcdef0123456789"', true],
        ['"ABCDEF01-2345-6789-ABCD-EF0123456789"', true],
        ['"{abcdef01-2345-6789-abcd-ef0123456789}"', true],
        ['"(abcdef01-2345-6789-abcd-ef0123456789)"', true],
        ['42', false], ['"not-a-uuid"', false], ['id', false], ['id + other', false],
        ['"11111111-1111-1111-1111-111111111111" "22222222-2222-2222-2222-222222222222"', false],
        ['" abcdef01-2345-6789-abcd-ef0123456789"', false],
        ['"{0xabcdef01,0x2345,0x6789,{0xab,0xcd,0xef,0x01,0x23,0x45,0x67,0x89}}"', false],
    ])('should validate generated identifier for %s as one concrete UUID', (value, valid) => {
        const diagnostics = parse(prefix + `      command C\n        id Id generated identifier\n      specification Fixture\n        when C\n          for ${value}`, 'fixture.play').diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0490');
        expect(diagnostics).toHaveLength(valid ? 0 : 1);
        if (!valid) expect(diagnostics[0].location).toEqual({ path: 'fixture.play', line: 9, column: 11 });
    });
    it.each(['id Id identifier', 'id Id', 'id External generated identifier'])('should leave legacy and unresolved for values unchanged for %s', declaration => {
        expect(codes('import Outside.External\n' + prefix + `      command C\n        ${declaration}\n      specification Fixture\n        when C\n          for id + other`)).not.toContain('PLAY0490');
    });
    it('should defer missing generated identifier completeness', () => {
        expect(codes(prefix + '      command C\n        id Id generated identifier\n      specification Fixture\n        when C')).not.toContain('PLAY0490');
    });
    it('should not tighten general literal parsing', () => {
        expect(parse(prefix + '      command C\n        value Date\n      specification Accepts\n        when C\n          value = "Jan 2 2024"').success).toBe(true);
    });

    it.each(['', 'import External.Enum\n'])('should keep unavailable Enum shapes unknown with %s', imported => {
        expect(codes(imported + prefix + '      command C\n        value Enum\n        returns value\n      specification Accepts\n        when C\n        then returns "text"')).not.toContain('PLAY0491');
    });

    it('should keep imported Enum fixture shapes unknown', () => {
        expect(codes('import External.Enum\n' + prefix + '      command C\n        receipt Enum generated\n      specification Accepts\n        when C\n          generated receipt = "text"')).not.toContain('PLAY0490');
    });

    it.each(['refresh Q', 'navigate to Entry', 'open dialog Dialog', 'close dialog'])('should not attribute %s arguments to an enclosing execute', action => {
        const parsed = parseForAuthoring(prefix + `      command C\n        receipt Id generated\n      screen Entry\n        on submit\n          execute C\n            on success\n              ${action}\n                with receipt from $form.receipt`);
        expect(parsed.inputUses).toHaveLength(0);
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).not.toContain('PLAY0485');
    });

    it('should still collect actual command arguments in dialog continuations', () => {
        expect(codes(prefix + '      command C\n        receipt Id generated\n      screen Entry\n        on submit\n          open dialog Dialog\n            on result\n              execute C\n                with receipt from $result.receipt')).toContain('PLAY0485');
    });

    it.each([
        'parameter C\n  on submit\n    execute C\n      with receipt from $form.receipt',
        'on submit\n    execute C\n      with receipt from $form.receipt\n  parameter C',
    ])('should respect behavior parameter shadowing in any declaration order', body => {
        const parsed = parseForAuthoring('behavior B\n  ' + body + '\n' + prefix + '      command C\n        receipt Id generated\n      command D\n        receipt Id\n      screen Entry\n        uses B\n          C D');
        expect(parsed.inputUses).toHaveLength(1);
        expect(parsed.inputUses[0].isParameter).toBe(true);
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).not.toContain('PLAY0485');
    });

    it('should check escaped invocation mapping targets', () => {
        expect(codes(prefix + '      command C\n        receipt Id generated\n      reaction R\n        when Recorded\n          invokes C\n            @receipt = "ignored"').filter(code => code === 'PLAY0485')).toHaveLength(1);
    });

    it('should check table behavior inputs', () => {
        expect(codes(prefix + '      command C\n        receipt Id generated\n      screen Entry\n        table items\n          column name\n          on select\n            execute C\n              with receipt from $row.receipt').filter(code => code === 'PLAY0485')).toHaveLength(1);
    });

    it.each([
        ['returns Foo identifier optional', 'PLAY0480'],
        ['returns Id generated optional', 'PLAY0484'],
    ])('should retain modifier diagnostics for %s', (declaration, code) => {
        const diagnostics = codes(prefix + '      command C\n        ' + declaration);
        expect(diagnostics).toContain(code);
        expect(diagnostics).not.toContain('PLAY0486');
    });

    it('should keep tab-separated returns as the preexisting declaration', () => {
        const parsed = parse('concept id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id id identifier\n        returns\tid');
        expect(parsed.success).toBe(true);
        const command = parsed.value.modules[0].features[0].slices[0].commands[0];
        expect(command.response).toBeNull();
        expect(command.properties[1].name).toBe('returns');
        expect(command.properties[1].type.name).toBe('id');
    });
});
