// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const specification = (...lines: string[]) => parse(['statemachine L', '  follows A', '  state S initial', ...lines.map(line => `  ${line}`)].join('\n') + '\n');
const first = (...lines: string[]) => specification(...lines).value.stateMachines![0].specifications[0];
const codes = (...lines: string[]) => specification(...lines).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0834').map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);

describe('when reading lifecycle specifications', () => {
    it('should read every expectation kind with and without a target', () => {
        const result = first('specification Spec', '  description "d"', '  given E', '  when append F', '  outcome N next', '  then is S', '  then sends C', '  then starts W', '  then completes', '  then fails at N', '  then waits    at N');
        expect(result.description).toBe('d');
        expect(result.then.map(item => [item.expectation, item.target])).toEqual([['State', 'S'], ['Sends', 'C'], ['Starts', 'W'], ['Completes', null], ['Fails', 'N'], ['Waits', 'N']]);
        expect(result.outcomes).toMatchObject([{ node: 'N', outlet: 'next' }]);
        expect(result.whenAppended).not.toBeNull();
    });
    it('should read a start key', () => {
        const result = first('specification Spec', '  when start "abc"');
        expect(result.whenStarted).not.toBeNull();
        expect(result.whenAppended).toBeNull();
    });
    it('should refuse a malformed header and skip its body', () => {
        expect(codes('specification', '  when append F')).toEqual(['PLAY0834@4']);
        expect(specification('specification', '  when append F').value.stateMachines![0].specifications[0].name).toBe('');
    });
    it('should refuse a second when', () => expect(codes('specification Spec', '  when append F', '  when append G', '    for x')).toEqual(['PLAY0834@6']));
    it('should refuse a when that is neither append nor start', () => expect(codes('specification Spec', '  when something')).toEqual(['PLAY0834@5']));
    it('should refuse a when append without an event and keep no event', () => {
        expect(specification('specification Spec', '  when append').diagnostics.length).toBeGreaterThan(0);
        expect(first('specification Spec', '  when append').whenAppended).toBeNull();
    });
    it('should refuse a malformed outcome', () => expect(codes('specification Spec', '  outcome N')).toEqual(['PLAY0834@5']));
    it('should refuse an outcome with children', () => expect(codes('specification Spec', '  outcome N next', '    more')).toEqual(['PLAY0834@6']));
    it('should refuse a completes with a target and a target-less sends', () => {
        expect(codes('specification Spec', '  then completes N')).toEqual(['PLAY0834@5']);
        expect(codes('specification Spec', '  then invokes')).toEqual(['PLAY0834@5']);
    });
    it('should refuse an unknown expectation', () => expect(codes('specification Spec', '  then explodes')).toEqual(['PLAY0834@5']));
    it('should refuse a then with children', () => expect(codes('specification Spec', '  then completes', '    more')).toEqual(['PLAY0834@6']));
    it('should refuse an unexpected line and skip its block', () => expect(codes('specification Spec', '  bogus', '    nested')).toEqual(['PLAY0834@5']));
});
