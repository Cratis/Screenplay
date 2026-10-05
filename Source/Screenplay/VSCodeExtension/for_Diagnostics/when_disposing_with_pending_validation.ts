// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerDiagnostics } from '../Diagnostics';

const editor = vi.hoisted(() => ({
    changed: undefined as ((event: vscode.TextDocumentChangeEvent) => void) | undefined,
    disposed: false,
    writes: vi.fn(),
}));

vi.mock('vscode', async importOriginal => {
    const original = await importOriginal<typeof import('../vscode.stub')>();
    return {
        ...original,
        languages: {
            createDiagnosticCollection: () => ({
                set: () => {
                    if (editor.disposed) throw new Error('illegal state - object is disposed');
                    editor.writes();
                },
                dispose: () => { editor.disposed = true; },
            }),
        },
        workspace: {
            textDocuments: [],
            onDidOpenTextDocument: () => ({ dispose: () => {} }),
            onDidChangeTextDocument: (callback: typeof editor.changed) => {
                editor.changed = callback;
                return { dispose: () => { editor.changed = undefined; } };
            },
            onDidCloseTextDocument: () => ({ dispose: () => {} }),
        },
    };
});

function change(path: string): void {
    editor.changed?.({ document: {
        languageId: 'screenplay', isClosed: false, uri: vscode.Uri.file(path), getText: () => '',
    } } as vscode.TextDocumentChangeEvent);
}

describe('when disposing diagnostics with pending validation', () => {
    let subscriptions: vscode.Disposable[];

    beforeEach(() => {
        vi.useFakeTimers();
        editor.disposed = false;
        editor.writes.mockClear();
        subscriptions = [];
        registerDiagnostics({ subscriptions } as vscode.ExtensionContext, {
            fileOf: () => undefined,
            onDidChange: () => ({ dispose: () => {} }),
        } as unknown as ApplicationIndex);
    });

    afterEach(() => {
        subscriptions.forEach(subscription => subscription.dispose());
        vi.restoreAllMocks();
        vi.useRealTimers();
    });

    it('should cancel every pending debounce without writing to the disposed collection', () => {
        change('/first.play');
        change('/second.play');
        expect(vi.getTimerCount()).toBe(2);
        subscriptions.forEach(subscription => subscription.dispose());
        expect(editor.disposed).toBe(true);
        expect(vi.getTimerCount()).toBe(0);
        expect(() => vi.runAllTimers()).not.toThrow();
        expect(editor.writes).not.toHaveBeenCalled();
    });

    it('should ignore a debounce callback already queued when disposal starts', () => {
        const timers = vi.spyOn(globalThis, 'setTimeout');
        change('/first.play');
        const callback = timers.mock.calls[0][0];
        subscriptions.forEach(subscription => subscription.dispose());
        expect(typeof callback).toBe('function');
        expect(() => { if (typeof callback === 'function') callback(); }).not.toThrow();
        expect(editor.writes).not.toHaveBeenCalled();
    });

    it('should still refresh a live collection after the debounce', () => {
        change('/first.play');
        vi.advanceTimersByTime(299);
        expect(editor.writes).not.toHaveBeenCalled();
        vi.advanceTimersByTime(1);
        expect(editor.writes).toHaveBeenCalledOnce();
    });
});
