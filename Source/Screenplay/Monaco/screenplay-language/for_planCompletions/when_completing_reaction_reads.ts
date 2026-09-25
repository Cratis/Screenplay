// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor, planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { clauseKeywords } from '../language';

// Monaco's tokenizer uses clauseKeywords for directives at every indentation depth.
describe('when editing reaction trigger reads', () => {
    it('highlights reads as a clause keyword under a trigger', () => {
        clauseKeywords.includes('reads').should.be.true;
    });

    it('offers a reads clause under a reaction trigger', () => {
        // Context chains are innermost first: ['when', 'reaction'], not ['reaction', 'when'].
        completionEntriesFor(['when', 'reaction']).some(entry => entry.label === 'reads').should.be.true;
        const plan = planCompletions(['reaction Handle', '  when Placed', '    '], 2, '    ');
        (plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'reads')).should.be.true;
    });

    for (const source of ['every 15 minutes', 'at 08:00']) {
        it(`offers a whole-view read with no by clause under ${source}`, () => {
            const plan = planCompletions(['reaction Handle', `  ${source}`, '    '], 2, '    ');
            (plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'reads' &&
                entry.insertText === 'reads ${1:View}')).should.be.true;
        });

        it(`shows no by syntax in reads hover under ${source}`, () => {
            const lines = ['reaction Handle', `  ${source}`, '    reads Status'];
            const content = hoverContent(lines, 2, 'reads', 5, 10);
            content!.should.contain('reads <View>');
            content!.should.not.contain('by <');
        });
    }
});
