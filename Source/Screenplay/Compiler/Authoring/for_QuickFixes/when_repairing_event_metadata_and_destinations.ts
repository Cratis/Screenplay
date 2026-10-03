// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import vectors from '../../Conformance/production-quick-fixes.json';
import * as compiler from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes, prepareQuickFixes } from '../QuickFixes';

const prefix = 'module M\n  feature F\n    slice StateChange S\n';
const source = prefix + '      command Register\n        projectId Uuid identifier\n        produces Registered\n          projectId = projectId\n      event Registered\n        projectId Uuid';

describe('when repairing event metadata and destinations', () => {
    it.each(vectors.cases)('should honor shared eligibility: $name', vector => {
        const source = vector.source.join('\n');
        const fixes = prepareQuickFixes(source);
        const offered = vector.source.flatMap((_text, index) => fixes(index + 1, 'PLAY0478'));
        expect(offered.map(fix => fix.line)).toEqual(vector.eligibleLines);
        for (const fix of offered) {
            expect(fix.scope).toBe('occurrence');
            expect(fix.title).toBe('State the destination: for projectId');
            const candidate = applyQuickFixEdits(source, fix.edits)!;
            expect(compiler.parse(candidate).success).toBe(true);
            expect(findQuickFixes(candidate, { line: fix.line, diagnosticCode: 'PLAY0478' })).toEqual([]);
        }
    });

    it.each(['\n', '\r\n'])('should remove a whole redundant id line with %j endings', ending => {
        const source = (prefix + '      event Registered\n        id "Registered"\n        name String\n').replaceAll('\n', ending);
        const fix = findQuickFixes(source, { line: 5, diagnosticCode: 'PLAY0471' });
        expect(fix).toHaveLength(1);
        expect(applyQuickFixEdits(source, fix[0].edits)).toBe(source.replace(`        id "Registered"${ending}`, ''));
        expect(compiler.parse(applyQuickFixEdits(source, fix[0].edits)!).diagnostics).toEqual([]);
    });

    it('should remove a final id without a newline and preserve adjacent comments', () => {
        const source = prefix + '      event Registered\n        // keep this\n        id "Registered"';
        const fix = findQuickFixes(source, { line: 6 })[0];
        expect(applyQuickFixEdits(source, fix.edits)).toBe(source.replace('        id "Registered"', ''));
    });

    it('should keep trailing comments by refusing the removal', () => {
        expect(findQuickFixes(prefix + '      event Registered\n        id "Registered" // keep this', { line: 5 })).toEqual([]);
    });

    it('should not remove a meaningful event pin', () => {
        expect(findQuickFixes(prefix + '      event Registered\n        id "OldName"', { line: 5 })).toEqual([]);
    });

    it('should remove an inline id and leave the next redundant id available after line shifting', () => {
        const source = prefix + '      command Register\n        projectId Uuid identifier\n        produces event Registered\n          id "Registered"\n        produces event Audited\n          id "Audited"';
        const fix = findQuickFixes(source, { line: 7 })[0];
        const candidate = applyQuickFixEdits(source, fix.edits)!;
        expect(candidate).toBe(source.replace('          id "Registered"\n', ''));
        expect(findQuickFixes(candidate, { line: 8 })).toHaveLength(1);
    });

    it.each(['\n', '\r\n'])('should put the destination before mappings and preserve header comments with %j', ending => {
        const text = source.replace('produces Registered', 'produces Registered // keep').replaceAll('\n', ending);
        const fix = findQuickFixes(text, { line: 6, diagnosticCode: 'PLAY0478' })[0];
        expect(applyQuickFixEdits(text, fix.edits)).toBe(text.replace(`produces Registered // keep${ending}`, `produces Registered // keep${ending}          for projectId${ending}`));
    });

    it('should insert at EOF without moving the diagnostic anchor to the edit line', () => {
        const source = prefix + '      event Registered\n        projectId Uuid\n      command Register\n        projectId Uuid identifier\n        produces Registered';
        const fix = findQuickFixes(source, { line: 8 })[0];
        expect(fix.line).toBe(8);
        expect(applyQuickFixEdits(source, fix.edits)).toBe(source + '\n          for projectId');
    });

    it('should respect placement for a local imported slice without inferring external contracts', () => {
        const source = 'slice StateChange S\n  command Register\n    projectId Uuid identifier\n    produces Registered\n  event Registered\n    projectId Uuid';
        expect(findQuickFixes(source, { line: 4, placement: ['M', 'F'] })).toHaveLength(1);
        expect(findQuickFixes(source.replace('  event Registered\n    projectId Uuid', ''), { line: 4, placement: ['M', 'F'] })).toEqual([]);
    });

    it('should verify only the requested occurrence and cache it rather than reparse every production', () => {
        const source = prefix + Array.from({ length: 2000 }, (_value, index) => `      command C${index}\n        id Uuid identifier\n        produces E${index}\n      event E${index}\n        id Uuid\n`).join('');
        const spy = vi.spyOn(compiler, 'parseForAuthoring');
        try {
            const fixes = prepareQuickFixes(source);
            expect(spy).toHaveBeenCalledTimes(1);
            expect(fixes(6, 'PLAY0478')).toHaveLength(1);
            expect(fixes(6, 'PLAY0478')).toHaveLength(1);
            expect(spy).toHaveBeenCalledTimes(2);
        } finally {
            spy.mockRestore();
        }
    });

    it('should index a wide command without rescanning its properties for every production', () => {
        const source = prefix + '      command C\n' + Array.from({ length: 2000 }, (_value, index) => `        p${index} String\n`).join('') +
            '        projectId Uuid identifier\n' + Array.from({ length: 2000 }, (_value, index) => `        produces E${index}\n`).join('');
        const parsed = compiler.parseForAuthoring(source);
        const properties = parsed.value.modules[0].features[0].slices[0].commands[0].properties;
        let visits = 0;
        for (const property of properties) {
            const identifier = property.isIdentifier;
            Object.defineProperty(property, 'isIdentifier', { enumerable: true, get: () => { visits++; return identifier; } });
        }
        const spy = vi.spyOn(compiler, 'parseForAuthoring').mockReturnValue(parsed);
        try {
            prepareQuickFixes(source);
            expect(visits).toBeLessThan(properties.length * 8);
        } finally {
            spy.mockRestore();
        }
    });

    it('should refuse unrelated syntax drift during verification', () => {
        const parse = compiler.parseForAuthoring;
        const spy = vi.spyOn(compiler, 'parseForAuthoring').mockImplementation((text, path, placement) => {
            const parsed = parse(text, path, placement);
            return text.includes('for projectId') ? { ...parsed, value: { ...parsed.value, modules: [] } } : parsed;
        });
        try {
            expect(findQuickFixes(source, { line: 6 })).toEqual([]);
        } finally {
            spy.mockRestore();
        }
    });
});
