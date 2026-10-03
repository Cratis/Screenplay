// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes, prepareQuickFixes } from '../QuickFixes';

const prefix = 'module M\n  feature F\n    slice StateChange S\n';

describe('when removing redundant event ids', () => {
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

    it('should keep redundant id removal available in placed applications', () => {
        expect(findQuickFixes('slice StateChange S\n  event E\n    id "E"', { line: 3, placement: ['M', 'F'] })).toHaveLength(1);
    });

    it('should verify only the requested occurrence and cache it rather than reparse every event', () => {
        const source = prefix + Array.from({ length: 2000 }, (_value, index) => `      event E${index}\n        id "E${index}"\n`).join('');
        const spy = vi.spyOn(compiler, 'parseForAuthoring');
        try {
            const fixes = prepareQuickFixes(source);
            expect(spy).toHaveBeenCalledTimes(1);
            expect(fixes(5, 'PLAY0471')).toHaveLength(1);
            expect(fixes(5, 'PLAY0471')).toHaveLength(1);
            expect(spy).toHaveBeenCalledTimes(2);
        } finally {
            spy.mockRestore();
        }
    });

    it('should verify all range occurrences in one linear batch and cache the verdict', () => {
        const source = prefix + Array.from({ length: 200 }, (_value, index) => `      event E${index}\n        id "E${index}"\n`).join('');
        const requests = Array.from({ length: 200 }, (_value, index) => ({ line: 5 + index * 2, diagnosticCode: 'PLAY0471' }));
        const spy = vi.spyOn(compiler, 'parseForAuthoring');
        try {
            const fixes = prepareQuickFixes(source);
            expect(fixes([...requests, ...requests])).toHaveLength(200);
            expect(fixes(requests)).toHaveLength(200);
            expect(spy).toHaveBeenCalledTimes(2);
            for (const request of requests) expect(fixes(request.line, request.diagnosticCode)).toHaveLength(1);
        } finally {
            spy.mockRestore();
        }
    });

    it('should refuse unrelated syntax drift during verification', () => {
        const source = prefix + '      event E\n        id "E"';
        const parse = compiler.parseForAuthoring;
        const spy = vi.spyOn(compiler, 'parseForAuthoring').mockImplementation((text, path, placement) => {
            const parsed = parse(text, path, placement);
            return text.includes('id "E"') ? parsed : { ...parsed, value: { ...parsed.value, modules: [] } };
        });
        try {
            expect(findQuickFixes(source, { line: 5 })).toEqual([]);
            expect(prepareQuickFixes(source)([{ line: 5, diagnosticCode: 'PLAY0471' }])).toEqual([]);
        } finally {
            spy.mockRestore();
        }
    });
});
