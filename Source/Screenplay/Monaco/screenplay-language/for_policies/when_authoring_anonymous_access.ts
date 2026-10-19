// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { keywordDocs } from '../keyword-docs';
import { clauseKeywords } from '../language';
import { validateLines } from '../validation';

describe('when authoring anonymous access', () => {
    it('should offer the anonymous requirement inside a policy', () => {
        const plan = planCompletions(['policy Anyone', '  '], 1, '  ');
        expect(plan.kind).toBe('entries');
        if (plan.kind === 'entries') expect(plan.entries.map(entry => entry.label)).toContain('require anonymous');
    });
    it('should explain and recognize the keyword', () => {
        expect(clauseKeywords).toContain('anonymous');
        expect(keywordDocs.anonymous).toContain('without an identity');
    });
    it('should accept anonymous on its own', () => {
        expect(validateLines(['policy Anyone', '  require anonymous'])).toEqual([]);
    });
    it('should report anonymous combined with another condition', () => {
        validateLines(['policy Anyone', '  require anonymous or authenticated']).map(issue => issue.code).should.deep.equal(['PLAY0804']);
    });
    it('should report negated anonymous', () => {
        validateLines(['policy Signed', '  require not anonymous']).map(issue => issue.code).should.deep.equal(['PLAY0805']);
    });
});
