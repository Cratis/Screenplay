// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const read = (...lines: string[]) => parse(lines.join('\n') + '\n');
const codes = (...lines: string[]) => read(...lines).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0834').map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);
const machine = (...lines: string[]) => read(...lines).value.stateMachines![0];

describe('when reading state machines', () => {
    it('should read states, modifiers, entries and guarded transitions', () => {
        const result = machine(
            'statemachine Life',
            '  description "Where it is"',
            '  follows Invoice',
            '  state Draft initial',
            '    on Sent when overdue == true -> Sent',
            '  state Sent',
            '    entering invokes Charge',
            '    entering starts Collect',
            '    on Paid -> Paid',
            '  state Paid final');
        expect(result).toMatchObject({ name: 'Life', follows: 'Invoice', description: 'Where it is' });
        expect(result.states.map(state => [state.name, state.isInitial, state.isFinal])).toEqual([['Draft', true, false], ['Sent', false, false], ['Paid', false, true]]);
        expect(result.states[0].transitions[0]).toMatchObject({ event: 'Sent', target: 'Sent', when: { kind: expect.any(String) } });
        expect(result.states[1].entering.map(entry => [entry.entryKind, entry.target])).toEqual([['Send', 'Charge'], ['Start', 'Collect']]);
    });
    it('should refuse a malformed declaration and skip its body', () => {
        const result = read('statemachine', '  follows Invoice', '  state A initial');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0834']);
        expect(result.value.stateMachines![0].states).toEqual([]);
    });
    it('should refuse a malformed follows', () => expect(codes('statemachine L', '  follows', '  state A initial')).toEqual(['PLAY0834@2', 'PLAY0834@1']));
    it('should refuse a second follows', () => expect(codes('statemachine L', '  follows A', '  follows B', '  state S initial')).toEqual(['PLAY0834@3']));
    it('should refuse a follows with children', () => expect(codes('statemachine L', '  follows A', '    extra', '  state S initial')).toEqual(['PLAY0834@3']));
    it('should refuse a machine that follows nothing', () => expect(codes('statemachine L', '  state S initial')).toEqual(['PLAY0834@1']));
    it('should refuse an unexpected line in the machine and skip its block', () => expect(codes('statemachine L', '  follows A', '  bogus here', '    nested', '  state S initial')).toEqual(['PLAY0834@3']));
    it('should refuse a state declared twice', () => expect(codes('statemachine L', '  follows A', '  state S initial', '  state S')).toEqual(['PLAY0834@4']));
    it('should refuse a malformed state and skip its body', () => {
        const result = machine('statemachine L', '  follows A', '  state 1bad', '    on E -> X');
        expect(result.states[0].name).toBe('');
    });
    it('should refuse a repeated modifier', () => expect(codes('statemachine L', '  follows A', '  state S initial initial')).toEqual(['PLAY0834@3']));
    it('should refuse a malformed entering', () => expect(codes('statemachine L', '  follows A', '  state S initial', '    entering run X')).toEqual(['PLAY0834@4']));
    it('should refuse an entering with children', () => expect(codes('statemachine L', '  follows A', '  state S initial', '    entering invokes X', '      more')).toEqual(['PLAY0834@5']));
    it('should refuse a malformed transition', () => expect(codes('statemachine L', '  follows A', '  state S initial', '    on E to S')).toEqual(['PLAY0834@4']));
    it('should refuse a transition with children', () => expect(codes('statemachine L', '  follows A', '  state S initial', '    on E -> S', '      more')).toEqual(['PLAY0834@5']));
    it('should drop a transition whose condition does not parse', () => {
        const result = read('statemachine L', '  follows A', '  state S initial', '    on E when date of -> S');
        expect(result.value.stateMachines![0].states[0].transitions).toEqual([]);
        expect(result.diagnostics.length).toBeGreaterThan(0);
    });
    it('should refuse an unexpected line in a state', () => expect(codes('statemachine L', '  follows A', '  state S initial', '    bogus', '      nested')).toEqual(['PLAY0834@4']));
    it('should read a specification inside the machine', () => {
        const result = machine('statemachine L', '  follows A', '  state S initial', '  specification Spec', '    given E', '    when append F', '    then is S');
        expect(result.specifications.length).toBe(1);
    });
});
