// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { conceptItems } from '../completion-items';
import { hoverContent } from '../hover-content';
import { diagnosticCodes } from '../diagnostic-codes';

function labels(lines: string[]): string[] {
    const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
}

describe('when authoring phase-three generation', () => {
    it.each(['pattern', 'random', 'rule'])('should describe %s as admitted rather than preview', strategy => {
        const item = conceptItems.find(entry => entry.label === `generate ${strategy}`)!;
        expect(item.documentation).toContain('ESM v21');
        expect(item.documentation).not.toContain('not yet executable');
        const line = `  generate ${strategy} `;
        expect(hoverContent(['concept Code : String', line], 1, strategy, 12, 12 + strategy.length)).toContain('ESM v21');
    });
    it('should offer direct sources under a generation rule', () => {
        expect(labels(['concept Code : String', '  generate rule Make', '    '])).toEqual(['file', 'csharp']);
    });
    it.each([
        ['concept Code : String', '  validate checksum '],
        ['concept Code : String', '  validate', '    checksum '],
    ])('should complete checksum algorithms on String concepts', (...lines: string[]) => {
        expect(labels(lines)).toEqual(['luhn', 'mod11', 'mod97']);
    });
    it.each(['Int', 'Uuid', 'Enum', 'Bool'])('should not offer concept checksum validation for %s', primitive => {
        expect(labels([`concept Ticket : ${primitive}`, '  '])).not.toContain('validate checksum');
        expect(labels([`concept Ticket : ${primitive}`, '  validate', '    '])).not.toContain('checksum');
    });
    it('should offer a checksum rule inside a String concept validation block', () => {
        expect(labels(['concept Code : String', '  validate', '    '])).toContain('checksum');
    });
    it('should preserve generation support diagnostic codes', () => {
        expect(diagnosticCodes.generationSupportBudgetExceeded).toBe('PLAY0691');
        expect(diagnosticCodes.emptyGenerationSupport).toBe('PLAY0692');
    });
});
