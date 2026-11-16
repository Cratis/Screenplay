// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { boardFor } from './compileBoard';
import { compileForBoard } from './boardSources';
import { resolveBoardSource } from './BoardSourcePath';

// How long the board waits after a change before compiling again, so typing does not redraw it per key.
const recompileDelay = 300;

// Keeps one board in step with its application. The board compiles again when the document changes, when
// another .play file of its folder application is edited, and when one is saved, created or deleted on
// disk. Compiling is asynchronous, so a result is only shown when no later one was asked for meanwhile.
// Disposal is final: a compilation still running then is dropped, and nothing schedules another.
export class BoardRefresh implements vscode.Disposable {
    readonly #subscriptions: vscode.Disposable[] = [];
    #watcher: vscode.FileSystemWatcher | undefined;
    #root: vscode.Uri | undefined;
    #files: ReadonlySet<string> = new Set();
    #pending: ReturnType<typeof setTimeout> | undefined;
    #latest = 0;
    #disposed = false;

    constructor(private readonly document: vscode.TextDocument, private readonly webview: vscode.Webview) {
        this.#subscriptions.push(vscode.workspace.onDidChangeTextDocument(event => {
            if (this.#concerns(event.document.uri)) {
                this.soon();
            }
        }));
    }

    // The folder of the application the board shows, when the document is part of one.
    get root(): vscode.Uri | undefined {
        return this.#root;
    }

    // The file to open for a path the board's webview asked for; only a file the board compiled is ever returned.
    sourceFor(requested: unknown): vscode.Uri | undefined {
        return resolveBoardSource(this.#root, this.#files, requested);
    }

    soon(): void {
        if (this.#disposed) {
            return;
        }
        clearTimeout(this.#pending);
        this.#pending = setTimeout(() => this.now(), recompileDelay);
    }

    now(): void {
        if (this.#disposed) {
            return;
        }
        void this.#compile(++this.#latest);
    }

    dispose(): void {
        if (this.#disposed) {
            return;
        }
        this.#disposed = true;
        this.#latest++;
        clearTimeout(this.#pending);
        this.#pending = undefined;
        this.#watcher?.dispose();
        this.#watcher = undefined;
        this.#subscriptions.forEach(subscription => subscription.dispose());
    }

    async #compile(request: number): Promise<void> {
        const compilation = await compileForBoard(this.document);
        if (this.#disposed || request !== this.#latest) {
            return;
        }
        this.#watch(compilation.root);
        this.#files = new Set(compilation.files);
        void this.webview.postMessage(boardFor(compilation.result, compilation.name, compilation.wholeApplication));
    }

    #concerns(uri: vscode.Uri): boolean {
        if (uri.toString() === this.document.uri.toString()) {
            return true;
        }
        return this.#root !== undefined && uri.path.endsWith('.play') && uri.path.startsWith(`${this.#root.path}/`);
    }

    #watch(root: vscode.Uri | undefined): void {
        if (root?.path === this.#root?.path) {
            return;
        }
        this.#watcher?.dispose();
        this.#watcher = undefined;
        this.#root = root;
        if (root !== undefined) {
            this.#watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(root, '**/*.play'));
            this.#watcher.onDidChange(() => this.soon());
            this.#watcher.onDidCreate(() => this.soon());
            this.#watcher.onDidDelete(() => this.soon());
        }
    }
}
