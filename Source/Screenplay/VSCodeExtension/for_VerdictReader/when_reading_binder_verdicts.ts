// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { RepairFailure } from '../RepairClient';
import { readVerdicts } from '../VerdictReader';
import { diagnosticsServer, finding } from './given/a_diagnostics_server';

describe('when reading binder verdicts', () => {
    it('pages the executable diagnostics pinned to the opened workspace and keeps only PLAY0546', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' })], { binder: [finding({ code: 'PLAY0100' }), finding({ code: 'PLAY0546', path: 'b.play' }), finding({ code: 'PLAY0546', path: 'c.play' })] });
        const result = await readVerdicts(server.call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0532', 'PLAY0546', 'PLAY0546']);
        expect(server.workspaceCalls).toHaveLength(3);
        expect(server.workspaceCalls.every(call => call.view === 'executable-diagnostics' && call.expectedRevision === 'ws-1' && call.expectedCatalogRevision === 'catalog-1')).toBe(true);
    });
    it('proves the source did not change by pinning the diagnostics revision again', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' })], { binder: [finding({ code: 'PLAY0546' })] });
        await readVerdicts(server.call);
        expect(server.calls.at(-1)).toMatchObject({ limit: 1, offset: 0, expectedSourceRevision: 'rev-1' });
    });
    it('treats a source revision that differs between the two reads as stale', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' })], { binder: [finding({ code: 'PLAY0546' })], revisions: ['rev-1', 'rev-2'] });
        await expect(readVerdicts(server.call)).rejects.toMatchObject({ kind: 'StaleSourceRevision' });
    });
    it('treats a workspace that changed while paging as skipped, silently', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' })], { binder: [finding({ code: 'PLAY0546' })], driftedWorkspace: true });
        const result = await readVerdicts(server.call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0532']);
    });
    it('skips the binder verdicts silently when the tool refuses', async () => {
        const result = await readVerdicts(diagnosticsServer([finding({ code: 'PLAY0532' })], { binder: 'refused' }).call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0532']);
    });
    it('skips the binder verdicts silently while a repair review is open, keeping the completeness findings', async () => {
        const server = diagnosticsServer([finding({ code: 'PLAY0532' })], { binder: [finding({ code: 'PLAY0546' })] });
        const call = (tool: 'diagnostics' | 'open-workspace' | 'read-workspace', args: Record<string, unknown>) => tool === 'open-workspace'
            ? Promise.reject(new RepairFailure('SessionBusy', 'A repair review is open.'))
            : server.call(tool, args);
        const result = await readVerdicts(call);
        expect(result.diagnostics.map(item => item.code)).toEqual(['PLAY0532']);
        expect(server.workspaceCalls).toEqual([]);
    });
});
