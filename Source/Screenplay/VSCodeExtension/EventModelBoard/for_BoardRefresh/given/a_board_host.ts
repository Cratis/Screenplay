// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { TextDocument, Webview } from 'vscode';

type Listener = () => void;

// A file system watcher that counts its disposals, so a spec can tell a watcher that was cleaned up from
// one that was left behind; `fire()` stands in for a .play file changing on disk.
export class WatcherDouble {
    readonly #listeners: Listener[] = [];
    disposals = 0;

    constructor(readonly base: string) {}

    readonly onDidChange = (listener: Listener) => this.#listen(listener);
    readonly onDidCreate = (listener: Listener) => this.#listen(listener);
    readonly onDidDelete = (listener: Listener) => this.#listen(listener);

    fire(): void {
        this.#listeners.forEach(listener => listener());
    }

    dispose(): void {
        this.disposals++;
    }

    #listen(listener: Listener) {
        this.#listeners.push(listener);
        return { dispose: () => {} };
    }
}

class RelativePattern {
    constructor(readonly base: { path: string }, readonly pattern: string) {}
}

// What the board refresh reached: each compilation it asked for, waiting until the spec finishes it, every
// watcher it created, every board it mapped and every message it posted to the board.
export const host = {
    compilations: [] as ((compilation: unknown) => void)[],
    watchers: [] as WatcherDouble[],
    mappings: 0,
    messages: [] as unknown[],
    changeDocument: (_uri: unknown) => {},
};

export function reset(): void {
    host.compilations = [];
    host.watchers = [];
    host.mappings = 0;
    host.messages = [];
    host.changeDocument = () => {};
}

// The parts of `vscode` the board refresh talks to, laid over the shared stub.
export const vscode = {
    RelativePattern,
    workspace: {
        onDidChangeTextDocument: (listener: (event: { document: { uri: unknown } }) => void) => {
            host.changeDocument = uri => listener({ document: { uri } });
            return { dispose: () => {} };
        },
        createFileSystemWatcher: (pattern: RelativePattern) => {
            const watcher = new WatcherDouble(pattern.base.path);
            host.watchers.push(watcher);
            return watcher;
        },
    },
};

// Compilation never finishes on its own; the spec decides when, which is what lets it dispose in between.
export function compileForBoard(): Promise<unknown> {
    return new Promise(resolve => host.compilations.push(resolve));
}

export function boardFor(_result: unknown, name: string) {
    host.mappings++;
    return { type: 'showBoard', name };
}

export function a_compilation_rooted_at(root: string) {
    return { name: 'work', root: { path: root }, files: [`${root}/work.play`], result: {}, wholeApplication: {} };
}

export const document = {
    uri: { path: '/work/app/work.play', toString: () => 'file:///work/app/work.play' },
} as unknown as TextDocument;

export const webview = {
    postMessage: async (message: unknown) => {
        host.messages.push(message);
        return true;
    },
} as unknown as Webview;
