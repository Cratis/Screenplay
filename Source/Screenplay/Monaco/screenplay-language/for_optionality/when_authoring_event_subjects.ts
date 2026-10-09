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
    it.each([
        '  require authenticated and claim "id" matches subject',
        '  require not not (claim "id" matches subject)',
        '  or (role "A" and claim "id" matches subject)',
        '  require claim "escaped\\"id" matches subject',
    ])('should explain policy subjects in compound or continued condition %s', line => {
        hover(['policy Owner', '  require authenticated', line])!.should.contain('thing acted on, not the caller');
    });
    it('should explain each subject on a compound policy line', () => {
        const line = '  require claim "first" matches subject or claim "second" matches subject';
        for (const column of [line.indexOf('subject') + 1, line.lastIndexOf('subject') + 1]) {
            hoverContent(['policy Owner', line], 1, 'subject', column, column + 7)!.should.contain('thing acted on, not the caller');
        }
    });
    it.each(['event Changed', '  produces event Changed'])('should compose optional and subject completions under %s', header => {
        const parents = header.startsWith(' ') ? ['command Change', header] : [header];
        const indent = header.startsWith(' ') ? '    ' : '  ';
        for (const [suffix, expected] of [['', ['optional', 'subject']], ['o', ['optional']], ['s', ['subject']], ['x', []]] as const) {
            const line = `${indent}note String ${suffix}`;
            const lines = [...parents, line];
            const plan = planCompletions(lines, lines.length - 1, line);
            const modifiers = plan.kind === 'entries' ? plan.entries.map(entry => entry.label).filter(label => ['optional', 'subject'].includes(label)) : [];
            modifiers.should.deep.equal(expected);
        }
    });
    it.each(['event Changed', '  produces event Changed'])('should preserve collection optionality without subject under %s', header => {
        const parents = header.startsWith(' ') ? ['command Change', header] : [header];
        const indent = header.startsWith(' ') ? '    ' : '  ';
        for (const suffix of ['', 'o', 's']) {
            const line = `${indent}notes String[] ${suffix}`;
            const lines = [...parents, line];
            const plan = planCompletions(lines, lines.length - 1, line);
            const modifiers = plan.kind === 'entries' ? plan.entries.map(entry => entry.label).filter(label => ['optional', 'subject'].includes(label)) : [];
            modifiers.should.deep.equal(suffix === 's' ? [] : ['optional']);
        }
    });
    it.each(['Int', 'Decimal', 'Bool', 'Date', 'DateTime', 'String optional', 'String?', 'Address', 'Status', 'Email', 'Secret'])('should not suggest subject for refused target %s', type => {
        const line = `  value ${type} s`;
        const lines = ['type Address', '  street String', 'concept Status : Enum', '  active', 'concept Email : String pii', 'concept Secret : String secret', 'event Changed', line];
        const plan = planCompletions(lines, lines.length - 1, line);
        (plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'subject')).should.be.false;
    });
    it.each(['String', 'Uuid', 'Int'])('should suggest subject for unprotected identity concept over %s', primitive => {
        const line = '  value CustomerId s';
        const lines = [`concept CustomerId : ${primitive}`, 'event Changed', line];
        const plan = planCompletions(lines, 2, line);
        (plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'subject')).should.be.true;
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
    it.each([
        { lines: ['trigger T', '  customerId Uuid subject'] },
        { lines: ['trigger T', '  customerId Uuid', 'module M', '  feature F', '    slice Automation S', '      reaction R', '        when T', '          customerId Uuid subject'] },
    ])('should surface the trigger and reaction data refusal in $lines', ({ lines }) => {
        validateLines(lines).some(issue => issue.code === 'PLAY0593').should.be.true;
    });
    it('should surface the shared compiler refusal', () => {
        validateLines(['module M', '  feature F', '    slice StateChange S', '      event Changed', '        key Int subject']).some(issue => issue.code === 'PLAY0591').should.be.true;
    });
});
