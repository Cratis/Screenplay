// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';
import { keywordDocs } from '../keyword-docs';
import { clauseKeywords, constructKeywords } from '../language';

const source = [
    'audience Staff',
    '    description "Employees"',
    '',
    'audience Customers',
    '',
    'availability StaffOnly',
    '    for Staff',
    '',
    'persona Clerk',
    '    audience Staff',
    '',
    'module Billing',
    '    available for Staff or Customers',
    '    feature Invoices',
    '        available with StaffOnly',
    '        slice Issue',
    '            available when $context.audiences == "x"',
].join('\n').split('\n');

function plan(lineIndex: number, textBefore: string) {
    const lines = [...source];
    lines[lineIndex] = textBefore;
    return planCompletions(lines, lineIndex, textBefore, scanDocument(lines));
}

describe('when authoring availability', () => {
    it('should scan the declared audiences and availabilities', () => {
        const symbols = scanDocument(source);
        expect(symbols.audiences.map(audience => audience.name)).toEqual(['Staff', 'Customers']);
        expect(symbols.availabilities.map(availability => availability.name)).toEqual(['StaffOnly']);
    });

    it('should complete audiences after available for', () => {
        expect(plan(12, '    available for ').kind).toBe('audiences');
        expect(plan(12, '    available for Staff or ').kind).toBe('audiences');
    });

    it('should complete availabilities after available with', () => {
        expect(plan(14, '        available with ').kind).toBe('availabilities');
    });

    it('should complete audiences on a persona audience line', () => {
        expect(plan(9, '    audience ').kind).toBe('audiences');
    });

    it('should complete audiences after for inside an availability', () => {
        expect(plan(6, '    for ').kind).toBe('audiences');
    });

    it('should document and highlight the keywords', () => {
        for (const word of ['audience', 'availability', 'available']) {
            expect(constructKeywords).toContain(word);
            expect(keywordDocs[word]).toBeTruthy();
        }
    });

    it('should highlight the clause words', () => {
        for (const word of ['for', 'when', 'with']) expect(clauseKeywords).toContain(word);
    });

    it('should not flag the gate lines', () => {
        expect(validateLines(source).filter(issue => /audience|availab/i.test(source[issue.line] ?? ''))).toEqual([]);
    });
});
