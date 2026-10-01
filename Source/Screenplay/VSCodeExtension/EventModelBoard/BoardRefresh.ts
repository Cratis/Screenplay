// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { boardFor } from './compileBoard';
import { compileForBoard } from './boardSources';

// How long the board waits after a change before compiling again, so typing does not redraw it per key.
const recompileDelay = 300;

// Keeps one board in step with its application. The board compiles again when the document changes, when
// another .play file of its folder application is edited, and when one is saved, created or deleted on
// disk. Compiling is asynchronous, so a result is only shown when no later one was asked for meanwhile.
export class BoardRefresh implements vscode.Disposable {
    readonly #subscriptions: vscode.Disposable[] = [];
    #watcher: vscode.FileSystemWatcher | undefined;
    #root: vscode.Uri | undefined;
    #pending: ReturnType<typeof setTimeout> | undefined;
    #latest = 0;

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

    soon(): void {
        clearTimeout(this.#pending);
        this.#pending = setTimeout(() => this.now(), recompileDelay);
    }

    now(): void {
        void this.#compile(++this.#latest);
    }

    dispose(): void {
        clearTimeout(this.#pending);
        this.#watcher?.dispose();
        this.#subscriptions.forEach(subscription => subscription.dispose());
    }

    async #compile(request: number): Promise<void> {
        const compilation = await compileForBoard(this.document);
        if (request !== this.#latest) {
            return;
        }
        this.#watch(compilation.root);
        void this.webview.postMessage(boardFor(compilation.result, compilation.name));
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
