// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes } from '../QuickFixes';

const shell = 'layout AppShell\n  category application\n  topbar\n  content\n\n';

describe('when replacing the legacy profile layout', () => {
    it('should report PLAY0791 on the profile with the canonical form in the message', () => {
        const diagnostics = compiler.parse(shell + 'ui profile Desktop\n  layout AppShell\n').diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0791');
        expect(diagnostics.map(diagnostic => [diagnostic.severity, diagnostic.location.line, diagnostic.message])).toEqual([['warning', 6, "The ui profile 'Desktop' selects its shell with the legacy 'layout AppShell' - write 'root template AppShell'"]]);
    });

    it('should not report a profile that names its shell with root template', () => {
        expect(compiler.parse(shell + 'ui profile Desktop\n  root template AppShell\n').diagnostics).toEqual([]);
    });

    it.each(['\n', '\r\n'])('should rewrite the layout line to root template with %j endings', ending => {
        const source = (shell + 'ui profile Desktop\n  target platform web\n  layout AppShell\n  theme Dark\n').replaceAll('\n', ending);
        const fixes = findQuickFixes(source, { line: 6, diagnosticCode: 'PLAY0791' });
        expect(fixes).toHaveLength(1);
        const fixed = applyQuickFixEdits(source, fixes[0].edits)!;
        expect(fixed).toBe(source.replace('  layout AppShell', '  root template AppShell'));
        expect(compiler.parse(fixed).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0791')).toEqual([]);
    });

    it('should remove the ignored layout line beside a root template', () => {
        const source = shell + 'ui profile Desktop\n  layout AppShell\n  root template AppShell\n';
        const fixes = findQuickFixes(source, { line: 6 });
        expect(fixes.map(fix => fix.title)).toEqual(['Remove the ignored layout line']);
        expect(applyQuickFixEdits(source, fixes[0].edits)).toBe(shell + 'ui profile Desktop\n  root template AppShell\n');
    });

    it('should fix only the profile asked for', () => {
        const source = shell + 'ui profile A\n  layout AppShell\n\nui profile B\n  layout AppShell\n';
        expect(applyQuickFixEdits(source, findQuickFixes(source, { line: 9 })[0].edits)).toBe(shell + 'ui profile A\n  layout AppShell\n\nui profile B\n  root template AppShell\n');
    });

    it('should offer no fix when the layout line carries a comment it cannot safely rewrite', () => {
        const source = shell + 'ui profile Desktop\n  layout AppShell // legacy\n';
        expect(compiler.parse(source).diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0791']);
        expect(findQuickFixes(source, { line: 6 })).toEqual([]);
    });

    it('should skip a ui profile whose declaration is invalid and one that holds a fenced body', () => {
        expect(compiler.parse('ui profile\n  layout AppShell\n').diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0791')).toEqual([]);
        expect(compiler.parse(shell + 'ui profile Desktop\n  notes\n    ```\n    layout Inside\n    ```\n  root template AppShell\n').diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0791')).toEqual([]);
    });
});
