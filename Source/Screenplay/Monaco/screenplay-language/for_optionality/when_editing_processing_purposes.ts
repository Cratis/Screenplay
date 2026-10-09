// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';

describe('when editing processing purposes', () => {
    it('should complete declarations, references and purpose fields', () => {
        expect(completionEntriesFor([]).some(entry => entry.label === 'purpose')).toBe(true);
        for (const kind of ['module', 'feature', 'slice']) expect(completionEntriesFor([kind]).some(entry => entry.label === 'purpose')).toBe(true);
        expect(completionEntriesFor(['purpose']).map(entry => entry.label)).toContain('erasure exception');
        expect(completionEntriesFor(['purpose']).find(entry => entry.label === 'basis')?.insertText).toContain('legitimateInterests');
    });
    it('should distinguish report-only metadata from authorization', () => {
        expect(hoverContent(['purpose Billing'], 0, 'purpose', 1, 8)).toContain('report-only');
    });
    it('should forward structural purpose diagnostics without running completeness', () => {
        expect(validateLines(['purpose Billing', '  basis unknown']).filter(issue => issue.code === 'PLAY0573')).toMatchObject([{ severity: 'error' }]);
        expect(validateLines(['purpose Billing'])).toEqual([]);
    });
});
