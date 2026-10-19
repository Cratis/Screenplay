// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { RepairFailure } from '../RepairClient';
import { registerVerdicts, Verdicts, VerdictConnection, verdictsCommand } from '../RegisterVerdicts';
import { editor, FakeDocument } from './given/an_editor';
import { diagnosticsServer, finding } from '../for_VerdictReader/given/a_diagnostics_server';

vi.mock('vscode', async importOriginal => (await import('./given/an_editor')).createVscode(await importOriginal<typeof import('../vscode.stub')>()));

const collection = 'screenplay-csharp-verdicts';
const file = (name: string) => vscode.Uri.file(`/model/${name}`);
const document = (name: string, languageId = 'screenplay'): FakeDocument => ({ uri: Object.assign(file(name), { scheme: 'file', fsPath: `/model/${name}` }), languageId, isDirty: false, lineCount: 10, lineAt: () => ({ text: 'screen Orders' }) });
const run = () => editor.commands.get(verdictsCommand)!() as Promise<void>;

describe('when running the C# checks', () => {
    let verdicts: Verdicts;
    let connect: ReturnType<typeof vi.fn<() => Promise<VerdictConnection>>>;
    let served: ReturnType<typeof diagnosticsServer>;

    const serve = (findings: Record<string, unknown>[], options: Parameters<typeof diagnosticsServer>[1] = {}) => {
        served = diagnosticsServer(findings, options);
        connect.mockResolvedValue({ root: '/model', call: (tool, args) => served.call(tool, args) });
    };
    beforeEach(() => {
        editor.reset();
        connect = vi.fn<() => Promise<VerdictConnection>>();
        verdicts = registerVerdicts({ subscriptions: [] } as unknown as vscode.ExtensionContext, { connect });
    });
    afterEach(() => { verdicts.dispose(); vi.useRealTimers(); });

    it('publishes the findings of the bridge into the separate collection', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' }), finding({ code: 'PLAY0100', path: 'a.play' })]);
        await run();
        expect(editor.diagnosticsOf(collection, file('a.play')).map(marker => marker.code)).toEqual(['PLAY0535']);
        expect(editor.warnings).toEqual([]);
    });
    it('publishes the binder verdict into the same collection', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' })], { binder: [finding({ code: 'PLAY0546', path: 'a.play' })] });
        await run();
        expect(editor.diagnosticsOf(collection, file('a.play')).map(marker => marker.code)).toEqual(['PLAY0535', 'PLAY0546']);
        expect([...editor.collections.keys()]).toEqual([collection]);
    });
    it('clears everything when the binder read finds the source changed', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' })], { binder: [finding({ code: 'PLAY0546', path: 'a.play' })] });
        await run();
        serve([finding({ code: 'PLAY0535', path: 'a.play' })], { binder: [finding({ code: 'PLAY0546', path: 'a.play' })], revisions: ['rev-1', 'rev-2'] });
        await run();
        expect(editor.count(collection)).toBe(0);
        expect(editor.warnings[0]).toContain('StaleSourceRevision');
    });
    it('tells the user when the bridge is not enabled, and launches nothing further', async () => {
        connect.mockRejectedValue(new RepairFailure('NotEnabled', 'C# repairs are opt-in. Configure screenplay.repairs in User settings.'));
        await run();
        expect(editor.warnings).toHaveLength(1);
        expect(editor.warnings[0]).toContain('NotEnabled');
        expect(editor.warnings[0]).toContain('screenplay.repairs');
        expect(editor.count(collection)).toBe(0);
    });
    it('drops the verdicts of a document when that document changes', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' }), finding({ code: 'PLAY0536', path: 'b.play' })]);
        await run();
        editor.changed!({ document: document('a.play'), contentChanges: [{}] } as unknown as vscode.TextDocumentChangeEvent);
        expect(editor.diagnosticsOf(collection, file('a.play'))).toEqual([]);
        expect(editor.diagnosticsOf(collection, file('b.play'))).toHaveLength(1);
    });
    it('keeps the verdicts when a change carries no edit', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' })]);
        await run();
        editor.changed!({ document: document('a.play'), contentChanges: [] } as unknown as vscode.TextDocumentChangeEvent);
        expect(editor.diagnosticsOf(collection, file('a.play'))).toHaveLength(1);
    });
    it('clears everything and says so when the source revision went stale', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' })]);
        await run();
        serve([finding({ code: 'PLAY0536', path: 'a.play' }), finding({ code: 'PLAY0537', path: 'a.play' })], { size: 1, revisions: ['rev-1', 'rev-2'] });
        await run();
        expect(editor.count(collection)).toBe(0);
        expect(editor.warnings[0]).toContain('StaleSourceRevision');
    });
    it('clears everything when the bridge reports that the root or configuration changed', async () => {
        serve([finding({ code: 'PLAY0535', path: 'a.play' })]);
        await run();
        verdicts.clear();
        expect(editor.count(collection)).toBe(0);
    });
    it('removes the collection on dispose', () => {
        verdicts.dispose();
        expect(editor.disposedCollections).toEqual([collection]);
    });

    describe('after a save', () => {
        beforeEach(() => { vi.useFakeTimers(); });

        it('stays quiet while the setting is off', async () => {
            editor.saved!(document('a.play') as unknown as vscode.TextDocument);
            await vi.advanceTimersByTimeAsync(2_000);
            expect(connect).not.toHaveBeenCalled();
        });
        it('checks once, a moment after the last of several saves, when the setting is on', async () => {
            editor.configuration.onSave = true;
            serve([finding({ code: 'PLAY0535', path: 'a.play' })]);
            editor.saved!(document('a.play') as unknown as vscode.TextDocument);
            editor.saved!(document('a.play') as unknown as vscode.TextDocument);
            await vi.advanceTimersByTimeAsync(2_000);
            expect(connect).toHaveBeenCalledTimes(1);
            expect(editor.diagnosticsOf(collection, file('a.play'))).toHaveLength(1);
        });
        it('ignores files that are not Screenplay documents', async () => {
            editor.configuration.onSave = true;
            editor.saved!(document('a.txt', 'plaintext') as unknown as vscode.TextDocument);
            await vi.advanceTimersByTimeAsync(2_000);
            expect(connect).not.toHaveBeenCalled();
        });
        it('does not interrupt the user when the bridge refuses', async () => {
            editor.configuration.onSave = true;
            connect.mockRejectedValue(new RepairFailure('NotEnabled', 'not enabled'));
            editor.saved!(document('a.play') as unknown as vscode.TextDocument);
            await vi.advanceTimersByTimeAsync(2_000);
            expect(editor.warnings).toEqual([]);
        });
        it('tries once more when the workspace changed under the first attempt', async () => {
            editor.configuration.onSave = true;
            let attempts = 0;
            served = diagnosticsServer([finding({ code: 'PLAY0535', path: 'a.play' })]);
            connect.mockImplementation(async () => ({ root: '/model', call: async (tool, args) => {
                if (tool === 'diagnostics' && ++attempts === 1) throw new RepairFailure('StaleEpoch', 'changed');
                return served.call(tool, args);
            } }));
            editor.saved!(document('a.play') as unknown as vscode.TextDocument);
            await vi.advanceTimersByTimeAsync(2_000);
            expect(attempts).toBe(2);
            expect(editor.diagnosticsOf(collection, file('a.play'))).toHaveLength(1);
        });
    });
});
