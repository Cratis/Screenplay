// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'node:path';
import { createRequire } from 'node:module';
import type { RepairObservation } from '../RepairObservation';

/** Test-driver observation of actual installed callbacks; never fabricates product authority. */
export class NativeTestController {
    readonly #api: typeof vscode;
    readonly #register: typeof vscode.languages.registerCodeActionsProvider;
    readonly #listeners = new Set<(uri: string) => void>();
    readonly #createDiagnostics: typeof vscode.languages.createDiagnosticCollection;
    readonly diagnostics: { method: string; afterDispose: boolean }[] = [];
    readonly trace: { uri: string; at: number; phase: string }[] = [];
    constructor(extensionPath: string) {
        this.#api = createRequire(path.join(extensionPath, 'package.json'))('vscode') as typeof vscode;
        this.#register = this.#api.languages.registerCodeActionsProvider;
        this.#createDiagnostics = this.#api.languages.createDiagnosticCollection;
        this.#api.languages.createDiagnosticCollection = (name => {
            const collection = this.#createDiagnostics(name);
            if (name === 'screenplay-csharp') {
                let disposed = false;
                for (const method of ['clear', 'set', 'dispose'] as const) {
                    const original = collection[method];
                    Object.assign(collection, { [method]: (...args: unknown[]) => {
                        this.diagnostics.push({ method, afterDispose: disposed });
                        if (this.diagnostics.length > 512) this.diagnostics.shift();
                        if (method === 'dispose') disposed = true;
                        return Reflect.apply(original, collection, args);
                    } });
                }
            }
            return collection;
        }) as typeof vscode.languages.createDiagnosticCollection;
        this.#api.languages.registerCodeActionsProvider = ((selector, provider, metadata) => {
            if (typeof selector !== 'object' || !('language' in selector) || !('scheme' in selector) || selector.language !== 'screenplay' || selector.scheme !== 'file') return this.#register(selector, provider, metadata);
            const original = provider.provideCodeActions.bind(provider);
            return this.#register(selector, { ...provider, provideCodeActions: (document, ...args) => {
                const uri = document.uri.toString();
                this.trace.push({ uri, at: Date.now(), phase: 'entered' });
                for (const listener of [...this.#listeners]) listener(uri);
                const result = original(document, ...args);
                return Promise.resolve(result).finally(() => this.trace.push({ uri, at: Date.now(), phase: 'completed' }));
            } }, metadata);
        }) as typeof vscode.languages.registerCodeActionsProvider;
    }
    entered(uri: vscode.Uri): { promise: Promise<void>; dispose(): void } {
        let listener!: (value: string) => void;
        let timer!: ReturnType<typeof setTimeout>;
        const dispose = () => { clearTimeout(timer); this.#listeners.delete(listener); };
        const promise = new Promise<void>((resolve, reject) => {
            listener = value => { if (value === uri.toString()) { dispose(); resolve(); } };
            timer = setTimeout(() => { dispose(); reject(new Error('Actual installed provider did not enter within 5 seconds.')); }, 5_000);
            this.#listeners.add(listener);
        });
        return { promise, dispose };
    }
    /** Read-only installed-extension metadata; never obtains a session or token. */
    readObservation(): readonly RepairObservation[] {
        const exported = this.#api.extensions.getExtension<{ repairObservation?: { read(): readonly RepairObservation[] } }>('cratis.screenplay')?.exports;
        if (!exported?.repairObservation) throw new Error('Installed extension has no passive repair observation reader.');
        return exported.repairObservation.read();
    }
    dispose(): void {
        this.#api.languages.registerCodeActionsProvider = this.#register;
        // Keep method observers on the already-created real collection through
        // native disposal; restoring this factory never changes that collection.
        this.#api.languages.createDiagnosticCollection = this.#createDiagnostics;
    }
}
