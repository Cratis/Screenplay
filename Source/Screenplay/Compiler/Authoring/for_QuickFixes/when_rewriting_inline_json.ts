// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes } from '../QuickFixes';

const prefix = 'module M\n  feature F\n    slice StateView S\n      screen S\n        component Cratis.Blueprint.Default.list items\n';

describe('when rewriting inline json', () => {
    it.each(['\n', '\r\n'])('should write a component property as an indented block with %j endings', ending => {
        const source = (prefix + '          property columns = [{"id":"received","count":2}, {"id":"approved","count":-1.5}] // keep\n').replaceAll('\n', ending);
        const fix = findQuickFixes(source, { line: 6, diagnosticCode: 'PLAY0833' });
        expect(fix).toHaveLength(1);
        const expected = (prefix + [
            '          property columns // keep',
            '            item',
            '              id = "received"',
            '              count = 2',
            '            item',
            '              id = "approved"',
            '              count = -1.5',
            ''].join('\n')).replaceAll('\n', ending);
        expect(applyQuickFixEdits(source, fix[0].edits)).toBe(expected);
        expect(compiler.parse(expected).diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
    });

    it('should write empty lists, nested lists and awkward member names', () => {
        const source = prefix + '          property emptyState = {"title":"No \\"items\\"","item":1,"first name":"Ada","actions":[],"grid":[[1],[]]}\n';
        const candidate = applyQuickFixEdits(source, findQuickFixes(source, { line: 6 })[0].edits);
        expect(candidate).toBe(prefix + [
            '          property emptyState',
            '            title = "No \\"items\\""',
            '            @item = 1',
            '            "first name" = "Ada"',
            '            actions none',
            '            grid',
            '              item',
            '                item 1',
            '              item none',
            ''].join('\n'));
    });

    it('should write a specification value and an absence key', () => {
        const head = 'numbers exact\nmodule M\n  feature F\n    slice StateChange X\n      specification S\n        when C\n';
        const source = head + '          lines = []\n        then no readmodel V for {"id":1}\n';
        const lines = applyQuickFixEdits(source, findQuickFixes(source, { line: 7 })[0].edits)!;
        expect(lines).toBe(head + '          lines none\n        then no readmodel V for {"id":1}\n');
        expect(applyQuickFixEdits(lines, findQuickFixes(lines, { line: 8 })[0].edits)).toBe(head + '          lines none\n        then no readmodel V for\n          id = 1\n');
    });

    it('should leave an inline step assignment to the author', () => {
        expect(findQuickFixes('module M\n  feature F\n    slice StateChange X\n      specification S\n        when C lines = [1]\n', { line: 5, diagnosticCode: 'PLAY0833' })).toEqual([]);
    });
});
