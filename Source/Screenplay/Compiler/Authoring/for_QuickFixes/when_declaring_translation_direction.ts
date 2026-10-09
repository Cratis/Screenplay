// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes, isQuickFixDiagnostic } from '../QuickFixes';

const inbound = 'import Outside.Arrived from "other"\nmodule M\n  feature F\n    slice Translate S\n      event Local\n      reaction R\n        when Arrived\n          produces Local\n';
const outbound = 'module M\n  feature F\n    slice Translate S\n      event Local\n      public event Published\n      reaction R\n        when Local\n          produces Published\n';

describe('when declaring translation direction', () => {
    it('should treat PLAY0603 as a quick-fix diagnostic', () => {
        expect(isQuickFixDiagnostic('PLAY0603')).toBe(true);
    });

    it.each([[inbound, 4, 'inbound'], [outbound, 3, 'outbound']])('should declare the one consistent direction', (source, line, direction) => {
        const fixes = findQuickFixes(source, { line, diagnosticCode: 'PLAY0603' });
        expect(fixes).toHaveLength(1);
        expect(fixes[0].title).toBe(`Declare 'direction ${direction}'`);
        const fixed = applyQuickFixEdits(source, fixes[0].edits)!;
        expect(fixed).toContain(`      direction ${direction}\n`);
        expect(compiler.parse(fixed).diagnostics.map(diagnostic => diagnostic.code)).not.toContain('PLAY0603');
    });

    it('should use the document line ending', () => {
        const source = inbound.replaceAll('\n', '\r\n');
        const fixed = applyQuickFixEdits(source, findQuickFixes(source, { line: 4, diagnosticCode: 'PLAY0603' })[0].edits)!;
        expect(fixed).toContain('    slice Translate S\r\n      direction inbound\r\n');
    });

    it('should not offer a direction when the slice does not use public events', () => {
        expect(findQuickFixes('module M\n  feature F\n    slice Translate S\n      event Local\n', { line: 3, diagnosticCode: 'PLAY0603' })).toEqual([]);
    });
});
