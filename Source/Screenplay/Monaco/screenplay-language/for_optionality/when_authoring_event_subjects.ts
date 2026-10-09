// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { hoverContent } from '../hover-content';
import { planCompletions } from '../completion-planner';
import { validateLines } from '../validation';
import { scanDocument } from '../symbols';

function hover(lines: string[]): string | null {
    const line = lines.at(-1)!;
    const column = line.lastIndexOf('subject') + 1;
    return hoverContent(lines, lines.length - 1, 'subject', column, column + 7);
}

describe('when authoring event subjects', () => {
    it('should explain event roles on standalone and inline properties', () => {
        hover(['event Changed', '  customerId Uuid subject'])!.should.contain('Report-only lineage metadata');
        hover(['command Change', '  produces event Changed', '    customerId Uuid subject = customerId'])!.should.contain('one data subject');
        scanDocument(['event Changed', '  customerId Uuid subject']).events[0].properties[0].isSubject!.should.be.true;
    });
    it('should distinguish policy subjects from callers', () => {
        hover(['policy Owner', '  require claim "id" matches subject'])!.should.contain('thing acted on, not the caller');
    });
    it('should preserve secret encryption scope meaning', () => {
        hover(['concept Key : String secret', '  secret scope subject'])!.should.contain('Secret encryption scope per data subject');
    });
    it('should not give role hover to contextual names or wrong owners', () => {
        (hover(['event Changed', '  subject String']) === null).should.be.true;
        (hover(['event Changed', '  key subject']) === null).should.be.true;
        (hover(['command Change', '  customerId Uuid subject']) === null).should.be.true;
    });
    it('should offer the modifier only on event property lines', () => {
        const lines = ['event Changed', '  customerId Uuid '];
        const plan = planCompletions(lines, 1, lines[1]);
        (plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'subject')).should.be.true;
        const command = planCompletions(['command Change', lines[1]], 1, lines[1]);
        (command.kind === 'entries' && command.entries.some(entry => entry.label === 'subject')).should.be.false;
    });
    it('should surface the shared compiler refusal', () => {
        validateLines(['module M', '  feature F', '    slice StateChange S', '      event Changed', '        key Int subject']).some(issue => issue.code === 'PLAY0591').should.be.true;
    });
});
