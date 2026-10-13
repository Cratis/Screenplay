// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { responseAnalysis } from '../response-analysis';
import { validateLines } from '../validation';

const prefix = ['module M', '  feature F', '    slice StateChange S', '      command C', '        value String', '        returns', '          receipt = value', '      command D', '        value Int', '        returns', '          receipt = value'];
const screen = ['      screen Entry', '        on submit'];

function completions(lines: string[]) {
    const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
    return plan.kind === 'entries' ? plan.entries.filter(entry => entry.documentation.startsWith('Returned by')) : [];
}

describe('when binding response continuations', () => {
    it('should complete a returned record name after from', () => {
        const entries = completions([...prefix, ...screen, '          execute C', '            on success', '              navigate to Entry', '                with id from ']);
        expect(entries).toEqual([{ label: 'receipt', insertText: 'receipt', detail: 'String', sortText: '0_receipt', documentation: 'Returned by C on success' }]);
    });
    it('should complete scalar names after form sugar by', () => {
        const lines = ['module M', '  form Input for C', '    on submit navigate to Entry by ', '  feature F', '    slice StateChange S', '      command C', '        value String', '        returns value'];
        const plan = planCompletions(lines, 2, lines[2]);
        expect(plan).toMatchObject({ kind: 'entries', entries: [{ label: 'value', documentation: 'Returned by C on success' }] });
    });
    it('should complete names in a block form submit', () => {
        const entries = completions([...prefix, '  form Input for C', '    on submit', '      navigate to Entry', '        with id from ']);
        expect(entries[0]?.label).toBe('receipt');
    });
    it('should shadow response types within nested execute success and restore siblings', () => {
        const lines = [...prefix, ...screen, '          execute C', '            on success', '              execute D', '                on success', '                  notify info receipt', '              notify info receipt'];
        const fields = responseAnalysis(lines).continuationResponses!;
        expect(fields.get(lines.length - 2)).toMatchObject([{ name: 'receipt', command: 'D', type: { name: 'Int' } }]);
        expect(fields.get(lines.length - 1)).toMatchObject([{ name: 'receipt', command: 'C', type: { name: 'String' } }]);
    });
    it('should retain outer success response within inner failure', () => {
        expect(completions([...prefix, ...screen, '          execute C', '            on success', '              execute D', '                on failure', '                  navigate to Entry', '                    with id from '])[0]?.documentation).toBe('Returned by C on success');
    });
    it('should surface the failed response diagnostic in editor validation', () => {
        const lines = [...prefix, ...screen, '          execute C', '            on failure', '              notify error receipt'];
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0685')).toMatchObject([{ severity: 'error', line: lines.length - 1, message: "'receipt' is returned by 'C' only on success" }]);
    });
    it('should not offer a failed command response or leak a sibling success', () => {
        expect(completions([...prefix, ...screen, '          execute C', '            on success', '              notify info receipt', '            on failure', '              navigate to Entry', '                with id from '])).toEqual([]);
    });
    it('should preserve form fields declared after submit', () => {
        const lines = [...prefix, '  form Input for C', '    on submit', '      navigate to Entry', '        with id from receipt', '    field receipt'];
        expect(responseAnalysis(lines).continuationResponses?.get(lines.length - 2)).toEqual([]);
    });
    it('should preserve behavior parameters and unknown execute targets', () => {
        const lines = ['behavior B', '  parameter C', '  on submit', '    execute C', '      on success', '        navigate to Entry', '          with id from ', ...prefix];
        const plan = planCompletions(lines, 6, lines[6]);
        expect(plan.kind === 'entries' ? plan.entries.filter(entry => entry.documentation.startsWith('Returned by')) : []).toEqual([]);
    });
    it.each(['notify info receipt', 'confirm receipt', 'set selected to receipt', 'navigate to Entry\n                with id from receipt'])('should hover returned field types in %s', action => {
        const lines = [...prefix, ...screen, '          execute C', '            on success', ...('              ' + action).split('\n')];
        const column = lines.at(-1)!.lastIndexOf('receipt') + 1;
        expect(hoverContent(lines, lines.length - 1, 'receipt', column, column + 7)).toBe('**receipt** — String. Returned by C on success.');
    });
    it('should not treat dotted operands or declaration names as returned bindings', () => {
        const lines = [...prefix, ...screen, '          execute C', '            on success', '              navigate to receipt', '                with id from item.receipt'];
        const column = lines.at(-1)!.lastIndexOf('receipt') + 1;
        expect(hoverContent(lines, lines.length - 1, 'receipt', column, column + 7)).toBeNull();
        const declarationColumn = lines.at(-2)!.lastIndexOf('receipt') + 1;
        expect(hoverContent(lines, lines.length - 2, 'receipt', declarationColumn, declarationColumn + 7)).toBeNull();
    });
});
