// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const read = (...lines: string[]) => parse(lines.join('\n') + '\n');
const flow = (...lines: string[]) => read('workflow W', '  for R', ...lines).value.workflows![0];
const codes = (...lines: string[]) => read('workflow W', '  for R', ...lines).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0843').map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);
const node = (line: string) => flow(line).nodes[0];

describe('when reading workflows', () => {
    it('should name triggers by ordinal and read every trigger kind', () => {
        const result = flow(
            '  trigger on Seen',
            '  trigger when Life enters Overdue',
            '  trigger Daily every 1 days',
            '  trigger manual',
            '  trigger Weekly at 08:00 on Monday');
        expect(result.nodes.map(item => [item.name, item.triggerKind])).toEqual([['Trigger', 'Event'], ['Trigger2', 'StateEntered'], ['Daily', 'Schedule'], ['Trigger4', 'Manual'], ['Weekly', 'Schedule']]);
        expect(result.nodes[1]).toMatchObject({ stateMachine: 'Life', state: 'Overdue' });
        expect(result.nodes[0].event).toBe('Seen');
    });
    it('should read edges as shorthand, named outlets and quoted outlets without a target', () => {
        const result = flow('  decide D when a == 1', '    -> Next', '    "yes" -> Next', '    no -> Next', '    maybe');
        expect(result.nodes[0].edges.map(edge => [edge.outlet, edge.target])).toEqual([['next', 'Next'], ['yes', 'Next'], ['no', 'Next'], ['maybe', '']]);
    });
    it('should read action options', () => {
        expect(node('  action A sends C attempts 3 timeout 30 seconds retry after 5 minutes')).toMatchObject({ nodeKind: 'Action', command: 'C', attempts: 3, timeout: { amount: 30, unit: 'Seconds' }, retryAfter: { amount: 5, unit: 'Minutes' } });
    });
    it('should read waits, approvals, foreach and joins in every form', () => {
        const result = flow(
            '  wait P for 2 hours',
            '  wait E for Paid expires after 7 days',
            '  wait F for Paid',
            '  approval Ap by Boss expires after 1 day',
            '  approval Bp by Boss',
            '  foreach Each over lines parallel 2',
            '  foreach Plain over lines',
            '  join J1 all',
            '  join J2 any',
            '  join J3 at least 2');
        expect(result.nodes.map(item => item.nodeKind)).toEqual(['Wait', 'Wait', 'Wait', 'Approval', 'Approval', 'ForEach', 'ForEach', 'Join', 'Join', 'Join']);
        expect(result.nodes[0].duration).toMatchObject({ amount: 2, unit: 'Hours' });
        expect(result.nodes[1]).toMatchObject({ event: 'Paid', expiresAfter: { amount: 7, unit: 'Days' } });
        expect(result.nodes[2]).toMatchObject({ event: 'Paid', expiresAfter: null });
        expect(result.nodes[3]).toMatchObject({ by: 'Boss', expiresAfter: { amount: 1, unit: 'Days' } });
        expect(result.nodes[4].expiresAfter).toBeNull();
        expect(result.nodes[5]).toMatchObject({ items: 'lines', parallel: 2 });
        expect(result.nodes[6].parallel).toBeNull();
        expect(result.nodes.slice(7).map(item => [item.joinMode, item.joinCount])).toEqual([['All', null], ['Any', null], ['AtLeast', 2]]);
    });
    it('should refuse a malformed declaration and skip its body', () => {
        const result = read('workflow', '  for R', '  trigger manual');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0843']);
        expect(result.value.workflows![0].nodes).toEqual([]);
    });
    it('should refuse a malformed subject', () => expect(read('workflow W', '  for', '  trigger manual').diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)).toContain('PLAY0843@2'));
    it('should refuse a second subject', () => expect(codes('  for Other')).toEqual(['PLAY0843@3']));
    it('should refuse a subject with children', () => expect(codes('  for R2', '    more')).toContain('PLAY0843@4'));
    it('should refuse a workflow without a subject', () => expect(read('workflow W', '  trigger manual').diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0843'));
    it('should refuse a node declared twice', () => expect(codes('  action A sends C', '  action A sends D')).toEqual(['PLAY0843@4']));
    it('should refuse an unexpected line and skip its block', () => expect(codes('  bogus here', '    nested')).toEqual(['PLAY0843@3']));
    it('should refuse a malformed edge and skip its block', () => expect(codes('  action A sends C', '    not an edge', '      nested')).toEqual(['PLAY0843@4']));
    it('should refuse an edge with children', () => expect(codes('  action A sends C', '    next -> B', '      more')).toEqual(['PLAY0843@5']));
    it('should refuse a malformed trigger', () => expect(codes('  trigger')).toEqual(['PLAY0843@3']));
    it('should refuse a trigger of an unknown kind', () => expect(codes('  trigger sometimes')).toEqual(['PLAY0843@3']));
    it('should refuse a schedule trigger that does not parse', () => expect(read('workflow W', '  for R', '  trigger every soon').diagnostics.length).toBeGreaterThan(0));
    it('should refuse a malformed action', () => expect(codes('  action A')).toEqual(['PLAY0843@3']));
    it('should refuse a repeated or zero attempts', () => {
        expect(codes('  action A sends C attempts 0')).toEqual(['PLAY0843@3']);
        expect(codes('  action A sends C attempts 2 attempts 3')).toEqual(['PLAY0843@3']);
    });
    it('should refuse a repeated timeout and a zero timeout', () => {
        expect(codes('  action A sends C timeout 1 seconds timeout 2 seconds')).toEqual(['PLAY0843@3']);
        expect(codes('  action A sends C timeout 0 seconds')).toContain('PLAY0843@3');
    });
    it('should refuse a repeated retry and a zero retry', () => {
        expect(codes('  action A sends C retry after 1 seconds retry after 2 seconds')).toEqual(['PLAY0843@3']);
        expect(codes('  action A sends C retry after 0 seconds')).toContain('PLAY0843@3');
    });
    it('should refuse a malformed decide', () => expect(codes('  decide D')).toEqual(['PLAY0843@3']));
    it('should drop a decide whose condition does not parse', () => expect(flow('  decide D when date of').nodes).toEqual([]));
    it('should refuse a malformed wait', () => expect(codes('  wait X')).toEqual(['PLAY0843@3']));
    it('should refuse a zero duration wait and a zero expiry', () => {
        expect(codes('  wait X for 0 days')).toContain('PLAY0843@3');
        expect(codes('  wait X for Paid expires after 0 days')).toContain('PLAY0843@3');
    });
    it('should refuse a malformed approval and a zero expiry', () => {
        expect(codes('  approval X')).toEqual(['PLAY0843@3']);
        expect(codes('  approval X by Boss expires after 0 days')).toContain('PLAY0843@3');
    });
    it('should refuse a malformed foreach and a zero parallelism', () => {
        expect(codes('  foreach X')).toEqual(['PLAY0843@3']);
        expect(codes('  foreach X over lines parallel 0')).toEqual(['PLAY0843@3']);
    });
    it('should refuse a malformed join and a zero count', () => {
        expect(codes('  join J')).toEqual(['PLAY0843@3']);
        expect(codes('  join J at least 0')).toEqual(['PLAY0843@3']);
    });
    it('should read a specification inside the workflow', () => expect(flow('  trigger manual', '  specification S', '    given E', '    when start "x"', '    then completes').specifications.length).toBe(1));
});
