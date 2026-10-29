// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';

const sources = [
    ['concept Money : Decimal', '  validate', '    file Rules/Money.cs'],
    ['command Pay', '  validate', '    file Rules/Pay.cs'],
];

describe('when authoring imperative validation files', () => {
    it.each(sources)('should admit the file body under %s', (...lines) => {
        expect(validateLines(lines).filter(issue => issue.severity === 'error')).toEqual([]);
        const plan = planCompletions(lines, 2, '    ');
        expect(plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'file')).toBe(true);
        expect(hoverContent(lines, 1, 'validate', 3, 11)).toContain('file <path>');
    });
});
