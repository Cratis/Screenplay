// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { keywordDocs } from '../keyword-docs';
import { contextVariableItems } from '../completion-items';

function entries(source: string) {
    const lines = source.split('\n');
    const index = lines.length - 1;
    const plan = planCompletions(lines, index, lines[index]);
    return plan.kind === 'entries' ? plan.entries : [];
}

function hover(line: string, word: string) {
    const column = line.indexOf(word) + 1;
    return hoverContent([line], 0, word, column, column + word.length);
}

const command = 'module Sales\n  feature Billing\n    slice StateChange Issue\n      command Issue\n';

describe('when authoring clock operands', () => {
    it('should offer operands in command conditions and property rules', () => {
        for (const body of ['        validate\n          require dueDate > ', '        produces when dueDate > ', '        validate\n          dueDate > ']) {
            entries(command + body).map(entry => entry.label).should.include.members(['today', 'now', 'date of now', 'today - days', 'now + hours']);
        }
    });
    it('should complete clock operands after date of', () => entries(command + '        validate\n          require dueDate > date of n').map(entry => entry.label).should.deep.equal(['today', 'now']));
    it('should not offer clocks in refused or non-expression contexts', () => {
        for (const source of ['concept Due : Date\n  validate\n    dueDate > ', command + '        handler\n          dueDate > ', 'module M\n  feature F\n    slice Automation A\n      reaction R\n        where dueDate > ', command + '        dueDate ', command + '        // require dueDate > ', command + '        description "dueDate > ']) {
            entries(source).some(entry => entry.label === 'today').should.equal(false);
        }
    });
    it('should offer the builtin calendar source without a context declaration', () => {
        for (const partial of ['', '$', '$context.', '$context.t']) {
            const result = entries(command + `        calendar from ${partial}`);
            result.map(entry => entry.label).should.deep.equal(['$context.timeZone']);
            result[0].insertText.should.equal(partial.startsWith('$context.') ? 'timeZone' : partial ? 'context.timeZone' : '\\$context.timeZone');
        }
        contextVariableItems.some(entry => entry.label === '$context.timeZone').should.equal(false);
    });
    it('should offer deterministic scenario fixtures and calendar declarations', () => {
        entries('specification Example\n  ').map(entry => entry.label).should.include.members(['given clock', 'given time zone']);
        entries(command + '        ').map(entry => entry.label).should.include.members(['calendar', 'calendar from']);
        entries('').map(entry => entry.label).should.include.members(['time zone', 'time zone configurable default']);
    });
    it('should explain v37 without claiming a blanket refusal', () => {
        for (const word of ['today', 'now', 'calendar', 'zone', 'Date']) {
            keywordDocs[word].should.not.match(/syntax-only|binding reports PLAY0268|context and today remain refused/i);
        }
        hover('        require dueDate > today', 'today')!.should.include('integral day');
        hover('        require dueDate > date of now + 2 hours', 'now')!.should.include('before converting');
    });
    it('should preserve ordinary property names and dotted paths', () => {
        for (const line of ['        now String', '        today Date', '        require value > book.now']) {
            const word = line.includes('today') ? 'today' : 'now';
            (hover(line, word) === null).should.equal(true);
        }
    });
});
