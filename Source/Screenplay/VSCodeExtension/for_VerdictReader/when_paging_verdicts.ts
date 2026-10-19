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
});
