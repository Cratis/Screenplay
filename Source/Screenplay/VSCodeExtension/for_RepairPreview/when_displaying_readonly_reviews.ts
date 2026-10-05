// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, it, expect, vi } from 'vitest';
import * as vscode from 'vscode';
import { RepairPreviewProvider } from '../RepairPreviewProvider';
import { RepairPreview } from '../RepairPreview';
import { object, array } from '../RepairClient';

const host = vi.hoisted(() => ({ documents: [] as { uri: { toString(): string }; isDirty: boolean; getText(): string }[], read: undefined as ((uri: vscode.Uri) => Uint8Array) | undefined, opened: [] as string[] }));
vi.mock('vscode', () => {
    const open = async (uri: vscode.Uri) => {
        const content = Buffer.from(host.read!(uri)).toString('utf8').replace(/^\uFEFF/, '');
        const document = { uri, isDirty: false, getText: () => content };
        host.documents.push(document); host.opened.push(uri.toString());
        return document;
    };
    return {
        EventEmitter: class { event = () => ({ dispose() {} }); dispose() {} },
        FileType: { File: 1 }, FilePermission: { Readonly: 1 }, FileSystemError: { NoPermissions: (message: string) => new Error(message) },
        Uri: { from: ({ scheme, path }: { scheme: string; path: string }) => ({ scheme, path, toString: () => `${scheme}:${path}` }) },
        workspace: { get textDocuments() { return host.documents; }, openTextDocument: open },
        window: { showTextDocument: async () => {} },
        commands: { executeCommand: async (_command: string, before: vscode.Uri, after: vscode.Uri) => { await open(before); await open(after); } },
    };
});
let provider: RepairPreviewProvider;
const preview: RepairPreview = { token: 'complete-preview', title: 'Explicit routing change', code: 'PLAY0478', files: [{ path: 'application.play', before: Buffer.from('\uFEFF// 😀\r\n'), after: Buffer.from('// 😀\n') }, { path: '.screenplay/identities.json', before: null, after: Buffer.from('{}') }], authoring: [], executable: [], executableReady: false, droppedComments: [] };
beforeEach(() => {
    host.documents = []; host.opened = [];
    provider = new RepairPreviewProvider(); host.read = uri => provider.readFile(uri);
});
it('opens every source/state diff plus readiness/routing summary without granting writable storage', async () => {
    await provider.show(preview);
    expect(host.opened).toHaveLength(5);
    expect(provider.review(preview.token)).toBe(preview);
    expect(() => provider.writeFile()).toThrow('read-only');
    const summary = host.documents[4].getText();
    expect(summary).toContain('Routing change'); expect(summary).toContain('not ready'); expect(summary).toContain('not normal editor Undo');
});
it('invalidates authority if another extension programmatically changes a virtual buffer', async () => {
    await provider.show(preview);
    host.documents[0].getText = () => 'changed';
    await expect(Promise.resolve().then(() => provider.review(preview.token))).rejects.toMatchObject({ kind: 'PreviewModified' });
    expect(provider.token).toBeUndefined();
});
it('releases byte/session views when a preview closes', async () => {
    await provider.show(preview);
    const uri = vscode.Uri.from({ scheme: provider.scheme, path: '/complete-preview/0/before/application.play' });
    expect(provider.closed(uri)).toBe(true);
    expect(provider.token).toBeUndefined();
    expect(() => provider.readFile(uri)).toThrow('expired');
});
it('opens inspection as clean read-only virtual JSON without granting a review token or writable storage', async () => {
    expect(await provider.showInspection({ state: { exists: true }, uncertainApply: { failureKind: 'ApplyOutcomeUnknown' } })).toBe(true);
    expect(host.opened[0]).toMatch(/^screenplay-repair:.*\/inspection\.json$/);
    const document = host.documents[0];
    expect(document.isDirty).toBe(false);
    expect(provider.stat(document.uri as vscode.Uri).permissions).toBe(vscode.FilePermission.Readonly);
    expect(JSON.parse(document.getText()).state.exists).toBe(true);
    expect(provider.token).toBeUndefined();
    expect(() => provider.writeFile()).toThrow('read-only');
    expect(() => provider.review('anything')).toThrow('expired');
});
it('keeps active review authority independent of opening and closing inspection', async () => {
    await provider.show(preview);
    await provider.showInspection({ state: { exists: true } });
    expect(provider.review(preview.token)).toBe(preview);
    const inspection = host.documents.at(-1)!;
    expect(provider.closed(inspection.uri as vscode.Uri)).toBe(false);
    expect(provider.review(preview.token)).toBe(preview);
    expect(() => provider.readFile(inspection.uri as vscode.Uri)).toThrow('expired');
});
it('does not count inspection as successfully displayed when its owner expires while the view opens', async () => {
    let checks = 0;
    expect(await provider.showInspection({}, () => ++checks === 1)).toBe(false);
    expect(provider.token).toBeUndefined();
});
it('presents structured conflict kinds and diagnostics separately without issuing an Apply token', async () => {
    await provider.showFailure('ProposalRejected', { conflicts: [{ kind: 'InvalidOperation', message: 'destination not proven' }], authoringDiagnostics: [{ code: 'PLAY0287' }], executableReady: false });
    const result = object(JSON.parse(host.documents[0].getText()));
    const details = object(result.details);
    expect(result.failureKind).toBe('ProposalRejected');
    expect(object(array(details.conflicts)[0]).kind).toBe('InvalidOperation');
    expect(object(array(details.authoringDiagnostics)[0]).code).toBe('PLAY0287');
    expect(provider.token).toBeUndefined();
    expect(() => provider.review('anything')).toThrow('expired');
});
