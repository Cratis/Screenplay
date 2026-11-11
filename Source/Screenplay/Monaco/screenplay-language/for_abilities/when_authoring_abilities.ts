// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { keywordDocs } from '../keyword-docs';
import { validateLines } from '../validation';

const labels = (chain: string[]) => completionEntriesFor(chain).map(entry => entry.label);

describe('when authoring abilities', () => {
    it('should offer abilities and a description under a system', () => expect(labels(['system'])).toEqual(['description', 'ability']));
    it('should offer the ability lines under an ability', () => expect(labels(['ability', 'system'])).toEqual(['description', 'input', 'returns', 'outcome', 'answers later', 'implementation']));
    it('should explain the ability keyword and its effect classes', () => expect(keywordDocs.ability).toContain('once'));
    it('should report a reserved outcome name with its code', () => {
        const issues = validateLines(['system S', '  ability A', '    outcome next']);
        expect(issues.map(issue => issue.code)).toEqual(['PLAY0985']);
    });
    it('should report nothing for a well-formed system', () => {
        expect(validateLines(['concept Id : Uuid', 'system S', '  ability A idempotent', '    id Id', '    returns', '      ok Bool', '    outcome Busy', '  ability B', '    answers later']).filter(issue => issue.severity === 'error')).toEqual([]);
    });
});
