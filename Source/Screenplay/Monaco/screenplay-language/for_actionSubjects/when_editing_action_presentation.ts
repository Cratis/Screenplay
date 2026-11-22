// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { completionEntriesFor, planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';

const hint = 'presentation placement';
const labels = (header: string) => {
    const plan = planCompletions(['screen List', `  ${header}`, '    '], 2, '    ');
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
};

describe('when editing action presentation', () => {
    it('should offer row-menu placement on an explicit item action', () => expect(labels('action Retry for item')).toContain(hint));
    it('should offer it alongside inline labels', () => expect(labels('action Retry for item label "Retry"')).toContain(hint));
    for (const header of ['action Retry', 'action Retry for screen', 'action "Choose" for item', 'action "Choose" for screen']) {
        it(`should not suggest an unsupported hint on ${header}`, () => expect(labels(header)).not.toContain(hint));
    }
    it('should share the item-action completion with language services', () => expect(completionEntriesFor(['action', 'screen'], { headers: ['action Retry for item'], document: [] }).find(entry => entry.label === hint)?.insertText).toBe('presentation placement "${1:row-menu}"'));
    it('should explain advisory behavior in hover', () => expect(hoverContent(['screen List', '  action Retry for item', '    presentation placement "row-menu"'], 2, 'presentation', 5, 17)).toContain('flat action bar'));
    it('should forward the new diagnostic from the compiler', () => expect(validateLines(['screen List', '  action Retry for screen', '    presentation placement "row-menu"']).some(marker => marker.code === 'PLAY1166')).toBe(true));
});
