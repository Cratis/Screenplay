// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, it, expect, vi } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';

const host = vi.hoisted(() => ({ trusted: true, uiKind: 1, folders: [] as { uri: { scheme: string; fsPath: string } }[], documents: [] as { uri: { scheme: string; fsPath: string; toString(): string }; version: number; isDirty: boolean }[], settings: new Map<string, Record<string, unknown>>() }));
vi.mock('vscode', () => ({
    workspace: { get isTrusted() { return host.trusted; }, get workspaceFolders() { return host.folders; }, get textDocuments() { return host.documents; }, getConfiguration: () => ({ inspect: (key: string) => host.settings.get(key) }) },
    env: { get uiKind() { return host.uiKind; } }, UIKind: { Web: 2 },
}));
import { checkRepairEnvironment, userRepairConfiguration } from '../RepairCodeActions';
let root: string;
beforeEach(() => {
    const tasks = path.resolve('../../..', '.ai-work'); fs.mkdirSync(tasks, { recursive: true });
    root = fs.mkdtempSync(path.join(tasks, 'repair-environment-'));
    host.trusted = true; host.uiKind = 1;
    host.folders = [{ uri: { scheme: 'file', fsPath: root } }]; host.documents = [];
    host.settings = new Map([
        ['enabled', { defaultValue: false, globalValue: true }], ['executable', { globalValue: process.execPath }],
        ['arguments', { globalValue: ['mcp'] }], ['modelRoot', { globalValue: root }],
    ]);
});
it('requires explicit User opt-in and a user-installed absolute executable', () => {
    expect(checkRepairEnvironment(userRepairConfiguration())).toEqual({});
    host.settings.set('enabled', { defaultValue: false });
    expect(userRepairConfiguration).toThrow('opt-in');
    host.settings.set('enabled', { globalValue: true });
    host.settings.set('executable', { globalValue: './project/server' });
    expect(userRepairConfiguration).toThrow('absolute');
});
for (const scope of ['workspaceValue', 'workspaceFolderValue', 'workspaceLanguageValue', 'workspaceFolderLanguageValue']) it(`rejects ${scope} even when it matches the approved executable`, () => {
    host.settings.set('executable', { globalValue: process.execPath, [scope]: process.execPath });
    expect(userRepairConfiguration).toThrow('User settings');
});
it('refuses trust, browser and virtual workspace hosts rather than pretending to spawn', () => {
    const launch = userRepairConfiguration(); host.trusted = false;
    expect(() => checkRepairEnvironment(launch)).toThrow('trusted'); host.trusted = true;
    host.uiKind = 2; expect(() => checkRepairEnvironment(launch)).toThrow('native'); host.uiKind = 1;
    host.folders[0].uri.scheme = 'vscode-vfs'; expect(() => checkRepairEnvironment(launch)).toThrow('filesystem');
});
it('refuses a missing binary, a changed executable configuration and a linked root', () => {
    const launch = userRepairConfiguration();
    host.settings.set('executable', { globalValue: path.join(root, 'missing') });
    expect(() => checkRepairEnvironment(userRepairConfiguration())).toThrow('user-installed');
    expect(() => checkRepairEnvironment(launch)).toThrow('configuration changed');
    const linked = path.join(root, 'linked'); fs.symlinkSync(root, linked, 'dir');
    host.settings.set('modelRoot', { globalValue: linked }); host.settings.set('executable', { globalValue: process.execPath });
    expect(() => checkRepairEnvironment(userRepairConfiguration())).toThrow('physical root');
});
for (const file of ['sibling.play', 'Handler.cs', '.screenplay/identities.json', 'new-attachment.ts']) it(`refuses unsaved ${file} without autosave; read-only recovery inspection preserves it`, () => {
    host.documents = [{ uri: { scheme: 'file', fsPath: path.join(root, file), toString: () => file }, version: 7, isDirty: true }];
    const launch = userRepairConfiguration();
    expect(() => checkRepairEnvironment(launch)).toThrow('Save or discard');
    expect(checkRepairEnvironment(launch, true)).toEqual({ [file]: 7 });
    expect(host.documents[0].isDirty).toBe(true);
});
