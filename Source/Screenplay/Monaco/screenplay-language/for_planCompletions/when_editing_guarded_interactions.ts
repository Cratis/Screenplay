// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { planCompletions } from '../completion-planner';
import { validateLines } from '../validation';

function labels(lines: string[]) {
    const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
}

describe('when editing guarded interactions', () => {
    for (const trigger of ['click', 'double click', 'select']) {
        it(`offers block choices under ${trigger}`, () => expect(labels(['screen V', `  on ${trigger}`, '    '])).toContain('when …'));
        it(`offers actions inside a ${trigger} branch`, () => expect(labels(['screen V', `  on ${trigger}`, '    when item.status == "open"', '      '])).toContain('execute'));
    }
    it('does not offer alternatives on submit', () => expect(labels(['screen V', '  on submit', '    '])).not.toContain('when …'));
    it('forwards the deprecated strict where warning', () => {
        const source = 'module M\n  feature F\n    slice StateView S\n      screen V\n        on click\n          where item.status == "open"\n          notify info "Open"';
        expect(validateLines(source.split('\n')).find(diagnostic => diagnostic.code === 'PLAY0564')?.severity).toBe('warning');
    });
});
