// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { readVerdicts } from '../VerdictReader';
import { diagnosticsServer, finding } from './given/a_diagnostics_server';

describe('when paging verdicts', () => {
    it('requests every check and pins the revision of the first page on the following pages', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' }), finding({ code: 'PLAY0533' }), finding({ code: 'PLAY0534' })], { size: 1 });
        const result = await readVerdicts(server.call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0532', 'PLAY0533', 'PLAY0534']);
        expect(server.calls.map(call => call.offset)).toEqual([0, 1, 2]);
        expect(server.calls.every(call => call.checks === 'all' && call.limit === 200)).toBe(true);
        expect(server.calls[0]).not.toHaveProperty('expectedSourceRevision');
        expect(server.calls.slice(1).every(call => call.expectedSourceRevision === 'rev-1')).toBe(true);
        expect(result.revision).toBe('rev-1');
    });
    it('keeps only the completeness findings, because the editor already shows every other code', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0100', severity: 'Error' }), finding({ code: 'PLAY0537' }), finding({ code: 'PLAY0021' })]);
        const result = await readVerdicts(server.call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0537']);
    });
    it('reports why the checks did not run', async () => {
        const result = await readVerdicts(diagnosticsServer([], { status: 'completeness checks skipped: the model has 1 error(s)' }).call);
        expect(result.status).toContain('skipped');
        expect(result.diagnostics).toEqual([]);
    });
    it('refuses pages whose source revision changed', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' }), finding({ code: 'PLAY0533' })], { size: 1, revisions: ['rev-1', 'rev-2'] });
        await expect(readVerdicts(server.call)).rejects.toMatchObject({ kind: 'StaleSourceRevision' });
    });
    it('refuses a page that does not progress', async () => {
        const call = async () => ({ sourceRevision: 'rev-1', page: { revision: 'rev-1', totalCount: 5, offset: 0, items: [], nextOffset: 0 } });
        await expect(readVerdicts(call)).rejects.toMatchObject({ kind: 'MalformedContract' });
    });
    it('keeps an application-level finding with no path at position 0 instead of aborting the run', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0537', path: null, line: 0, column: 0 }), finding({ code: 'PLAY0532' })]);
        const result = await readVerdicts(server.call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0537', 'PLAY0532']);
        expect(result.diagnostics[0].location).toEqual({ line: 0, column: 0, path: null });
    });
    it('never parses the location of a finding it does not keep', async () => {
        const unwanted = { code: 'PLAY0100', message: 'x', severity: 'Error', location: { path: null, line: 0, column: 0 } };
        const result = await readVerdicts(diagnosticsServer([unwanted, finding({ code: 'PLAY0532' })]).call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0532']);
    });
    it('still refuses a kept finding with a malformed position', async () => {
        await expect(readVerdicts(diagnosticsServer([finding({ code: 'PLAY0532', line: 0, column: 0 })]).call)).rejects.toMatchObject({ kind: 'MalformedContract' });
    });
});
