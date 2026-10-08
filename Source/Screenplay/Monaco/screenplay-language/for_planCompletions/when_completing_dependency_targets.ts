// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { mergeSymbols, scanDocument } from '../symbols';

const rest = '\n    feature Nested\n  feature Runs\nmodule Timesheets\n  feature Approval\n  feature Reporting';
const entries = (before = '    depends on ', declared = '') => {
    const lines = ('module Payroll\n  feature Handover\n' + declared + before + rest).split('\n');
    const plan = planCompletions(lines, 2 + (declared ? 1 : 0), before);
    expect(plan.kind).toBe('entries');
    return plan.kind === 'entries' ? plan.entries : [];
};

describe('when completing dependency targets', () => {
    it('should offer siblings modules and qualified foreign features', () => {
        expect(entries().map(entry => entry.label)).toEqual(['Runs', 'Timesheets', 'Timesheets.Approval', 'Timesheets.Reporting']);
    });
    it('should exclude self ancestors and descendants', () => {
        expect(entries().map(entry => entry.label)).not.toEqual(expect.arrayContaining(['Handover', 'Payroll', 'Nested']));
    });
    it('should order nearest scope first with sortText', () => {
        const targets = entries();
        expect(targets.every(entry => typeof entry.sortText === 'string')).toBe(true);
        expect(targets.map(entry => entry.sortText)).toEqual(targets.map(entry => entry.sortText).sort());
    });
    it('should insert only the final segment after a qualifier', () => {
        expect(entries('    depends on Timesheets.').map(entry => [entry.label, entry.insertText])).toEqual([['Approval', 'Approval'], ['Reporting', 'Reporting']]);
    });
    it('should exclude already declared targets by resolved address', () => {
        expect(entries('    depends on ', '    depends on Payroll.Runs\n').map(entry => entry.label)).not.toContain('Runs');
    });
    it('should not suppress the declaration on the current line', () => {
        expect(entries('    depends on Runs').map(entry => entry.label)).toContain('Runs');
    });
    it('should also work in a module body', () => {
        const lines = 'module Payroll\n  depends on \n  feature Runs\nmodule Timesheets\n  feature Approval'.split('\n');
        const plan = planCompletions(lines, 1, lines[1]);
        expect(plan).toMatchObject({ kind: 'entries', entries: [{ label: 'Timesheets' }, { label: 'Timesheets.Approval' }] });
    });
    it('should not expose synthetic scopes for an unplaced feature', () => {
        const lines = 'feature Handover\n  depends on '.split('\n');
        const symbols = mergeSymbols(scanDocument(lines), scanDocument('feature Other'.split('\n')));
        expect(planCompletions(lines, 1, lines[1], symbols)).toEqual({ kind: 'entries', entries: [] });
    });
    it('should refuse comments and fences', () => {
        expect(planCompletions(['module Payroll', '  depends on // Runs'], 1, '  depends on // Runs')).toEqual({ kind: 'none' });
        expect(planCompletions(['module Payroll', '  description', '    ```text', '    depends on ', '    ```'], 3, '    depends on ')).toEqual({ kind: 'none' });
    });
});
