// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { clauseKeywords } from '../language';

const labels = (lines: string[]) => {
    const plan = planCompletions(lines, lines.length - 1, lines[lines.length - 1]);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
};

describe('when editing guarded actions', () => {
    it('should offer a label headed snippet inside a screen section', () => labels(['screen V', '  section actions', '    ']).should.contain('action "…"'));
    for (const header of ['action "Choose"', 'action $strings.actions.choose']) {
        it(`should offer alternatives and fallback but not label under ${header}`, () => labels(['screen V', `  ${header}`, '    '])
            .should.deep.equal(['when … execute', 'otherwise hidden', 'otherwise execute', 'navigate to']));
        for (const clause of ['when item.ready == true execute ', 'otherwise execute ']) {
            it(`should offer commands for ${clause} under ${header}`, () => planCompletions(['screen V', `  ${header}`, `    ${clause}`], 2, `    ${clause}`).should.deep.equal({ kind: 'commands' }));
        }
    }
    for (const clause of ['when item.ready == true execute C', 'otherwise execute C']) {
        it(`should offer input bindings under ${clause}`, () => labels(['screen V', '  action "Choose"', `    ${clause}`, '      ']).should.deep.equal(['with … from']));
    }
    it('should offer no arguments under hidden', () => labels(['screen V', '  action "Choose"', '    otherwise hidden', '      ']).should.deep.equal([]));
    it('should preserve plain action completions', () => labels(['screen V', '  action C', '    ']).should.deep.equal(['navigate to', 'label']));
    it('should not treat specification steps as guarded alternatives', () => {
        const plan = planCompletions(['specification S', '  when C', '    '], 2, '    ');
        (plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'with … from')).should.be.false;
    });
    for (const word of ['otherwise', 'hidden', 'execute', 'when']) {
        if (word !== 'when') it(`should keep ${word} contextual`, () => clauseKeywords.should.not.contain(word));
        it(`should document ${word}`, () => hoverContent(['screen V', '  action "Choose"', `    ${word}`], 2, word, 5, 5 + word.length)!.should.contain('action'));
    }
});
