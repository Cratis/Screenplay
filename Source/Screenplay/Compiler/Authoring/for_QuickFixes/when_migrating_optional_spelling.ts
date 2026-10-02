// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { applyQuickFixEdits, findQuickFixes, prepareQuickFixes } from '../QuickFixes';

const source = '// café?\r\ntype T\r\n  note    String? // keep?\r\n  lines   String[]?';

describe('when migrating optional spelling', () => {
    it('should offer only verified edits and never change the source while discovering', () => {
        const fixes = findQuickFixes(source, { line: 3 });
        expect(fixes.map(fix => fix.scope)).toEqual(['occurrence', 'document']);
        expect(applyQuickFixEdits(source, fixes[0].edits)).toBe(source.replace('String?', 'String optional'));
        const candidate = applyQuickFixEdits(source, fixes[1].edits)!;
        expect(candidate).toBe(source.replace('String?', 'String optional').replace('String[]?', 'String[] optional'));
        expect(toSyntaxJson(compiler.parse(candidate).value)).toEqual(toSyntaxJson(compiler.parse(source).value));
        expect(compiler.parse(candidate).diagnostics).toEqual([]);
        expect(findQuickFixes(candidate)).toEqual([]);
    });

    it('should support a dirty imported slice in its actual placement', () => {
        const fragment = 'slice StateView Notes\n  readmodel Note\n    text String?\n  query Q => Note?';
        const fixes = findQuickFixes(fragment, { line: 3, placement: ['M', 'F'] });
        expect(fixes).toHaveLength(2);
        expect(fixes[1].edits).toHaveLength(2);
        expect(compiler.parse(applyQuickFixEdits(fragment, fixes[1].edits)!, undefined, ['M', 'F']).diagnostics).toEqual([]);
    });

    it('should not patch strings imports attachments or fenced code', () => {
        expect(findQuickFixes('import "file?.play"\ntype T\n  file Types/T?.cs\n  description "String?"\n  note String\n')).toEqual([]);
    });

    it('should refuse parser-invalid documents instead of proving equality of partial trees', () => {
        expect(findQuickFixes('type T\n  note String?\n  malformed String optional optional')).toEqual([]);
    });

    it('should verify a document-wide migration with a constant number of parses', () => {
        const parse = vi.spyOn(compiler, 'parseForAuthoring');
        try {
            const document = `type T\n${Array.from({ length: 4000 }, (_, index) => `  p${index} String?`).join('\n')}`;
            const fixes = findQuickFixes(document, { line: 2 });
            expect(fixes[1].edits).toHaveLength(4000);
            expect(parse).toHaveBeenCalledTimes(3);
            expect(parse.mock.calls.map(call => call[0].length).every(length => length < document.length * 2)).toBe(true);
        } finally {
            parse.mockRestore();
        }
    });

    it('should refuse ambiguous query replacements and duplicate query keys', () => {
        const prefix = 'module M\n  feature F\n    slice StateView S\n      query Q => ';
        expect(findQuickFixes(prefix + 'observable?', { line: 4 })).toEqual([]);
        expect(findQuickFixes(prefix + 'View\n        by first Uuid?\n        by second Uuid?', { line: 5 })).toEqual([]);
    });

    it('should verify both top-level and reaction trigger data outside the syntax projection', () => {
        const source = 'trigger T\n  note String?\nmodule M\n  feature F\n    slice Automation S\n      reaction R\n        when T\n          data String[]?';
        const fix = findQuickFixes(source, { line: 8 })[1];
        expect(fix.edits).toHaveLength(2);
        const candidate = compiler.parseForAuthoring(applyQuickFixEdits(source, fix.edits)!);
        expect(candidate.triggerData.map(value => [value.name, value.type.name, value.type.isOptional, value.type.isCollection])).toEqual([
            ['note', 'String', true, false], ['data', 'String', true, true],
        ]);
    });

    it('should refuse when omitted trigger data changes even if SyntaxJson stays equal', () => {
        const realParse = compiler.parseForAuthoring;
        const spy = vi.spyOn(compiler, 'parseForAuthoring').mockImplementation((source, path, placement) => {
            const result = realParse(source, path, placement);
            return source.includes(' optional') ? { ...result, triggerData: result.triggerData.map(value => ({ ...value, type: { ...value.type, isOptional: false } })) } : result;
        });
        try {
            expect(findQuickFixes('trigger T\n  value String?', { line: 2 })).toEqual([]);
        } finally {
            spy.mockRestore();
        }
    });

    it('should analyze each buffer once and reuse its document and occurrence verdicts', () => {
        const spy = vi.spyOn(compiler, 'parseForAuthoring');
        try {
            const fixes = prepareQuickFixes(source);
            fixes(3);
            fixes(3);
            fixes(4);
            fixes(4);
            expect(spy).toHaveBeenCalledTimes(4); // original, document, two selected occurrences
        } finally {
            spy.mockRestore();
        }
    });

    it('should reject overlapping edits', () => {
        expect(applyQuickFixEdits('value', [{ start: 2, length: 2, text: '' }, { start: 1, length: 1, text: '' }])).toBeUndefined();
    });
});
