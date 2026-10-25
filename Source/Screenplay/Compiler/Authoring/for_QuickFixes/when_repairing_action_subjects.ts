// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes, isQuickFixDiagnostic } from '../QuickFixes';
import { prepareActionSubjectQuickFixes } from '../ActionSubjectQuickFixes';

const source = `concept ItemId : Uuid
module Work
  feature Items
    slice StateView List
      readmodel Row
        rowId ItemId key
        name String
      command Retry
        itemId ItemId identifier
      query Rows => Row[]
      screen List
        data Row[] via query Rows
        action Retry // preserve this comment
`;

const fixes = (text: string, code = 'PLAY0819') => findQuickFixes(text, { diagnosticCode: code });

describe('when repairing action subjects', () => {
    it('should advertise both inference diagnostics', () => {
        expect(isQuickFixDiagnostic('PLAY0818')).toBe(true);
        expect(isQuickFixDiagnostic('PLAY0819')).toBe(true);
    });
    it('should make the inferred item subject explicit without rewriting comments', () => {
        const fix = fixes(source, 'PLAY0818');
        expect(fix).toHaveLength(1);
        const edited = applyQuickFixEdits(source, fix[0].edits)!;
        expect(edited).toContain('action Retry for item // preserve this comment');
        expect(parse(edited).diagnostics.some(diagnostic => ['PLAY0818', 'PLAY0819'].includes(diagnostic.code))).toBe(false);
        expect(fixes(edited, 'PLAY0818')).toEqual([]);
    });
    it('should suppress legacy intent that the effective key fallback cannot represent', () => {
        expect(fixes(source).map(fix => fix.title)).toEqual(["Use 'for item' (item-bound action)", "Use 'for screen' (no selection binding)"]);
    });
    it('should offer explicit legacy row inputs when name matching is provable', () => {
        const legacy = source.replace('rowId ItemId key', 'itemId ItemId key').replace('itemId ItemId identifier', 'itemId ItemId');
        const proposals = fixes(legacy);
        expect(proposals).toHaveLength(3);
        const retained = proposals.find(fix => fix.title === 'Keep legacy row bindings explicitly')!;
        const edited = applyQuickFixEdits(legacy, retained.edits)!;
        expect(edited).toContain('action Retry for item');
        expect(edited).toContain('with itemId from item.itemId');
        expect(fixes(edited)).toEqual([]);
    });
    it('should preserve toolbar legacy bindings with existing parameters and CRLF endings', () => {
        const toolbar = source.replace('rowId ItemId key', 'itemId ItemId key')
            .replace('itemId ItemId identifier', 'itemId ItemId\n        name String')
            .replace('        action Retry // preserve this comment\n', '        toolbar\n          item retry action Retry // preserve this comment\n            parameter name from data item.name')
            .replaceAll('\n', '\r\n');
        const proposals = fixes(toolbar);
        expect(proposals).toHaveLength(3);
        const retained = proposals.find(fix => fix.title === 'Keep legacy row bindings explicitly')!;
        const edited = applyQuickFixEdits(toolbar, retained.edits)!;
        expect(edited).toContain('item retry action Retry for item // preserve this comment');
        expect(edited).toContain('parameter name from data item.name\r\n            parameter itemId from data item.itemId\r\n');
        expect(fixes(edited)).toEqual([]);
    });
    it('should not propose subject repairs when the authored clause is invalid', () => {
        const invalid = source.replace('action Retry //', 'action Retry for unknown //');
        expect(prepareActionSubjectQuickFixes(invalid, {})()).toEqual([]);
    });
    it('should not guess declarations in imported buffer snapshots', () => {
        expect(findQuickFixes(source, { documents: new Map([['a.play', source], ['b.play', '']]), path: 'a.play', diagnosticCode: 'PLAY0818' })).toEqual([]);
    });
});
